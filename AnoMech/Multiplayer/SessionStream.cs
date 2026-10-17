using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AnoMech.Core;

namespace AnoMech.Multiplayer;

// Every message but an IUnsequencedMessage reaches the other end exactly once and in order,
// however either end's link drops in between. The sender numbers each one and keeps it until the
// other end has taken it; a receiver takes only the next number and asks for whatever it's
// missing. A peer's stream goes to the host alone; the host's goes to every peer, each taking it
// from where it joined. Decisions only: MultiplayerManager sends what these return.
internal sealed class SessionStream(Guid self, bool isHost, long epoch, Func<long> nowMs)
{
    // Over a minute of a run's traffic; a peer further behind left the run long before.
    public const int MaxLogged = 5_000;
    // An asked-for gap not filled by then is asked for again.
    public const long GapRetryMs = 1000;
    // A peer asking the host again sooner, for no later message, is ignored: each answer goes to
    // every peer.
    private const long MinResendIntervalMs = 500;

    private sealed class Remote
    {
        // Host: the peer's stream, from its Hello; a new one restarts its numbering.
        public long Epoch;
        // Peer: false until the host's StreamState says where its stream stands.
        public bool Started;
        public long Received;
        // Host: how far that peer has taken the host's stream.
        public long Acknowledged;
        public long GapFrom;
        public long GapAskedAt = long.MinValue;
        public long ResendFrom;
        public long ResentAt = long.MinValue;
    }

    // Each message as it was sent: one built on live state (a scenario's dictionary) would go
    // again as it is by then.
    private readonly Queue<(long Seq, byte[] Json)> log = new();
    private readonly Dictionary<Guid, Remote> remotes = new();
    private bool logFullWarned;

    public Guid Self => self;
    public bool IsHost => isHost;
    public long Epoch => epoch;
    public long Sent { get; private set; }
    public int Logged => log.Count;

    // Unsequenced messages pass through unchanged.
    public MpMessage Number(MpMessage message)
    {
        if (message is IUnsequencedMessage) return message;
        var numbered = message with { Seq = ++Sent, Epoch = isHost ? 0 : epoch };
        log.Enqueue((numbered.Seq, JsonSerializer.SerializeToUtf8Bytes(numbered)));
        if (isHost) Trim();
        if (log.Count > MaxLogged)
        {
            log.Dequeue();
            if (!logFullWarned)
                DiagnosticLog.Warn($"[Multiplayer] Stream log over {MaxLogged} messages -- the oldest are dropped; a client that far behind can't resume.");
            logFullWarned = true;
        }
        return numbered;
    }

    // Whether a numbered message from `sender` is the next one to take. A later one is dropped
    // and `ask` names the gap; an earlier one is a duplicate. A peer's message from a stream its
    // Hello hasn't announced waits for that Hello.
    public bool Take(Guid sender, MpMessage message, out MpMessage? ask)
    {
        ask = null;
        if (!remotes.TryGetValue(sender, out var remote) || !remote.Started || message.Seq <= remote.Received) return false;
        if (isHost && message.Epoch != remote.Epoch) return false;
        if (message.Seq == remote.Received + 1)
        {
            remote.Received = message.Seq;
            return true;
        }
        ask = AskGap(sender, remote);
        return false;
    }

    // ── Peer ─────────────────────────────────────────────────────────────────

    // Received: -1 until the host's stream has started here, so the host reads a fresh join.
    public (long Epoch, long Sent, long Received) HelloFields(Guid host)
        => (epoch, Sent, remotes.TryGetValue(host, out var remote) && remote.Started ? remote.Received : -1);

    // The host's answer to a Hello: a fresh join starts at the host's latest message, and
    // whatever of ours the host hasn't taken goes again. One answering an earlier stream's Hello,
    // from before we joined again, says nothing about this one.
    public IReadOnlyList<MpMessage> State(Guid host, long streamEpoch, long hostSent, long hostReceived)
    {
        if (streamEpoch != epoch) return [];
        var remote = RemoteOf(host);
        if (!remote.Started)
        {
            remote.Started = true;
            remote.Received = hostSent;
        }
        Acknowledge(hostReceived);
        return hostReceived < Sent ? Resend(host, hostReceived + 1) : [];
    }

