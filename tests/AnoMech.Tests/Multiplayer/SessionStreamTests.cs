using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;

namespace AnoMech.Tests;

// SessionStream's promise, whatever happens to the links: every numbered message the host or a
// peer sends is taken by the other end exactly once and in order, and the logs that make that
// possible empty once everyone has caught up. The host and its peers talk through a simulated
// relay that routes as the real one does and keeps nothing for a client that isn't there, and
// each end drives its stream as MultiplayerManager does (Hello on every connect, the ping cycle,
// gap requests, resends). Each seed draws its own traffic and its own interruptions: a link
// dropping and reconnecting on the real backoff, an upload or a download dying before the link
// does, an inbound queue overflowing, a peer leaving and joining again -- on either end, at any
// moment, several at once. No case names a message or a moment, so anything sent through the
// stream later is covered as it is.
public class SessionStreamTests
{
    private static IEnumerable<TestCaseData> Seeds() =>
        Enumerable.Range(0, 500).Select(seed => new TestCaseData(seed).SetArgDisplayNames($"seed {seed}"));

    [TestCaseSource(nameof(Seeds))]
    public void EveryNumberedMessageIsTakenOnceAndInOrderWhateverTheLinksDo(int seed)
    {
        var net = new StreamNet(seed);
        net.Run();
        foreach (var line in net.Log.TakeLast(30)) TestContext.Out.WriteLine(line);
        Assert.That(net.Problems(), Is.Empty);
    }

    private sealed class StreamNet
    {
        // MultiplayerManager's own: PingIntervalSeconds, HelloRetrySeconds, ReconnectBackoff.
        private const long PingIntervalMs = 2000;
        private const long HelloRetryMs = 2000;
        private static readonly long[] BackoffMs = [2000, 4000, 8000, 15000];
        private const long StepMs = 5;
        private const long SettleMs = 60_000;

        private readonly Random rng;
        private readonly List<End> ends = [];
        private readonly List<(long At, End To, End From, int Generation, MpMessage Message)> arriving = [];
        private readonly List<(long At, End From, int Generation, MpMessage Message)> uploading = [];
        private readonly List<string> problems = [];
        private readonly long activeMs;
        private long now;
        private long nextPing;

        public List<string> Log { get; } = [];

        private End Host => ends[0];

        private sealed class End(Guid id, bool isHost)
        {
            public readonly Guid Id = id;
            public readonly bool IsHost = isHost;
            public SessionStream Stream = null!;
            public bool Member = true;
            public bool Connected;
            public int Generation;
            public long DownUntil;
            public long? ReconnectAt;
            public int ReconnectAttempt;
            public long UplinkDeadUntil = -1, DownlinkDeadUntil = -1;
            public long? DropAt;
            public long? LeaveAt, JoinAt;
            public long InboundDropUntil = -1;
            public bool HelloAcknowledged;
            public long NextHello;
            public long Baseline = -1;
            public double Rate;
            public long UpLatency, DownLatency, LastUpload, LastDownload;
            public long NextFault = long.MaxValue;
            // This stream's numbered messages, and what it took from each other end's current one.
            public readonly List<MpMessage> Sent = [];
            public readonly Dictionary<Guid, List<MpMessage>> Taken = new();
            public readonly Dictionary<Guid, long> EpochTaken = new();
        }

        public StreamNet(int seed)
        {
            rng = new Random(seed);
            activeMs = rng.Next(40_000, 120_000);
            var peers = rng.Next(1, 8);
            for (var i = 0; i <= peers; i++)
            {
                var end = new End(Guid.NewGuid(), i == 0)
                {
                    UpLatency = rng.Next(1, 150),
                    DownLatency = rng.Next(1, 150),
                };
                end.Stream = NewStream(end);
                end.Rate = NextRate(end);
                end.NextFault = NextFaultAt(end);
                ends.Add(end);
            }
            // The host first; each peer joins a moment later.
            Host.Connected = true;
            foreach (var peer in ends.Skip(1)) peer.JoinAt = rng.Next(0, 3000);
        }

