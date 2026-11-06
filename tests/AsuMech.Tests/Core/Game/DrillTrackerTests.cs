using System.Collections.Generic;
using AsuMech.Core.Game;
using NUnit.Framework;

namespace AsuMech.Tests.Core.Game;

public class DrillTrackerTests
{
    private Dictionary<string, int> best = null!;
    private int saves;
    private DrillTracker drill = null!;

    [SetUp]
    public void SetUp()
    {
        best = [];
        saves = 0;
        drill = new DrillTracker(best, () => saves++);
    }

    [Test]
    public void ReachingTheTargetStopsTheLoopAndRaisesOnce()
    {
        var reached = new List<int>();
        drill.TargetReached += (_, n) => reached.Add(n);
        drill.StreakTarget = 3;
        Assert.That(drill.RecordFinish("s", clean: true, streakBefore: 1, counting: true), Is.False);
        Assert.That(drill.RecordFinish("s", clean: true, streakBefore: 2, counting: true), Is.True);
        Assert.That(reached, Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void BestOnlyGoesUp()
    {
        drill.RecordFinish("s", true, 4, true);
        drill.RecordFinish("s", true, 0, true);
        Assert.That(drill.BestOf("s"), Is.EqualTo(5));
        Assert.That(saves, Is.EqualTo(1));
    }

    [Test]
    public void UncountedOrFailedRunsChangeNothing()
    {
        drill.StreakTarget = 1;
        Assert.That(drill.RecordFinish("s", clean: true, streakBefore: 9, counting: false), Is.False);
        Assert.That(drill.RecordFinish("s", clean: false, streakBefore: 9, counting: true), Is.False);
        Assert.That(best, Is.Empty);
    }

    [Test]
    public void TargetCyclesThroughOff()
    {
        var seen = new List<int?>();
        for (var i = 0; i < 4; i++)
        {
            drill.CycleTarget();
            seen.Add(drill.StreakTarget);
        }
        Assert.That(seen, Is.EqualTo(new int?[] { 3, 5, 10, null }));
    }
}
