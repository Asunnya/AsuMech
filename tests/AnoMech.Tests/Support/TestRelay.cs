using System.Net.WebSockets;
using AnoMech.Multiplayer;
using AnoMech.Network;

namespace AnoMech.Tests;

// What a client's link can suffer: it drops, with all it carries, and reconnecting fails for
// Outage seconds; or its uploads (downloads) vanish for Dead seconds first, as a link does before
// anyone notices it's gone.
internal abstract record LinkFault(double Outage);
internal sealed record LinkDrops(double Outage) : LinkFault(Outage);
internal sealed record UploadsDie(double Dead, double Outage) : LinkFault(Outage);
internal sealed record DownloadsDie(double Dead, double Outage) : LinkFault(Outage);

// A message a client handed its link, From being the n-th client to connect. Resent: a numbered
// message that was on the wire before.
internal sealed record Carried(int From, double At, MpMessage Message, bool Resent)
{
    public string Kind => Message.GetType().Name + (Resent ? " sent again" : "");
}

// A relay and each client's link to it, in virtual time. A client's upload is RelayClient's: one
// queue in order, one message on the socket at a time, a send done once the socket buffer has
// taken all of it. The relay routes as the real one does, the host to everyone and a peer to the
// host only, stamping each message with its sender; it knows the host by identity, so a host that
// reconnects is the host again, and keeps nothing for a client that isn't connected. Each
// client's download delivers in order, through RelayClient's own decoding. A link dropping is
// told to its client during that client's own frame, as the real client hears it on its own.
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
    private readonly List<Guid> identities = [];
    private readonly Dictionary<Guid, LinkState> links = new();
    private readonly List<string> rejected = [];
    // By client: the n-th to connect, as linkFor numbers them.
    private readonly List<(double At, int Client, LinkFault Fault)> scheduled = [];
    private readonly List<(int Client, Func<Carried, bool> Match, LinkFault Fault)> afterSending = [];
    private readonly List<(int Client, Func<Carried, bool> Match, LinkFault Fault)> beforeReceiving = [];
    private readonly Dictionary<Guid, long> lastSeq = new();
    private Guid hostIdentity;
    private long tick;

    private sealed class LinkState
    {
        public double DownUntil = double.NegativeInfinity;
        public double UploadsDeadUntil = double.NegativeInfinity;
        public double DownloadsDeadUntil = double.NegativeInfinity;
    }

    public double Now { get; private set; }
    // Every client's clock: a run never reads the machine's, or how fast it ran would change it.
    public TimeProvider Time => field ??= new VirtualTime(this);
    public IReadOnlyList<string> Rejected => rejected;
    // The longest a host's message took from its send to a peer's queue.
    public double MaxHostDelay { get; private set; }
    // When each client's link went down and came back.
    public List<(int Client, double Down, double Up)> Outages { get; } = [];
    // Everything the clients handed their links, and what reached each, in order.
    public List<Carried> Sends { get; } = [];
    public List<(int To, Carried Carried)> Delivered { get; } = [];
    public int FaultsApplied { get; private set; }
    public int TriggersPending => afterSending.Count + beforeReceiving.Count;

    // MultiplayerManager.RelayFactory: the n-th client to connect gets linkFor(n), its
    // reconnections included.
    public IRelayConnection Connect(string secret)
    {
        var identity = RelayWire.PeerId(secret);
        if (!identities.Contains(identity)) identities.Add(identity);
        var connection = new Connection(this, identity, (uint)connections.Count + 1, linkFor(identities.IndexOf(identity)), seed + 1000 * connections.Count);
        connections.Add(connection);
        return connection;
    }

    public void Schedule(int client, double at, LinkFault fault) => scheduled.Add((at, client, fault));

    // The client's link drops right after it sends the first message `match` takes, which goes
    // with it.
    public void DropAfterSending(int client, Func<Carried, bool> match, LinkFault fault) => afterSending.Add((client, match, fault));

    // The client's link drops right as the first message `match` takes reaches it, which is lost.
    public void DropBeforeReceiving(int client, Func<Carried, bool> match, LinkFault fault) => beforeReceiving.Add((client, match, fault));

    // Within that client's frame (TestClient.Frame).
    public void RaiseDisconnects(Guid identity)
    {
        foreach (var connection in connections.Where(c => c.Identity == identity).ToList())
            connection.RaisePendingDisconnect();
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
            foreach (var fault in scheduled.Where(s => s.At <= Now).ToList())
            {
                scheduled.Remove(fault);
                if (fault.Client < identities.Count) Apply(identities[fault.Client], fault.Fault);
            }
            foreach (var connection in connections.ToList()) connection.Upload(Now);
            foreach (var connection in connections.ToList()) connection.Download(Now);
            Now = ++tick * Step;
        }
    }

    private LinkState LinkOf(Guid identity)
    {
        if (!links.TryGetValue(identity, out var link)) links[identity] = link = new LinkState();
        return link;
    }

    private void Apply(Guid identity, LinkFault fault)
    {
        FaultsApplied++;
        var link = LinkOf(identity);
        switch (fault)
        {
            case UploadsDie dies:
                link.UploadsDeadUntil = Now + dies.Dead;
                scheduled.Add((Now + dies.Dead, identities.IndexOf(identity), new LinkDrops(dies.Outage)));
                return;
            case DownloadsDie dies:
                link.DownloadsDeadUntil = Now + dies.Dead;
                scheduled.Add((Now + dies.Dead, identities.IndexOf(identity), new LinkDrops(dies.Outage)));
                return;
        }
        var live = connections.FirstOrDefault(c => c.Identity == identity && c.Joined);
        if (live == null) return;
        link.DownUntil = Now + fault.Outage;
        live.Drop();
        Outages.Add((identities.IndexOf(identity), Now, double.NaN));
    }

    private void Forward(Connection sender, Outgoing outgoing, double at)
    {
        if (!sender.Joined || at < LinkOf(sender.Identity).UploadsDeadUntil) return;
        foreach (var recipient in connections)
        {
            if (ReferenceEquals(recipient, sender) || !recipient.Joined) continue;
            if (!sender.IsHost && !recipient.IsHost) continue;
            recipient.Arrive(sender, outgoing, at);
        }
    }

    private Carried Record(Connection sender, MpMessage message)
    {
        var last = lastSeq.GetValueOrDefault(sender.Identity);
        var carried = new Carried(identities.IndexOf(sender.Identity), Now, message, message.Seq != 0 && message.Seq <= last);
        if (message.Seq > last) lastSeq[sender.Identity] = message.Seq;
        Sends.Add(carried);
        return carried;
    }

    private void Sent(Connection sender, Carried carried)
    {
        var client = identities.IndexOf(sender.Identity);
        var trigger = afterSending.FirstOrDefault(t => t.Client == client && t.Match(carried));
        if (trigger.Match == null) return;
        afterSending.Remove(trigger);
        Apply(sender.Identity, trigger.Fault);
    }

    // True when the delivery was the trigger, and is lost with the link.
    private bool Receiving(Connection recipient, Carried carried)
    {
        var client = identities.IndexOf(recipient.Identity);
        var trigger = beforeReceiving.FirstOrDefault(t => t.Client == client && t.Match(carried));
        if (trigger.Match == null) return false;
        beforeReceiving.Remove(trigger);
        Apply(recipient.Identity, trigger.Fault);
        return true;
    }

    private sealed class Outgoing(Carried carried, byte[] body, bool compressed, double sentAt)
    {
        public readonly Carried Carried = carried;
        public MpMessage Message => Carried.Message;
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
        private bool disconnectPending;

        public readonly Guid Identity;
        public readonly uint Id;
        public bool Joined;

        public Connection(TestRelay relay, Guid identity, uint id, ConnectionProfile link, int seed)
        {
            this.relay = relay;
            this.link = link;
            Identity = identity;
            Id = id;
            race = new Random(seed);
            swing = new Random(seed + 1);
            jitter = new Random(seed + 2);
            uploadStalls = new LinkStalls(new Random(seed + 3), link.UploadStallEvery, link.UploadStallMin, link.UploadStallMax);
            downloadStalls = new LinkStalls(new Random(seed + 4), link.DownloadStallEvery, link.DownloadStallMin, link.DownloadStallMax);
        }

        public bool IsHost => Identity == relay.hostIdentity;
        public bool IsConnected => Joined;
        public bool IsEncrypted => false;
        public bool FellBackToUnencrypted => false;
        public bool SupportsCompression => true;
        public bool SupportsSenderIdentity => true;

        public event Action<MpMessage, bool, uint, Guid>? MessageReceived;
        public event Action<IReadOnlyList<Guid>>? PeersRemoved;
        public event Action<Exception?>? Disconnected;

        // Fails while this identity's outage lasts; one connection per identity, as the relay
        // replaces a connection that hasn't noticed it's dead yet.
        public Task ConnectAsync(string relayUrl, string sessionCode, string? accessToken = null)
        {
            if (sessionCode != SessionCode)
            {
                Disconnected?.Invoke(new RelaySessionRejectedException("session not found"));
                return Task.CompletedTask;
            }
            var down = relay.LinkOf(Identity).DownUntil;
            if (relay.Now < down) return Task.CompletedTask;
            foreach (var stale in relay.connections.Where(c => c.Identity == Identity && c.Joined && !ReferenceEquals(c, this)).ToList())
                stale.Drop();
            Joined = true;
            var client = relay.identities.IndexOf(Identity);
            for (var i = relay.Outages.Count - 1; i >= 0; i--)
                if (relay.Outages[i].Client == client && double.IsNaN(relay.Outages[i].Up))
                {
                    relay.Outages[i] = relay.Outages[i] with { Up = relay.Now };
                    break;
                }
            return Task.CompletedTask;
        }

        public Task<string?> ConnectAndHostAsync(string relayUrl, string? accessToken = null)
        {
            relay.hostIdentity = Identity;
            Joined = true;
            return Task.FromResult<string?>(SessionCode);
        }

        // The send loop wakes on the first message queued, and races the rest of the frame's sends.
        public Task SendAsync(MpMessage message)
        {
            if (!Joined) return Task.CompletedTask;
            var carried = relay.Record(this, message);
            var (body, type) = RelayClient.Encode(message, compress: true);
            var outgoing = new Outgoing(carried, body, type == WebSocketMessageType.Binary, relay.Now);
            if (writing == null && queued.Count == 0 && race.NextDouble() >= link.SendLoopLate) writing = outgoing;
            else queued.Enqueue(outgoing);
            relay.Sent(this, carried);
            return outgoing.Completion.Task;
        }

        public Task ModerateAsync(string operation, Guid peerId)
        {
            if (!IsHost || operation is not ("kick" or "ban")) return Task.CompletedTask;
            foreach (var target in relay.connections.Where(c => c.Identity == peerId && c.Joined))
                target.Drop();
            return Task.CompletedTask;
        }

        public void Dispose() => Joined = false;

        // Everything on the link goes with it; a send still waiting for the socket fails, as
        // RelayClient's do once it's disposed.
        public void Drop()
        {
            Joined = false;
            disconnectPending = true;
            writing?.Completion.TrySetException(new ObjectDisposedException(nameof(RelayClient)));
            foreach (var waiting in queued) waiting.Completion.TrySetException(new ObjectDisposedException(nameof(RelayClient)));
            writing = null;
            queued.Clear();
            sendBuffer.Clear();
            toRelay.Clear();
            arriving.Clear();
            buffered = 0;
        }

        public void RaisePendingDisconnect()
        {
            if (!disconnectPending) return;
            disconnectPending = false;
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
            while (Joined && arriving.Count > 0 && arriving.Peek().At <= now)
            {
                var (_, sender, outgoing) = arriving.Dequeue();
                if (now < relay.LinkOf(Identity).DownloadsDeadUntil) continue;
                MpMessage? message;
                try
                {
                    message = RelayClient.Read(outgoing.Body, outgoing.Compressed, sender.IsHost, sender.Identity, new TrafficBudget());
                }
                catch (Exception e)
                {
                    relay.rejected.Add($"{outgoing.Message.GetType().Name} from connection {sender.Id}: {e.Message}");
                    continue;
                }
                if (message == null) continue;
                if (relay.Receiving(this, outgoing.Carried)) return;
                relay.Delivered.Add((relay.identities.IndexOf(Identity), outgoing.Carried));
                if (sender.IsHost) relay.MaxHostDelay = Math.Max(relay.MaxHostDelay, now - outgoing.SentAt);
                MessageReceived?.Invoke(message, sender.IsHost, sender.Id, sender.Identity);
            }
        }
    }
}
