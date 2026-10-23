namespace AnoMech.Core.Game;

// Ordered: everything from Running on has a run, so callers compare with >= Running.
public enum SessionStatus
{
    Offline,
    InZone,
    Running,
    // A death was reported but nobody went down: godmode.
    RunningWithMistake,
    // Someone went down or a mechanic failed; the run plays on, to a freeze if one was asked for.
    Failed,
    // Only a failed run freezes.
    Frozen,
    CompletedWithMistake,
    CompletedFailed,
    Succeeded,
}
