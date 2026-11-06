using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn.Mitigation;
using AsuMech.Scenarios;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Mitigation;

// The embedded cast timelines are what mit plans anchor on; a scenario edit that moves a cast
// shows up here, and `mit-timeline --all` rewrites the file.
public class MitTimelinesTests
{
    public static IEnumerable<TestCaseData> ScenariosWithStrats()
        => ScenarioCatalog.Create().Scenarios.Where(s => s.AiStrats.Count > 0).Select(s => new TestCaseData(s.GetType()).SetName($"TimelineHolds({s.GetType().Name})"));

    [Test]
    public void EveryScenarioWithAStratHasAnEmbeddedTimeline()
    {
        var missing = ScenarioCatalog.Create().Scenarios.Where(s => s.AiStrats.Count > 0).Select(s => s.GetType().Name).Where(n => MitTimelines.Load(n) is null).ToList();
        Assert.That(missing, Is.Empty, "run: mit-timeline --all");
    }

    [TestCaseSource(nameof(ScenariosWithStrats))]
    public void TimelineHolds(Type scenarioType)
    {
        var embedded = MitTimelines.Load(scenarioType.Name);
        Assume.That(embedded, Is.Not.Null, "no embedded timeline; run mit-timeline --all");
        var current = CastTimelines.Record(scenarioType);
        Assert.That(embedded!.Drift(current), Is.Empty, $"the run drifted from the embedded timeline; run: mit-timeline --all --scenario {scenarioType.Name}");
    }
}
