using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Core.UserActions;
using AnoMech.Learn;
using AnoMech.Learn.Ghost;
using AnoMech.Learn.Tools;

namespace AnoMech.Tests;

public class StaticDiffTests
{
    private static GhostTrack Track(params (float T, float X, float Z)[] tank)
    {
        var lane = new GhostLane("player 1", PartyRole.MainTank, (byte)JobId.Warrior, tank.Select(p => new GhostSample(p.T, new Vector2(p.X, p.Z), 0f)).ToList());
        var track = new GhostTrack([lane], [], end: 100f);
        track.AlignAt(0f);
        return track;
    }

    private static DumpedOrder Order(float time, int seed, float x, float z, float? arrival = null)
        => new("Ai", time, "Ai.cs:" + (int)time, null, "MainTank", seed, time, arrival, x, z);

    [Test]
    public void EachCallIsJudgedWhereTheSeatSettlesAgainstTheNearestSeedsSpot()
    {
        var track = Track((0f, 0f, 0f), (9.8f, 0f, 0f), (10.2f, 5f, 0f), (19.95f, 5.2f, 0f), (20.5f, -6f, 6f), (40f, -6f, 6f));
        var orders = new List<DumpedOrder>
        {
            Order(10f, 1, 5f, 0f), Order(10f, 2, -5f, 0f),        // two rolls, two spots; the real one took the east one
            Order(20f, 1, 0f, -8f, arrival: 25f), Order(20f, 2, 0f, -8f, arrival: 25f),
        };
        var lesson = new Lesson("X", "s", [], [new LessonStep(new LessonStepKey("Ai", Time: 10f), "Bait"), new LessonStep(new LessonStepKey("Ai", Time: 20f), "Stack", Tolerance: 2f)]);
        var rows = StaticDiff.Compare(track, lesson, orders, [PartyRole.MainTank, PartyRole.OffTank]);
        Assert.That(rows.Select(r => (r.Title, r.Differs)), Is.EqualTo(new[] { ("Bait", false), ("Stack", true) }), "no lane for the off tank: no rows");
        var bait = rows[0];
        Assert.That(bait.RollDependent, "two seeds, spots 10 y apart: the call follows a roll");
        Assert.That(rows[1].RollDependent, Is.False);
        Assert.That(bait.At, Is.EqualTo(19.9f).Within(0.001f), "just before the next order");
        Assert.That(bait.NearestSpot, Is.EqualTo(new Vector2(5f, 0f)), "the seed whose roll the real pull matched");
        Assert.That(bait.Distance, Is.EqualTo(0f).Within(0.01f), "standing on it through the long still gap");
        var stack = rows[1];
        Assert.That(stack.At, Is.EqualTo(25f), "the deadline");
        Assert.That(stack.Distance, Is.EqualTo(MathF.Sqrt(36f + 196f)).Within(0.01f));
        var report = StaticDiff.Report(rows);
        Assert.That(report, Does.Contain("MainTank: 1 of 2 steps differ, 0 unsure"));
        Assert.That(report, Does.Contain("Your static does these differently:"));
        Assert.That(report, Does.Contain("25.0s Stack [Ai:20]: MainTank"));
    }

    [Test]
    public void AMissOnARollDependentCallIsUnsureNotADifference()
    {
        var track = Track((0f, 0f, 10f), (30f, 0f, 10f));
        var orders = new List<DumpedOrder> { Order(10f, 1, 5f, 0f), Order(10f, 2, -5f, 0f) };
        var row = StaticDiff.Compare(track, null, orders, [PartyRole.MainTank]).Single();
        Assert.That((row.Off, row.Differs, row.Unsure), Is.EqualTo((true, false, true)));
        Assert.That(StaticDiff.Report([row]), Does.Contain("ROLL").And.Contain("1 unsure"));
    }

    [Test]
    public void ASkippedStepAndAMomentPastThePullAreLeftOut()
    {
        var track = Track((0f, 0f, 0f), (50f, 0f, 0f));
        var lesson = new Lesson("X", "s", [], [new LessonStep(new LessonStepKey("Ai", Time: 10f), "Skip me", Skip: true)]);
        var orders = new List<DumpedOrder> { Order(10f, 1, 0f, 0f), Order(150f, 1, 0f, 0f) };
        Assert.That(StaticDiff.Compare(track, lesson, orders, [PartyRole.MainTank]), Is.Empty);
        Assert.That(StaticDiff.Report([]), Does.Contain("the pull matches it at every step"));
    }
}
