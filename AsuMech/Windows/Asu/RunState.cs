using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Scenarios;

namespace AsuMech.Windows.Asu;

// What the run is doing, read the same way MainWindow reads it.
internal static class RunState
{
    // In a session the run counts from the start handshake, before Game has one.
    public static bool Active => Plugin.Multiplayer.SessionCode != null ? Plugin.Multiplayer.IsRunning : Plugin.Game.Status >= SessionStatus.Running;

    public static bool InSession => Plugin.Multiplayer.InSession;
    public static bool IsGuest => Plugin.Multiplayer.SessionCode != null && !Plugin.Multiplayer.IsHost;

    // Still playing, as opposed to frozen or finished and only left standing.
    public static bool Live(Game game)
        => game.Status is SessionStatus.Running or SessionStatus.RunningWithMistake or SessionStatus.Failed;

    public static bool IsFailed(SessionStatus status)
        => status is SessionStatus.Failed or SessionStatus.Frozen or SessionStatus.CompletedFailed;

    // The live run's state, else how the selected scenario's last run ended.
    public static (string Label, Vector4 Color) Status(Game game, IScenario? selected)
    {
        var idle = ("Idle", AsuTheme.Muted);
        if (game.LastRun is not { } run) return Active ? ("Running", AsuTheme.Ok) : idle;
        if (run.Ended && run.Scenario != selected) return idle;
        var status = run.Status;
        if (IsFailed(status)) return ("Failed", AsuTheme.Bad);
        return status switch
        {
            SessionStatus.Succeeded => ("Cleared", AsuTheme.Ok),
            SessionStatus.CompletedWithMistake => ("Cleared with mistakes", AsuTheme.Accent),
            _ when run.Ended => ("Stopped", AsuTheme.Muted),
            SessionStatus.RunningWithMistake => ("Running with mistakes", AsuTheme.Accent),
            _ => ("Running", AsuTheme.Ok),
        };
    }
}
