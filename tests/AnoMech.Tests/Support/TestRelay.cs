using System.Net.WebSockets;
using AnoMech.Multiplayer;
using AnoMech.Network;

namespace AnoMech.Tests;

// A relay and each client's link to it, in virtual time. A client's upload is RelayClient's: one
// queue in order, one message on the socket at a time, a send done once the socket buffer has
// taken all of it. The relay routes as the real one does, the host to everyone and a peer to the
// host only, stamping each message with its sender; each client's download delivers in order,
// through RelayClient's own decoding.
internal sealed class TestRelay(int seed, Func<int, ConnectionProfile> linkFor)
{
    public const string Address = "test-relay";
    private const string SessionCode = "TESTRM";
    private const double Step = 0.0005;
    // Framing per message: WebSocket header and mask going up; the relay's envelope and header
    // coming down; TLS records both ways.
    private const int UploadOverhead = 40, DownloadOverhead = 60;

    // An unreachable rate or interval, for a link that never limits anything.
    private const double Never = 1e12;

    public static readonly ConnectionProfile Perfect = new("perfect link",
        UploadRate: Never, SendBufferBytes: int.MaxValue / 2, UploadLatency: 0,
        UploadStallEvery: Never, UploadStallMin: 0, UploadStallMax: 0,
        DownloadRate: Never, DownloadLatency: 0, DownloadJitter: 0,
        DownloadStallEvery: Never, DownloadStallMin: 0, DownloadStallMax: 0,
        PeerHitchEvery: Never, PeerHitchMin: 0, PeerHitchMax: 0);

    private readonly List<Connection> connections = [];
    private readonly List<string> rejected = [];
    private long tick;

    public double Now { get; private set; }
    // Every client's clock: a run never reads the machine's, or how fast it ran would change it.
    public TimeProvider Time => field ??= new VirtualTime(this);
    public IReadOnlyList<string> Rejected => rejected;
    // The longest a host's message took from its send to a peer's queue.
    public double MaxHostDelay { get; private set; }

    // MultiplayerManager.RelayFactory: the n-th client to connect gets linkFor(n).
    public IRelayConnection Connect(string secret)
    {
        var connection = new Connection(this, secret, (uint)connections.Count + 1, linkFor(connections.Count), seed + 1000 * connections.Count);
        connections.Add(connection);
        return connection;
    }

    private sealed class VirtualTime(TestRelay relay) : TimeProvider
    {
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => (long)(relay.Now * TimeSpan.TicksPerSecond);
    }

    public void AdvanceTo(double time)
    {
        while (Now < time)
        {
            foreach (var connection in connections) connection.Upload(Now);
            foreach (var connection in connections) connection.Download(Now);
            Now = ++tick * Step;
        }
    }

    private void Forward(Connection sender, Outgoing outgoing, double at)
    {
        if (!sender.Joined) return;
        foreach (var recipient in connections)
        {
            if (ReferenceEquals(recipient, sender) || !recipient.Joined) continue;
            if (!sender.IsHost && !recipient.IsHost) continue;
            recipient.Arrive(sender, outgoing, at);
        }
    }

    private sealed class Outgoing(MpMessage message, byte[] body, bool compressed, double sentAt)
    {
        public readonly MpMessage Message = message;
        public readonly double SentAt = sentAt;
        public readonly byte[] Body = body;
        public readonly bool Compressed = compressed;
        public readonly TaskCompletionSource Completion = new();
        public int Unwritten = body.Length + UploadOverhead;
        public int Buffered;
        public bool Written => Unwritten == 0;
    }

    private sealed class Connection : IRelayConnection
    {
        private readonly TestRelay relay;
        private readonly ConnectionProfile link;
        private readonly Random race, swing, jitter;
        private readonly LinkStalls uploadStalls, downloadStalls;
        private readonly Queue<Outgoing> queued = new(), sendBuffer = new();
        private readonly Queue<(double At, Outgoing Message)> toRelay = new();
        private readonly Queue<(double At, Connection Sender, Outgoing Message)> arriving = new();
        private Outgoing? writing;
        private int buffered;
        private double uploadCredit, uploadShare = 1, nextSwing, downloadFreeAt, lastArrival;

        public readonly Guid PeerId;
        public readonly uint Id;
        public bool Joined, IsHost;

        public Connection(TestRelay relay, string secret, uint id, ConnectionProfile link, int seed)
        {
            this.relay = relay;
            this.link = link;
            PeerId = RelayWire.PeerId(secret);
            Id = id;
            race = new Random(seed);
            swing = new Random(seed + 1);
            jitter = new Random(seed + 2);
            uploadStalls = new LinkStalls(new Random(seed + 3), link.UploadStallEvery, link.UploadStallMin, link.UploadStallMax);
            downloadStalls = new LinkStalls(new Random(seed + 4), link.DownloadStallEvery, link.DownloadStallMin, link.DownloadStallMax);
        }

        public bool IsConnected => Joined;
        public bool IsEncrypted => false;
        public bool FellBackToUnencrypted => false;
        public bool SupportsCompression => true;
        public bool SupportsSenderIdentity => true;

