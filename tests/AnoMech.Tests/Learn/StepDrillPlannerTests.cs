using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn;
using AnoMech.Scenarios;
using NUnit.Framework;

namespace AnoMech.Tests.Learn;

public class StepDrillPlannerTests
{
    private static readonly LessonStep Early = new(new LessonStepKey("Ai", Time: 5f), Title: "early");
    private static readonly LessonStep Mid = new(new LessonStepKey("Ai", Time: 40f), Title: "mid");
    private static readonly LessonStep Skipped = new(new LessonStepKey("Ai", Time: 50f), Skip: true);
    private static readonly LessonStep Ranged = new(new LessonStepKey("Ai", From: 60f, To: 90f), Title: "ranged");
    private static readonly Lesson Lesson = new("S", "A", [], [Early, Mid, Skipped, Ranged]);

    private static MasteryStore Store(Dictionary<string, int>? hits = null) => new(hits ?? new(), () => { });

    [Test]
    public void TheWindowLeadsTheStepAndTrailsItsDeadline()
    {
        Assert.That(StepDrillPlanner.Window(Mid), Is.EqualTo(new DrillWindow(40f - StepDrillPlanner.LeadSeconds, 40f + StepDrillPlanner.TailSeconds)));
        Assert.That(StepDrillPlanner.Window(Early).From, Is.Zero);
        Assert.That(StepDrillPlanner.Window(Ranged), Is.EqualTo(new DrillWindow(60f - StepDrillPlanner.LeadSeconds, 90f + StepDrillPlanner.TailSeconds)));
    }

    [Test]
    public void TheShortestStreakIsWeakestAndSkippedStepsNeverAre()
    {
        var store = Store(new() { [MasteryStore.Id(Lesson, PartyRole.MainTank, Early.Key)] = 2, [MasteryStore.Id(Lesson, PartyRole.MainTank, Mid.Key)] = 1 });
        Assert.That(StepDrillPlanner.Weakest(Lesson, PartyRole.MainTank, store, null), Is.EqualTo(Ranged));
        store.Record(Lesson, PartyRole.MainTank, Ranged.Key, StepResult.OnTime);
        store.Record(Lesson, PartyRole.MainTank, Ranged.Key, StepResult.OnTime);
        Assert.That(StepDrillPlanner.Weakest(Lesson, PartyRole.MainTank, store, null), Is.EqualTo(Mid));
    }

    [Test]
    public void NothingIsLeftOnceEveryStepIsMastered()
    {
        var store = Store();
        foreach (var step in new[] { Early, Mid, Ranged })
            for (var i = 0; i < MasteryStore.HitsToMaster; i++) store.Record(Lesson, PartyRole.MainTank, step.Key, StepResult.OnTime);
        Assert.That(StepDrillPlanner.Weakest(Lesson, PartyRole.MainTank, store, null), Is.Null);
    }

    [Test]
    public void TheLastRunsFirstMissComesBeforeTheMasteryOrder()
    {
        var store = Store();
        var evaluation = new StepEvaluation(Vector3.Zero, 1f, 1f);
        evaluation.Close();
        var order = new GuidanceOrder(new GuidanceKey("Ai", 40f, "Ai.cs:1", null), PartyRole.MainTank, Vector3.Zero, 40f, null, false);
        var results = new List<(ActiveStep, StepResult)> { (new ActiveStep(order, Mid, evaluation, "mid", null, null, 0f), StepResult.WrongPlace) };
        var scenario = ScenarioCatalog.Create().Scenarios[0];
        var report = new LessonReport(scenario, scenario.AiStrats[0], Lesson, LearnLevel.Exam, PartyRole.MainTank, results);
        Assert.That(StepDrillPlanner.Weakest(Lesson, PartyRole.MainTank, store, report), Is.EqualTo(Mid));
        Assert.That(StepDrillPlanner.Weakest(Lesson, PartyRole.OffTank, store, report), Is.EqualTo(Early));
    }
}
