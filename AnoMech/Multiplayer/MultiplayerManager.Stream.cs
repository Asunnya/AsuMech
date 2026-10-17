using System;
using System.Threading.Tasks;
using AnoMech.Core;

namespace AnoMech.Multiplayer;

public sealed partial class MultiplayerManager
{
    // ---- The session's stream (SessionStream) --------------------------------

    private SessionStream? stream;

    // A numbered message as it first leaves, and one taken in order from its sender.
    internal event Action<MpMessage>? StreamSent;
    internal event Action<Guid, MpMessage>? StreamTaken;

    private void OpenStream(bool host)
        => stream = new SessionStream(MyPeerId, host, Random.Shared.NextInt64(1, long.MaxValue), () => NowMs);

    // Numbered and kept until the other end has it, so one sent while the link is down goes out
    // once it's back.
    private Task Send(MpMessage message)
    {
        if (stream != null)
        {
            message = stream.Number(message);
            if (message.Seq != 0) StreamSent?.Invoke(message);
        }
        return relay is { IsConnected: true } client ? client.SendAsync(message) : Task.CompletedTask;
    }

    // As it is: a stream answer, or a message going out again.
    private void Transmit(MpMessage message)
    {
        if (relay is { IsConnected: true } client) _ = client.SendAsync(message);
    }

    private void Transmit(System.Collections.Generic.IEnumerable<MpMessage> messages)
    {
        foreach (var message in messages) Transmit(message);
    }

    // A numbered message is dispatched only as the next one from its sender.
    private bool TakeFromStream(MpMessage message, Guid sender)
    {
        if (message.Seq == 0) return true;
        if (stream == null) return false;
        if (!stream.Take(sender, message, out var ask))
        {
            if (ask != null) Transmit(ask);
            return false;
        }
        StreamTaken?.Invoke(sender, message);
        return true;
    }

    private HelloMessage NewHello(byte classJob)
    {
        var (epoch, sent, received) = stream?.HelloFields(Session.HostId) ?? (0, 0, -1);
        return new HelloMessage(MyPeerId, DisplayName, PluginBuildInfo.Version, PluginBuildInfo.Checksum, classJob, epoch, sent, received);
    }

    // Behind the host's stream for good: what this client shows of the run can't be trusted any
    // more, so it leaves the run and waits for the next.
    private void OnHostStreamLost(Guid host, long next)
    {
        stream?.Lost(host, next);
        DiagnosticLog.Warn($"[Multiplayer] The host no longer has the messages this client missed -- going on from #{next}.");
        if (running) OnEndReceived(new EndMessage(ReturnedToInn: true, "Lost the host's messages after a connection drop."));
    }
}
