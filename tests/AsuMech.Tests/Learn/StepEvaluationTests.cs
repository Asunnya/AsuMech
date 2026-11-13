using System.Numerics;
using AsuMech.Learn;
using NUnit.Framework;

namespace AsuMech.Tests.Learn;

public class StepEvaluationTests
{
    private static readonly Vector3 Target = new(5f, 0f, 5f);
    private static readonly Vector3 Far = new(20f, 0f, 20f);

    private static StepEvaluation New() => new(Target, tolerance: 1.5f, deadline: 10f);

    [Test]
    public void InPlaceAtTheDeadlineIsOnTime()
    {
        var e = New();
        e.Sample(1f, Far);
        e.Sample(9f, Target + new Vector3(1f, 0f, 0f));
        Assert.That(e.Result, Is.Null);
        e.Sample(10f, Target);
        Assert.That(e.Result, Is.EqualTo(StepResult.OnTime));
    }

    [Test]
    public void ArrivingAfterTheDeadlineIsLate()
    {
        var e = New();
        e.Sample(1f, Far);
        e.Sample(12f, Target);
        Assert.That(e.Result, Is.EqualTo(StepResult.Late));
    }

    [Test]
    public void TheDeadlineIsNeverSoonerThanARunFromWhereThePlayerStoodAtTheOrder()
    {
        var e = New();
        e.Sample(9.5f, Far);
        Assert.That(e.Deadline, Is.EqualTo(9.5f + 21.2f / 6.5f + StepEvaluation.ReactionGraceSeconds).Within(0.05f));
        e.Sample(12f, Target);
        e.Sample(13.5f, Target);
        Assert.That(e.Result, Is.EqualTo(StepResult.OnTime));
        var slowed = New();
        slowed.Sample(9.5f, Far, speedFactor: 0.5f);
        Assert.That(slowed.Deadline, Is.EqualTo(9.5f + 21.2f / 3.25f + StepEvaluation.ReactionGraceSeconds).Within(0.05f));
    }

    [Test]
    public void SupersededBeforeAnyoneCouldReactHasNoVerdict()
    {
        var e = New();
        e.Sample(3f, Far);
        e.Sample(3.2f, Far);
        Assert.That(e.Close(), Is.Null);
    }

    [Test]
    public void LeavingBeforeTheDeadlineDoesNotCount()
    {
        var e = New();
        e.Sample(5f, Target);
        e.Sample(10f, Far);
        Assert.That(e.Result, Is.Null);
        Assert.That(e.Close(), Is.EqualTo(StepResult.WrongPlace));
    }

    [Test]
    public void SupersededOnTheWayBeforeTheDeadlineHasNoVerdict()
    {
        var e = New();
        e.Sample(3f, Far);
        e.Sample(4f, (Far + Target) / 2f);
        Assert.That(e.Close(), Is.Null);
        Assert.That(e.Result, Is.Null);
    }

    [Test]
    public void NeverArrivingIsWrongPlaceWhenClosed()
    {
        var e = New();
        e.Sample(1f, Far);
        e.Sample(11f, Far);
        Assert.That(e.Close(), Is.EqualTo(StepResult.WrongPlace));
    }

    [Test]
    public void AForcedMoveSettlesOnlyAStepThePlayerWasHoldingTheSpotOf()
    {
        var e = New();
        e.Sample(3f, Target);
        Assert.That(e.Settle(), Is.EqualTo(StepResult.OnTime));
        var far = New();
        far.Sample(3f, Far);
        Assert.That(far.Settle(), Is.Null, "the push may land on the spot");
        far.Sample(10f, Target);
        Assert.That(far.Result, Is.EqualTo(StepResult.OnTime));
        Assert.That(New().Settle(), Is.Null, "never sampled");
    }

    [Test]
    public void AMechanicFailureWinsOverAnOpenStep()
    {
        var e = New();
        e.Sample(3f, Far);
        e.MarkFailed();
        e.Sample(10f, Target);
        Assert.That(e.Result, Is.EqualTo(StepResult.MechanicFailed));
    }

    [Test]
    public void YIsIgnored()
    {
        var e = New();
        e.Sample(10f, Target + new Vector3(0f, 7f, 0f));
        Assert.That(e.Result, Is.EqualTo(StepResult.OnTime));
    }

    [Test]
    public void TheDistanceAtTheVerdictIsKeptWhileLaterSamplesMoveOn()
    {
        var e = New();
        e.Sample(1f, Far);
        e.Sample(9f, Far);
        e.Close();
        Assert.That(e.ResolvedDistance, Is.EqualTo(Vector2.Distance(new Vector2(20f, 20f), new Vector2(5f, 5f))).Within(0.01f));
        e.Sample(11f, Target);
        Assert.That(e.ResolvedDistance, Is.EqualTo(Vector2.Distance(new Vector2(20f, 20f), new Vector2(5f, 5f))).Within(0.01f));
        Assert.That(e.LastDistance, Is.EqualTo(0f));
    }
}
