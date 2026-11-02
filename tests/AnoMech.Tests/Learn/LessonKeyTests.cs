using AnoMech.Core.Game.Ai;
using AnoMech.Learn;
using NUnit.Framework;

namespace AnoMech.Tests.Learn;

public class LessonKeyTests
{
    private static GuidanceKey Order(float time, int line = 41, string cls = "UwuP1GarudaAi")
        => new(cls, time, $"UwuP1GarudaAi.cs:{line}", null);

    [Test]
    public void TimeMatchesRegardlessOfLine()
    {
        var key = new LessonStepKey("UwuP1GarudaAi", Time: 11.2f, Line: 41);
        Assert.That(key.Matches(Order(11.2f, line: 57)), Is.True);
        Assert.That(key.Matches(Order(11.3f)), Is.False);
        Assert.That(key.Matches(Order(11.2f, cls: "OtherAi")), Is.False);
    }

    [Test]
    public void RangeCoversALoopOfCalls()
    {
        var key = new LessonStepKey("UwuP1GarudaAi", From: 119.7f, To: 123.9f);
        Assert.That(key.Matches(Order(119.7f)), Is.True);
        Assert.That(key.Matches(Order(122.1f)), Is.True);
        Assert.That(key.Matches(Order(124.2f)), Is.False);
    }

    [Test]
    public void LineBreaksTiesBetweenStepsWithTheSameTime()
    {
        var lesson = new Lesson("S", "A", [], [
            new LessonStep(new LessonStepKey("UwuP1GarudaAi", Time: 5f, Line: 10), Title: "first"),
            new LessonStep(new LessonStepKey("UwuP1GarudaAi", Time: 5f, Line: 20), Title: "second"),
        ]);
        Assert.That(lesson.StepFor(Order(5f, line: 20))!.Title, Is.EqualTo("second"));
        Assert.That(lesson.StepFor(Order(5f, line: 99))!.Title, Is.EqualTo("first"));
        Assert.That(lesson.StepFor(Order(6f)), Is.Null);
    }

    [Test]
    public void RoleTextOverridesTheGenericOne()
    {
        var step = new LessonStep(new LessonStepKey("X", Time: 1f), Rule: "all", Tolerance: 2f,
            Roles: new() { ["MainTank"] = new LessonRoleText(Rule: "tank", Tolerance: 1f) });
        Assert.That(step.RuleFor(AnoMech.Core.Game.Party.PartyRole.MainTank), Is.EqualTo("tank"));
        Assert.That(step.RuleFor(AnoMech.Core.Game.Party.PartyRole.CasterDps), Is.EqualTo("all"));
        Assert.That(step.ToleranceFor(AnoMech.Core.Game.Party.PartyRole.MainTank), Is.EqualTo(1f));
        Assert.That(step.ToleranceFor(AnoMech.Core.Game.Party.PartyRole.CasterDps), Is.EqualTo(2f));
    }
}