        private SessionStream NewStream(End end) => new(end.Id, end.IsHost, rng.NextInt64(1, long.MaxValue), () => now);

        private double NextRate(End end) => rng.NextDouble() < 0.3 ? 0 : end.IsHost ? rng.Next(1, 61) : rng.Next(0, 6);

        private long NextFaultAt(End end) => now + (long)(-Math.Log(1 - rng.NextDouble()) * rng.Next(6_000, 20_000));

        public void Run()
        {
            var until = activeMs + SettleMs;
            while (now < until)
            {
                var active = now < activeMs;
                foreach (var end in ends) TickEnd(end, active);
                if (Host.Connected && now >= nextPing)
                {
                    nextPing = now + PingIntervalMs;
                    var (sent, received) = Host.Stream.PingFields();
                    Transmit(Host, new PingMessage(now, sent, received));
                }
                Deliver();
                if (!active && CaughtUp()) break;
                now += StepMs;
            }
        }

        private void TickEnd(End end, bool active)
        {
            if (end.JoinAt is { } joinAt && now >= joinAt) Join(end);
            if (!end.Member) return;
            if (end.LeaveAt is { } leaveAt && now >= leaveAt) Leave(end);
            if (end.DropAt is { } dropAt && now >= dropAt) Drop(end, rng.Next(0, 2000));
            if (!end.Connected && end.ReconnectAt is { } reconnectAt && now >= reconnectAt) TryReconnect(end);
            if (!end.IsHost && end.Connected && !end.HelloAcknowledged && now >= end.NextHello) SendHello(end);
            if (!active) return;
            if (now >= end.NextFault) Fault(end);
            if (rng.NextDouble() < 0.0005) end.Rate = NextRate(end);
            if (rng.NextDouble() < end.Rate * StepMs / 1000.0)
                Send(end, end.IsHost
                    ? new AnnouncementMessage($"{end.Stream.Sent + 1}")
                    : new ClaimRoleMessage(end.Id, (PartyRole)rng.Next(8)));
            // Never numbered, never kept: it rides along and must not disturb anything.
            if (!end.IsHost && rng.NextDouble() < 0.02) Transmit(end, new SelfPoseMessage(end.Id, 0, 0, 0, 0));
        }

        // ── Interruptions ────────────────────────────────────────────────────

        private void Fault(End end)
        {
            end.NextFault = NextFaultAt(end);
            var kind = rng.NextDouble();
            if (kind < 0.4)
            {
                Note($"{Name(end)} drops");
                Drop(end, rng.Next(0, 6000));
            }
            else if (kind < 0.6)
            {
                end.UplinkDeadUntil = now + rng.Next(100, 5000);
                end.DropAt = end.UplinkDeadUntil;
                Note($"{Name(end)}'s uploads die until {end.UplinkDeadUntil}");
            }
            else if (kind < 0.8)
            {
                end.DownlinkDeadUntil = now + rng.Next(100, 5000);
                end.DropAt = end.DownlinkDeadUntil;
                Note($"{Name(end)}'s downloads die until {end.DownlinkDeadUntil}");
            }
            else if (kind < 0.95 || end.IsHost)
            {
                end.InboundDropUntil = now + rng.Next(10, 500);
                Note($"{Name(end)}'s inbound queue overflows until {end.InboundDropUntil}");
            }
            else
            {
                Note($"{Name(end)} leaves");
                end.LeaveAt = now;
            }
        }

        // The link goes with everything on it; reconnecting fails until the outage is over.
        private void Drop(End end, long outage)
        {
            end.DropAt = null;
            if (!end.Connected) return;
            end.Connected = false;
            end.Generation++;
            end.DownUntil = now + outage;
            end.ReconnectAttempt = 0;
            end.ReconnectAt = now + BackoffMs[0];
        }

        private void TryReconnect(End end)
        {
            if (now < end.DownUntil)
            {
                end.ReconnectAttempt++;
                end.ReconnectAt = now + BackoffMs[Math.Min(end.ReconnectAttempt, BackoffMs.Length - 1)];
                return;
            }
            Connect(end);
            Note($"{Name(end)} reconnects");
            if (!end.IsHost) SendHello(end);
        }

