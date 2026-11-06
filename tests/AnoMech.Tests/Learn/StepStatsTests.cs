using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn;

namespace AnoMech.Tests;

public class StepStatsTests
{
    private static readonly LessonStep Tower = new(new LessonStepKey("Ai", Time: 20f), "Tower");
    private static readonly LessonStep Spread = new(new LessonStepKey("Ai", Time: 40f), "Spread");
    private static readonly LessonStep Stack = new(new LessonStepKey("Ai", Time: 60f), "Stack");
    private static readonly Lesson Lesson = new("X", "strat", [], [Tower, Spread, Stack]);

    [Test]
    public void ReactionIsTheFirstSampleThatClosesAYalmOnTheSpot()
    {
        var eval = new StepEvaluation(new Vector3(10f, 0f, 0f), 1.5f, 25f);
        eval.Sample(20.0f, new Vector3(0f, 0f, 0f));
        eval.Sample(20.4f, new Vector3(0.3f, 0f, 0f));
        Assert.That(eval.ReactedAt, Is.Null, "a shuffle isn't a reaction");
        eval.Sample(20.9f, new Vector3(1.2f, 0f, 0f));
        Assert.That(eval.ReactedAt, Is.EqualTo(20.9f));
        eval.Sample(22f, new Vector3(9f, 0f, 0f));
        Assert.That(eval.ReactedAt, Is.EqualTo(20.9f), "the first one counts");
        var already = new StepEvaluation(Vector3.Zero, 1.5f, 25f);
        already.Sample(20f, new Vector3(1f, 0f, 0f));
        Assert.That(already.ReactedAt, Is.EqualTo(20f), "on the spot from the start: no reaction needed");
    }

    private static StepStatsStore Store(out Dictionary<string, List<StepAttempt>> data)
    {
        data = [];
        return new StepStatsStore(data, () => { });
    }

    private static StepAttempt Try(StepResult result, float reaction = 1f, string? failure = null) => new() { Result = result, Reaction = reaction, Distance = 0.5f, Failure = failure };

    [Test]
    public void TheMapListsStepsWithEnoughTriesWorstFirstWithATrend()
    {
        var stats = Store(out var data);
        for (var i = 0; i < 6; i++) stats.Record(Lesson, PartyRole.MainTank, Tower.Key, Try(StepResult.OnTime, 0.8f));
        for (var i = 0; i < 4; i++) stats.Record(Lesson, PartyRole.MainTank, Spread.Key, Try(StepResult.OnTime, 1.5f));
        for (var i = 0; i < 6; i++) stats.Record(Lesson, PartyRole.MainTank, Spread.Key, Try(StepResult.WrongPlace, 2.5f, "Spread: you were 1.2 yalms inside the circle"));
        for (var i = 0; i < 3; i++) stats.Record(Lesson, PartyRole.MainTank, Stack.Key, Try(StepResult.Late));
        var map = stats.Map(Lesson, PartyRole.MainTank);
        Assert.That(map.Select(t => t.Step.Title), Is.EqualTo(new[] { "Spread", "Tower" }), "Stack has only 3 tries");
        var spread = map[0];
        Assert.That(spread.Attempts, Is.EqualTo(10));
        Assert.That(spread.FailRate, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(spread.MeanReaction, Is.EqualTo(2.1f).Within(0.001f));
        Assert.That(spread.Trend, Is.GreaterThan(0f), "the last five all failed: worse");
        Assert.That(spread.CommonFailure, Does.Contain("inside the circle"));
        Assert.That(map[1].FailRate, Is.Zero);
        Assert.That(stats.Map(Lesson, PartyRole.OffTank), Is.Empty, "per role");
        for (var i = 0; i < 30; i++) stats.Record(Lesson, PartyRole.MainTank, Tower.Key, Try(StepResult.OnTime));
        Assert.That(stats.For(Lesson, PartyRole.MainTank, Tower.Key), Has.Count.EqualTo(StepStatsStore.Kept));
        Assert.That(data, Has.Count.EqualTo(3));
    }

    [Test]
    public void TheAdaptiveDrillTakesTheExamsMissesAndTheWorstInTimelineOrder()
    {
        var stats = Store(out _);
        for (var i = 0; i < 5; i++) stats.Record(Lesson, PartyRole.MainTank, Stack.Key, Try(StepResult.WrongPlace));
        for (var i = 0; i < 5; i++) stats.Record(Lesson, PartyRole.MainTank, Tower.Key, Try(i < 2 ? StepResult.Late : StepResult.OnTime));
        var order = new GuidanceOrder(new GuidanceKey("Ai", 40f, "Ai.cs:1", null), PartyRole.MainTank, Vector3.Zero, 40f, null, false);
        var missedSpread = new ActiveStep(order, Spread, new StepEvaluation(Vector3.Zero, 1.5f, 45f), "Spread", null, null, 40f);
        var report = new LessonReport(null!, null!, Lesson, LearnLevel.Exam, PartyRole.MainTank, [(missedSpread, StepResult.WrongPlace)]);
        var worst = StepDrillPlanner.Worst(Lesson, PartyRole.MainTank, stats, report);
        Assert.That(worst.Select(s => s.Title), Is.EqualTo(new[] { "Tower", "Spread", "Stack" }), "the three, by the timeline");
        Assert.That(StepDrillPlanner.Worst(Lesson, PartyRole.MainTank, stats, report, count: 2).Select(s => s.Title), Is.EqualTo(new[] { "Spread", "Stack" }),
            "the miss, then the worst rate");
        Assert.That(StepDrillPlanner.Worst(Lesson, PartyRole.OffTank, Store(out _), null), Is.Empty, "nothing failed, nothing to drill");
    }
}
