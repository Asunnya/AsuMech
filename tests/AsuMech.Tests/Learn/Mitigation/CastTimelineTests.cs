using System;
using System.IO;
using System.Linq;
using AsuMech.Learn.Mitigation;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Mitigation;

public class CastTimelineTests
{
    internal static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Learn", "Mitigation", "Fixtures", name));
    internal static CastTimeline Garuda => CastTimeline.FromTimelineCsv("UwuP1GarudaScenario", Fixture("garuda.casts.csv"));

    [Test]
    public void CastsResolveAfterTheirBarAndCountOccurrences()
    {
        var t = Garuda;
        var shriek = t.Find("Mistral Shriek", 1)!;
        Assert.That(shriek.Start, Is.EqualTo(38.3f).Within(0.1f));
        Assert.That(shriek.Resolve, Is.EqualTo(41.0f).Within(0.1f));
        Assert.That(t.Find("Friction", 2)!.Start, Is.GreaterThan(t.Find("Friction", 1)!.Start));
        Assert.That(t.Find("Friction", 3), Is.Null);
    }

    [Test]
    public void SimultaneousCastersCountOnceAndACastIsNotAlsoAnInstant()
    {
        var t = Garuda;
        Assert.That(t.Casts.Count(c => c.Name == "Feather Rain" && Math.Abs(c.Start - 35.5f) < 0.2f), Is.EqualTo(1));
        Assert.That(t.Casts.Count(c => c.Name == "Mistral Shriek"), Is.EqualTo(1));
        var downburst = t.Find("Downburst", 1)!;
        Assert.That(downburst.Start, Is.EqualTo(downburst.Resolve), "an instant has no bar");
    }

    [Test]
    public void RoundTripsThroughJson()
    {
        var t = Garuda;
        var back = CastTimeline.Parse(t.ToJson());
        Assert.That(back.Casts, Has.Count.EqualTo(t.Casts.Count));
    }
}