        private void Connect(End end)
        {
            end.Connected = true;
            end.Generation++;
            end.ReconnectAt = null;
            end.UplinkDeadUntil = end.DownlinkDeadUntil = -1;
        }

        // As LeaveSession: its leave goes out on the stream, then the link closes. Back later, as
        // a new member with a new stream.
        private void Leave(End end)
        {
            end.LeaveAt = null;
            Send(end, new SessionEndedMessage(end.Id));
            end.Member = false;
            end.JoinAt = now + rng.Next(1000, 5000);
        }

        private void Join(End end)
        {
            end.JoinAt = null;
            end.Member = true;
            end.Connected = false;
            end.Generation++;
            end.Stream = NewStream(end);
            end.Sent.Clear();
            end.Taken.Clear();
            end.Baseline = -1;
            end.HelloAcknowledged = false;
            Connect(end);
            Note($"{Name(end)} joins");
            SendHello(end);
        }

        // ── The relay ────────────────────────────────────────────────────────

        private void Send(End end, MpMessage message)
        {
            var numbered = end.Stream.Number(message);
            if (numbered.Seq != 0) end.Sent.Add(numbered);
            Transmit(end, numbered);
        }

        private void Transmit(End end, MpMessage message)
        {
            if (!end.Connected || now < end.UplinkDeadUntil) return;
            end.LastUpload = Math.Max(end.LastUpload, now + end.UpLatency + rng.Next(0, 20));
            uploading.Add((end.LastUpload, end, end.Generation, message));
        }

        private void Transmit(End end, IEnumerable<MpMessage> messages)
        {
            foreach (var message in messages) Transmit(end, message);
        }

        private void SendHello(End end)
        {
            end.NextHello = now + HelloRetryMs;
            var (epoch, sent, received) = end.Stream.HelloFields(Host.Id);
            Transmit(end, new HelloMessage(end.Id, "", "", "", 0, epoch, sent, received));
        }

        // The relay routes what reached it: the host's to every connected peer, a peer's to the host.
        private void Deliver()
        {
            foreach (var up in uploading.Where(u => u.At <= now).ToList())
            {
                uploading.Remove(up);
                if (up.From.Generation != up.Generation) continue;
                foreach (var to in ends)
                {
                    if (to == up.From || !to.Member || !to.Connected) continue;
                    if (!up.From.IsHost && !to.IsHost) continue;
                    to.LastDownload = Math.Max(to.LastDownload, now + to.DownLatency + rng.Next(0, 20));
                    arriving.Add((to.LastDownload, to, up.From, to.Generation, up.Message));
                }
            }
            foreach (var down in arriving.Where(a => a.At <= now).OrderBy(a => a.At).ToList())
            {
                arriving.Remove(down);
                var to = down.To;
                if (to.Generation != down.Generation || now < to.DownlinkDeadUntil) continue;
                if (now < to.InboundDropUntil) continue;
                Handle(to, down.From, down.Message);
            }
        }

