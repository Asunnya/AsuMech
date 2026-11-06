using System;
using System.Collections.Generic;

namespace AsuMech.Core.Game;

// Best clean streak per scenario, and an optional streak target that ends Auto-restart.
// Hears a finish before PracticeLoop does, so it can stop the loop before the next restart.
public sealed class DrillTracker
{
    public static readonly IReadOnlyList<int> Targets = [3, 5, 10];

    private readonly IDictionary<string, int> best;
    private readonly Action save;

    public DrillTracker(IDictionary<string, int> best, Action save)
    {
        this.best = best;
        this.save = save;
    }

    public int? StreakTarget { get; set; }

    // Scenario full name, streak reached.
    public event Action<string, int>? TargetReached;

    public int BestOf(string scenario) => best.TryGetValue(scenario, out var n) ? n : 0;

    // streakBefore is PracticeLoop's streak before this finish is counted. True when the loop must stop.
    public bool RecordFinish(string scenario, bool clean, int streakBefore, bool counting)
    {
        if (!clean || !counting) return false;
        var streak = streakBefore + 1;
        if (streak > BestOf(scenario))
        {
            best[scenario] = streak;
            save();
        }
        if (StreakTarget is not { } target || streak < target) return false;
        TargetReached?.Invoke(scenario, streak);
        return true;
    }

    public void CycleTarget()
    {
        var i = StreakTarget is { } t ? IndexOf(t) : -1;
        StreakTarget = i + 1 < Targets.Count ? Targets[i + 1] : null;
    }

    private static int IndexOf(int target)
    {
        for (var i = 0; i < Targets.Count; i++)
            if (Targets[i] == target) return i;
        return -1;
    }
}