    public (long Epoch, long Sent, long Received) PongFields(Guid host)
        => (epoch, Sent, remotes.TryGetValue(host, out var remote) && remote.Started ? remote.Received : -1);

    // A message the host sent last is missing here when its Ping says more went out.
    public MpMessage? Ping(Guid host, long hostSent, StreamCursor? ours)
    {
        if (ours is { } cursor && cursor.Epoch == epoch) Acknowledge(cursor.Received);
        return remotes.TryGetValue(host, out var remote) && remote.Started && hostSent > remote.Received ? AskGap(host, remote) : null;
    }

    // ── Host ─────────────────────────────────────────────────────────────────

    // A peer's stream restarts with a new epoch: it left and joined again.
    public StreamStateMessage Hello(Guid peer, long peerEpoch, long peerReceived)
    {
        if (!remotes.TryGetValue(peer, out var remote) || remote.Epoch != peerEpoch)
            remotes[peer] = remote = new Remote { Epoch = peerEpoch, Started = true, Acknowledged = Sent };
        if (peerReceived >= 0) remote.Acknowledged = Math.Max(remote.Acknowledged, peerReceived);
        Trim();
        return new StreamStateMessage(peer, peerEpoch, Sent, remote.Received);
    }

    public (long Sent, List<StreamCursor> Received) PingFields()
        => (Sent, remotes.Select(r => new StreamCursor(r.Key, r.Value.Epoch, r.Value.Received)).ToList());

    // A message the peer sent last is missing here when its Pong says more went out; a Pong from a
    // stream this host hasn't had the Hello of yet says nothing about the one it has.
    public MpMessage? Pong(Guid peer, long peerEpoch, long peerSent, long peerReceived)
    {
        if (!remotes.TryGetValue(peer, out var remote)) return null;
        remote.Acknowledged = Math.Max(remote.Acknowledged, peerReceived);
        Trim();
        return peerEpoch == remote.Epoch && peerSent > remote.Received ? AskGap(peer, remote) : null;
    }

    // The peer left: nothing it hasn't taken is kept for it.
    public void Forget(Guid peer)
    {
        remotes.Remove(peer);
        Trim();
    }

    // ── Both ─────────────────────────────────────────────────────────────────

    // What `requester` asked for again, in order, or that it's gone.
    public IReadOnlyList<MpMessage> Resend(Guid requester, long from)
    {
        if (from > Sent) return [];
        if (isHost && remotes.TryGetValue(requester, out var remote))
        {
            var now = nowMs();
            if (from <= remote.ResendFrom && now - remote.ResentAt < MinResendIntervalMs) return [];
            remote.ResendFrom = from;
            remote.ResentAt = now;
        }
        if (log.Count == 0 || log.Peek().Seq > from)
            return [new StreamLostMessage(isHost ? requester : self, log.Count > 0 ? log.Peek().Seq : Sent + 1) { Epoch = isHost ? 0 : epoch }];
        return log.Where(m => m.Seq >= from).Select(m => JsonSerializer.Deserialize<MpMessage>(m.Json)!).ToList();
    }

    // The other end no longer has what we asked for: we go on from `next`. The host only hears
    // this from a peer it asked, about the stream it has.
    public void Lost(Guid sender, long next, long senderEpoch = 0)
    {
        if (isHost && (!remotes.TryGetValue(sender, out var known) || known.Epoch != senderEpoch)) return;
        var remote = RemoteOf(sender);
        remote.Started = true;
        remote.Received = Math.Max(remote.Received, next - 1);
    }

    private Remote RemoteOf(Guid id)
    {
        if (!remotes.TryGetValue(id, out var remote)) remotes[id] = remote = new Remote();
        return remote;
    }

    private MpMessage? AskGap(Guid sender, Remote remote)
    {
        var from = remote.Received + 1;
        var now = nowMs();
        if (from == remote.GapFrom && now - remote.GapAskedAt < GapRetryMs) return null;
        remote.GapFrom = from;
        remote.GapAskedAt = now;
        return new StreamGapMessage(isHost ? sender : self, from);
    }

    private void Acknowledge(long through)
    {
        while (log.Count > 0 && log.Peek().Seq <= through) log.Dequeue();
    }

    // The host keeps what any peer it knows hasn't taken yet.
    private void Trim() => Acknowledge(remotes.Count == 0 ? Sent : remotes.Values.Min(r => r.Acknowledged));
}
