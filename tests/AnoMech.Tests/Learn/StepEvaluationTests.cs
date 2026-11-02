using System.Numerics;
using AnoMech.Learn;
using NUnit.Framework;

namespace AnoMech.Tests.Learn;

public class StepEvaluationTests
{
    private static readonly Vector3 Target = new(5f, 0f, 5f);
    private static readonly Vector3 Far = new(20f, 0f, 20f);

    private static StepEvaluation New() => new(Target, tolerance: 1.5f, deadline: 10f);

    [Test]
    public void InPlaceAtTheDeadlineIsOnTime()
    {
        var e = New();
        e.Sample(8f, Far);
        e.Sample(9f, Target + new Vector3(1f, 0f, 0f));
        Assert.That(e.Result, Is.Null);
        e.Sample(10f, Target);
        Assert.That(e.Result, Is.EqualTo(StepResult.OnTime));
    }

    [Test]
    public void ArrivingAfterTheDeadlineIsLate()
    {
        var e = New();
        e.Sample(10f, Far);
        e.Sample(12f, Target);
        Assert.That(e.Result, Is.EqualTo(StepResult.Late));
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
    public void NeverArrivingIsWrongPlaceWhenClosed()
    {
        var e = New();
        e.Sample(11f, Far);
        Assert.That(e.Close(), Is.EqualTo(StepResult.WrongPlace));
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
}