        // MultiplayerManager's drain and Dispatch, for what concerns the stream.
        private void Handle(End to, End from, MpMessage message)
        {
            if (message.Seq != 0)
            {
                if (!to.Stream.Take(from.Id, message, out var ask))
                {
                    if (ask != null) Transmit(to, ask);
                    return;
                }
                if (!to.Taken.TryGetValue(from.Id, out var taken)) to.Taken[from.Id] = taken = [];
                taken.Add(message);
                if (to.IsHost && message is SessionEndedMessage) to.Stream.Forget(from.Id);
                return;
            }
            switch (message)
            {
                case HelloMessage hello when to.IsHost:
                    // A new epoch is a new stream: what came before it is the old one's.
                    if (to.EpochTaken.GetValueOrDefault(from.Id) != hello.StreamEpoch)
                    {
                        to.EpochTaken[from.Id] = hello.StreamEpoch;
                        to.Taken[from.Id] = [];
                    }
                    Transmit(to, to.Stream.Hello(hello.PeerId, hello.StreamEpoch, hello.StreamReceived));
                    break;
                case StreamStateMessage state when !to.IsHost && state.PeerId == to.Id:
                    if (state.StreamEpoch == to.Stream.Epoch)
                    {
                        to.HelloAcknowledged = true;
                        if (to.Baseline < 0) to.Baseline = state.Sent;
                    }
                    Transmit(to, to.Stream.State(from.Id, state.StreamEpoch, state.Sent, state.Received));
                    break;
                case PingMessage ping when !to.IsHost:
                {
                    var (epoch, sent, received) = to.Stream.PongFields(from.Id);
                    Transmit(to, new PongMessage(to.Id, ping.SentAtMs, epoch, sent, received));
                    var ours = ping.StreamReceived?.FirstOrDefault(c => c.PeerId == to.Id);
                    if (to.Stream.Ping(from.Id, ping.StreamSent, ours) is { } ask) Transmit(to, ask);
                    break;
                }
                case PongMessage pong when to.IsHost:
                    if (to.Stream.Pong(pong.PeerId, pong.StreamEpoch, pong.StreamSent, pong.StreamReceived) is { } askPeer) Transmit(to, askPeer);
                    break;
                case StreamGapMessage gap when to.IsHost:
                    Transmit(to, to.Stream.Resend(gap.PeerId, gap.From));
                    break;
                case StreamGapMessage gap when gap.PeerId == to.Id:
                    Transmit(to, to.Stream.Resend(from.Id, gap.From));
                    break;
                case StreamLostMessage lost when to.IsHost || lost.PeerId == to.Id:
                    problems.Add($"{Name(to)} was told {Name(from)} no longer has #{lost.Next - 1} and before");
                    to.Stream.Lost(to.IsHost ? lost.PeerId : from.Id, lost.Next, lost.Epoch);
                    break;
            }
        }

        // ── Checks ───────────────────────────────────────────────────────────

        private bool CaughtUp()
            => ends.All(e => !e.Member || e.Connected && (e.IsHost || e.Baseline >= 0))
               && Members().All(p => TakenFrom(p, Host).Count == Host.Sent.Count(m => m.Seq > p.Baseline)
                                     && TakenFrom(Host, p).Count == p.Sent.Count)
               && Host.Stream.Logged == 0 && Members().All(p => p.Stream.Logged == 0);

        private IEnumerable<End> Members() => ends.Skip(1).Where(p => p.Member);

        private static List<MpMessage> TakenFrom(End to, End from) => to.Taken.GetValueOrDefault(from.Id) ?? [];

        public List<string> Problems()
        {
            var all = new List<string>(problems);
            foreach (var peer in Members())
            {
                if (!peer.Connected || peer.Baseline < 0)
                {
                    all.Add($"{Name(peer)} never got back in");
                    continue;
                }
                Compare(all, Host, peer, Host.Sent.Where(m => m.Seq > peer.Baseline).ToList(), TakenFrom(peer, Host));
                Compare(all, peer, Host, peer.Sent, TakenFrom(Host, peer));
                if (peer.Stream.Logged > 0) all.Add($"{Name(peer)} still keeps {peer.Stream.Logged} messages the host has");
            }
            if (Host.Stream.Logged > 0) all.Add($"the host still keeps {Host.Stream.Logged} messages every peer has");
            return all;
        }

        private void Compare(List<string> all, End from, End to, List<MpMessage> sent, List<MpMessage> taken)
        {
            for (var i = 0; i < Math.Max(sent.Count, taken.Count); i++)
            {
                var expected = i < sent.Count ? sent[i] : null;
                var got = i < taken.Count ? taken[i] : null;
                if (Equals(expected, got)) continue;
                all.Add($"{Name(to)} took {Describe(got)} as {Name(from)}'s message #{i + 1} of its stream, which is {Describe(expected)} ({sent.Count} sent, {taken.Count} taken)");
                return;
            }
        }

        private static string Describe(MpMessage? message) => message == null ? "nothing" : $"#{message.Seq} {message.GetType().Name}";

        private string Name(End end) => end.IsHost ? "the host" : $"peer {ends.IndexOf(end)}";

        private void Note(string what) => Log.Add($"{now,7} {what}");
    }
}
