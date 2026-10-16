namespace AnoMech.Core.Game;

// Ordered: everything from Running on has a run, so callers compare with >= Running.
public enum SessionStatus
{
    Offline,
    InZone,
    Running,
    // A death was reported but nobody went down: godmode.
    RunningWithMistake,
    // Someone went down; the run plays on until the freeze 5s later.
    Failed,
    Frozen,
    CompletedWithMistake,
    Succeeded,
}
