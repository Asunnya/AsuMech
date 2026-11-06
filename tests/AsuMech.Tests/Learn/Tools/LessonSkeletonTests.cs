using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn;
using AsuMech.Learn.Tools;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Tools;

public class LessonSkeletonTests
{
    private static DumpedOrder Order(float time, int line, string? method = null, string role = "MainTank", int seed = 1)
        => new("Ai", time, $"Ai.cs:{line}", method, role, seed, time, null, 0f, 0f);

    private static readonly OrderDump Dump = new("S", "A", 0, [
        Order(0.5f, 10), Order(0.5f, 10, role: "OffTank"),
        Order(11.2f, 11, "DragGarudaWestPartyBehind"),
        Order(119.7f, 12, "TakeMesohighTethers"), Order(120.0f, 12, "TakeMesohighTethers"), Order(120.3f, 12, "TakeMesohighTethers"),
    ]);

    [Test]
    public void OneStepPerCallWithLoopsFoldedIntoARange()
    {
        var lesson = LessonSkeleton.Generate(Dump);
        Assert.That(lesson.Steps.Select(s => s.Title), Is.EqualTo(new[] { "Move at 0.5s", "Drag garuda west party behind", "Take mesohigh tethers" }));
        Assert.That(lesson.Steps[2].Key, Is.EqualTo(new LessonStepKey("Ai", From: 119.7f, To: 120.3f, Line: 12)));
        Assert.That(lesson.Steps[1].Key.Time, Is.EqualTo(11.2f));
    }

    [TestCase("TanksInterceptSistersSongsPartyStackSouthEast", "Tanks intercept sisters songs party stack south east")]
    [TestCase("StackAt", "Stack at")]
    [TestCase("OffTankAndMeleeStepOutForFirstFriction", "Off tank and melee step out for first friction")]
    public void TitlesComeFromMethodNames(string method, string title)
        => Assert.That(LessonSkeleton.Title(method), Is.EqualTo(title));

    [Test]
    public void RekeyFollowsALineWhoseTimeChanged()
    {
        var lesson = new Lesson("S", "A", [], [
            new LessonStep(new LessonStepKey("Ai", Time: 10.9f, Line: 11), Title: "drag", Rule: "r"),
            new LessonStep(new LessonStepKey("Ai", Time: 99f, Line: 99), Title: "gone", Rule: "r"),
            new LessonStep(new LessonStepKey("Ai", Time: 0.5f, Line: 10), Title: "same", Rule: "r"),
        ]);
        var (rekeyed, orphans) = LessonSkeleton.Rekey(lesson, Dump);
        Assert.That(rekeyed.Steps[0].Key.Time, Is.EqualTo(11.2f));
        Assert.That(rekeyed.Steps[2].Key.Time, Is.EqualTo(0.5f));
        Assert.That(orphans.Select(o => o.Title), Is.EqualTo(new[] { "gone" }));
    }

    [Test]
    public void CoverageReportsBothDirections()
    {
        var lesson = new Lesson("S", "A", [], [
            new LessonStep(new LessonStepKey("Ai", Time: 0.5f), Rule: "r"),
            new LessonStep(new LessonStepKey("Ai", Time: 42f), Rule: "r"),
        ]);
        var (uncovered, untriggered) = LessonSkeleton.Coverage(lesson, Dump);
        Assert.That(uncovered.Select(o => o.Time), Is.EqualTo(new[] { 11.2f, 119.7f, 120.0f, 120.3f }));
        Assert.That(untriggered.Select(s => s.Key.Time), Is.EqualTo(new float?[] { 42f }));
    }

    [Test]
    public void SkeletonRoundTripsThroughTheLoader()
    {
        var json = LessonSkeleton.ToJson(LessonSkeleton.Generate(Dump));
        var back = LessonLoader.Parse(json);
        Assert.That(back.Steps, Has.Count.EqualTo(3));
        Assert.That(back.Steps[2].Key.From, Is.EqualTo(119.7f));
    }
}
