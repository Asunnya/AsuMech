using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn;
using NUnit.Framework;

namespace AnoMech.Tests.Learn;

public class LessonDirectorTests
{
    private static readonly Lesson Lesson = new("S", "A", [], [
        new LessonStep(new LessonStepKey("Ai", Time: 1f), Title: "go", Rule: "go there", Tolerance: 1f),
        new LessonStep(new LessonStepKey("Ai", Time: 2f), Skip: true),
        new LessonStep(new LessonStepKey("Ai", Time: 3f), Title: "next", Rule: "then here", Roles: new() { ["MainTank"] = new LessonRoleText(Rule: "tank here") }),
    ]);

    private static GuidanceOrder Order(float time, Vector3 target, float? arrival = null)
        => new(new GuidanceKey("Ai", time, $"Ai.cs:{(int)time}", null), PartyRole.MainTank, target, time, arrival, false);

    private readonly List<float> speeds = [];
    private LessonDirector director = null!;

    [SetUp]
    public void SetUp()
    {
        speeds.Clear();
        director = new LessonDirector(Lesson, PartyRole.MainTank, LearnLevel.Guided, speeds.Add);
    }

    [Test]
    public void AnOrderBecomesTheCurrentStepWithTheRolesText()
    {
        director.OnMove(Order(3f, new Vector3(1f, 0f, 1f)));
        Assert.That(director.Current!.Title, Is.EqualTo("next"));
        Assert.That(director.Current.Rule, Is.EqualTo("tank here"));
    }

    [Test]
    public void AReloadedLessonRewordsTheStepInProgressAndKeepsItsEvaluation()
    {
        director.OnMove(Order(3f, new Vector3(1f, 0f, 1f), 9f));
        var evaluation = director.Current!.Evaluation;
        var changes = 0;
        director.StepChanged += _ => changes++;
        var fresh = Lesson with { Steps = [new LessonStep(new LessonStepKey("Ai", Time: 3f), Title: "reworded", Rule: "new words", Cue: "a cue")] };
        director.Reload(fresh);
        Assert.That(director.Current!.Title, Is.EqualTo("reworded"));
        Assert.That(director.Current.Rule, Is.EqualTo("new words"));
        Assert.That(director.Current.Cue, Is.EqualTo("a cue"));
        Assert.That(director.Current.Evaluation, Is.SameAs(evaluation));
        Assert.That(changes, Is.EqualTo(1));
        director.OnMove(Order(1f, Vector3.Zero));
        Assert.That(director.Current!.Step, Is.Null, "the fresh lesson has no step at 1s");
    }

    [Test]
    public void ASkippedOrderShowsNothing()
    {
        director.OnMove(Order(1f, Vector3.Zero));
        director.OnMove(Order(2f, Vector3.Zero));
        Assert.That(director.Current, Is.Null);
    }

    [Test]
    public void GuidedSlowsWhileAwayAndRestoresOnArrival()
    {
        director.OnMove(Order(1f, new Vector3(10f, 0f, 0f), arrival: 5f));
        director.Tick(1.1f, Vector3.Zero);
        Assert.That(speeds, Is.EqualTo(new[] { LessonDirector.GuidedSlowScale }));
        director.Tick(1.2f, new Vector3(10f, 0f, 0f));
        Assert.That(speeds, Is.EqualTo(new[] { LessonDirector.GuidedSlowScale, 1f }));
    }

    [Test]
    public void ExamNeverTouchesSpeed()
    {
        var exam = new LessonDirector(Lesson, PartyRole.MainTank, LearnLevel.Exam, speeds.Add);
        exam.OnMove(Order(1f, new Vector3(10f, 0f, 0f), arrival: 5f));
        exam.Tick(1.1f, Vector3.Zero);
        Assert.That(speeds, Is.Empty);
    }

    [Test]
    public void TheNextOrderClosesAnUnfinishedStepAsWrongPlace()
    {
        var resolved = new List<(string, StepResult)>();
        director.StepResolved += (s, r) => resolved.Add((s.Title, r));
        director.OnMove(Order(1f, new Vector3(10f, 0f, 0f), arrival: 5f));
        director.Tick(2f, Vector3.Zero);
        director.OnMove(Order(3f, Vector3.Zero));
        Assert.That(resolved, Is.EqualTo(new[] { ("go", StepResult.WrongPlace) }));
    }

    [Test]
    public void ArrivingInTimeResolvesOnTimeAtTheDeadline()
    {
        director.OnMove(Order(1f, new Vector3(10f, 0f, 0f), arrival: 5f));
        director.Tick(3f, new Vector3(10f, 0f, 0f));
        Assert.That(director.Results, Is.Empty);
        director.Tick(5f, new Vector3(10f, 0f, 0f));
        Assert.That(director.Results[0].Result, Is.EqualTo(StepResult.OnTime));
    }

    [Test]
    public void AssistedShowsTheHintLaterPerMasteryAndNeverOnceMastered()
    {
        var store = new MasteryStore(new Dictionary<string, int>(), () => { });
        var assisted = new LessonDirector(Lesson, PartyRole.MainTank, LearnLevel.Assisted, speeds.Add, store);
        assisted.OnMove(Order(1f, Vector3.Zero, arrival: 7f));
        Assert.That(assisted.Current!.ShowAt, Is.EqualTo(1f));
        assisted.Tick(7f, Vector3.Zero);
        assisted.OnMove(Order(1f, Vector3.Zero, arrival: 7f));
        Assert.That(assisted.Current!.ShowAt, Is.EqualTo(3f));
        assisted.Tick(7f, Vector3.Zero);
        assisted.OnMove(Order(1f, Vector3.Zero, arrival: 7f));
        assisted.Tick(7f, Vector3.Zero);
        assisted.OnMove(Order(1f, Vector3.Zero, arrival: 7f));
        Assert.That(assisted.Current!.HintVisible(1000f), Is.False);
        assisted.Tick(1.1f, Vector3.Zero);
        Assert.That(speeds, Is.Empty, "Assisted never slows the sim");
    }

    [Test]
    public void WithoutADeadlineTheDefaultWindowApplies()
    {
        director.OnMove(Order(1f, Vector3.Zero));
        Assert.That(director.Current!.Evaluation.Deadline, Is.EqualTo(1f + LessonDirector.DefaultWindowSeconds));
    }

    [Test]
    public void AFailureDuringTheStepIsRecorded()
    {
        director.OnMove(Order(1f, Vector3.Zero));
        director.OnMechanicFailed();
        Assert.That(director.Results[0].Result, Is.EqualTo(StepResult.MechanicFailed));
        director.End();
        Assert.That(director.Results, Has.Count.EqualTo(1));
    }

    [Test]
    public void ObserveNeverJudgesNorTouchesSpeed()
    {
        var store = new MasteryStore(new Dictionary<string, int>(), () => { });
        var observe = new LessonDirector(Lesson, PartyRole.MainTank, LearnLevel.Observe, speeds.Add, store);
        observe.OnMove(Order(1f, new Vector3(10f, 0f, 0f), arrival: 5f));
        observe.Tick(1.1f, Vector3.Zero);
        observe.Tick(6f, Vector3.Zero);
        observe.OnMechanicFailed();
        observe.OnMove(Order(3f, Vector3.Zero));
        observe.End();
        Assert.That(observe.Results, Is.Empty);
        Assert.That(store.MasteredCount(Lesson, PartyRole.MainTank), Is.Zero);
        Assert.That(speeds, Is.EqualTo(new[] { 1f }).Or.Empty);
    }
}