        public event Action<MpMessage, bool, uint, Guid>? MessageReceived;
        public event Action<IReadOnlyList<Guid>>? PeersRemoved;
        public event Action<Exception?>? Disconnected;

        public Task ConnectAsync(string relayUrl, string sessionCode, string? accessToken = null)
        {
            if (sessionCode == SessionCode) Joined = true;
            else Disconnected?.Invoke(new RelaySessionRejectedException("session not found"));
            return Task.CompletedTask;
        }

        public Task<string?> ConnectAndHostAsync(string relayUrl, string? accessToken = null)
        {
            Joined = IsHost = true;
            return Task.FromResult<string?>(SessionCode);
        }

        // The send loop wakes on the first message queued, and races the rest of the frame's sends.
        public Task SendAsync(MpMessage message)
        {
            if (!Joined) return Task.CompletedTask;
            var (body, type) = RelayClient.Encode(message, compress: true);
            var outgoing = new Outgoing(message, body, type == WebSocketMessageType.Binary, relay.Now);
            if (writing == null && queued.Count == 0 && race.NextDouble() >= link.SendLoopLate) writing = outgoing;
            else queued.Enqueue(outgoing);
            return outgoing.Completion.Task;
        }

        public Task ModerateAsync(string operation, Guid peerId)
        {
            if (!IsHost || operation is not ("kick" or "ban")) return Task.CompletedTask;
            foreach (var target in relay.connections.Where(c => c.PeerId == peerId && c.Joined))
                target.Close();
            return Task.CompletedTask;
        }

        public void Dispose() => Joined = false;

        private void Close()
        {
            Joined = false;
            Disconnected?.Invoke(null);
        }

        public void Upload(double now)
        {
            WriteToSocket();
            if (now >= nextSwing)
            {
                uploadShare = 1 - link.UploadSwing + swing.NextDouble() * 2 * link.UploadSwing;
                nextSwing = now + link.UploadSwingEvery;
            }
            if (uploadStalls.Within(now) == null)
            {
                uploadCredit += Math.Min(link.UploadRate * uploadShare * Step, int.MaxValue);
                while (sendBuffer.Count > 0 && uploadCredit >= 1)
                {
                    var head = sendBuffer.Peek();
                    var bytes = (int)Math.Min(uploadCredit, head.Buffered);
                    head.Buffered -= bytes;
                    buffered -= bytes;
                    uploadCredit -= bytes;
                    if (head.Buffered > 0 || !head.Written) break;
                    sendBuffer.Dequeue();
                    toRelay.Enqueue((now + link.UploadLatency, head));
                }
                // An idle link can't save up bandwidth for later.
                if (sendBuffer.Count == 0) uploadCredit = Math.Min(uploadCredit, link.UploadRate * uploadShare * Step);
            }
            while (toRelay.Count > 0 && toRelay.Peek().At <= now)
                relay.Forward(this, toRelay.Dequeue().Message, now);
        }

        private void WriteToSocket()
        {
            while (true)
            {
                writing ??= queued.Count > 0 ? queued.Dequeue() : null;
                if (writing == null) return;
                var space = link.SendBufferBytes - buffered;
                if (space <= 0) return;
                if (writing.Buffered == 0 && !sendBuffer.Contains(writing)) sendBuffer.Enqueue(writing);
                var bytes = Math.Min(space, writing.Unwritten);
                writing.Unwritten -= bytes;
                writing.Buffered += bytes;
                buffered += bytes;
                if (!writing.Written) return;
                writing.Completion.TrySetResult();
                writing = null;
            }
        }

        public void Arrive(Connection sender, Outgoing outgoing, double at)
        {
            downloadFreeAt = Math.Max(downloadFreeAt, at) + (outgoing.Body.Length + DownloadOverhead) / link.DownloadRate;
            var arrival = Math.Max(lastArrival, downloadFreeAt + link.DownloadLatency + jitter.NextDouble() * link.DownloadJitter);
            if (downloadStalls.Within(arrival) is { } stallEnd) arrival = stallEnd;
            lastArrival = arrival;
            arriving.Enqueue((arrival, sender, outgoing));
        }

        public void Download(double now)
        {
            while (arriving.Count > 0 && arriving.Peek().At <= now)
            {
                var (_, sender, outgoing) = arriving.Dequeue();
                if (!Joined) continue;
                MpMessage? message;
                try
                {
                    message = RelayClient.Read(outgoing.Body, outgoing.Compressed, sender.IsHost, sender.PeerId, new TrafficBudget());
                }
                catch (Exception e)
                {
                    relay.rejected.Add($"{outgoing.Message.GetType().Name} from connection {sender.Id}: {e.Message}");
                    continue;
                }
                if (sender.IsHost) relay.MaxHostDelay = Math.Max(relay.MaxHostDelay, now - outgoing.SentAt);
                if (message != null) MessageReceived?.Invoke(message, sender.IsHost, sender.Id, sender.PeerId);
            }
        }
    }
}
