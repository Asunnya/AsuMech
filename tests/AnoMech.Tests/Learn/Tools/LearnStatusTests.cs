using System.Linq;
using AnoMech.Learn;
using AnoMech.Learn.Tools;
using AnoMech.Learn.Toolbox;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Tools;

public class LearnStatusTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    [Test]
    public void TheMatrixCoversEveryStratAndEveryEmbeddedLesson()
    {
        var rows = LearnStatus.Rows(Catalog);
        TestContext.Out.WriteLine(LearnStatus.Table(rows));
        Assert.That(rows.Count, Is.EqualTo(Catalog.Scenarios.Sum(s => s.AiStrats.Count)));
        foreach (var lesson in LessonLoader.All)
            Assert.That(rows.Any(r => r.Scenario == lesson.Scenario && r.Strat == lesson.Strat && r.Lesson), $"{lesson.Scenario} / {lesson.Strat}");
        var garuda = rows.Single(r => r.Scenario == nameof(UwuP1GarudaScenario));
        Assert.That(garuda, Is.EqualTo(garuda with { Lesson = true, Toolbox = true, PlanEmbedded = true, ExampleSeed = true, Example = true, Glossary = true, MitTimeline = true, MitPlan = true, Registered = true }));
    }

    [Test]
    public void EveryRegisteredPlanNamesACatalogScenarioAndAKnownHost()
    {
        Assert.That(PlanRegistry.All, Is.Not.Empty);
        foreach (var (scenario, entries) in PlanRegistry.All)
        {
            var found = Catalog.Scenarios.FirstOrDefault(s => s.GetType().Name == scenario);
            Assert.That(found, Is.Not.Null, scenario);
            foreach (var e in entries)
            {
                Assert.That(e.Ref.Host, Is.Not.EqualTo(RaidplanHost.Unknown), e.Url);
                if (e.Layout != null) Assert.That(found!.Phase.Zone.WaymarkPresets.Select(w => w.Name), Has.Member(e.Layout), e.Url);
                if (e.Strat != null) Assert.That(found!.AiStrats.Select(a => a.Name), Has.Member(e.Strat), e.Url);
            }
        }
    }

    [Test]
    public void PlanLinksOfBothHostsParse()
    {
        Assert.That(PlanRef.Parse("https://ff14.toolboxgaming.space/?id=122994122467471&preview=1"), Is.EqualTo(new PlanRef(RaidplanHost.Toolbox, "122994122467471", "https://ff14.toolboxgaming.space/?id=122994122467471&preview=1")));
        Assert.That(PlanRef.Parse("122994122467471").Host, Is.EqualTo(RaidplanHost.Toolbox));
        Assert.That(PlanRef.Parse("https://raidplan.io/plan/PPYgcTqAr4DXZE50"), Is.EqualTo(new PlanRef(RaidplanHost.RaidplanIo, "PPYgcTqAr4DXZE50", "https://raidplan.io/plan/PPYgcTqAr4DXZE50")));
        Assert.That(PlanRef.Parse("https://example.com/x").Host, Is.EqualTo(RaidplanHost.Unknown));
    }

    [Test]
    public void ReadinessReadsTheScenarioWithoutRunningIt()
    {
        var garuda = Catalog.Scenarios.OfType<UwuP1GarudaScenario>().Single();
        var rows = LearnReadiness.Check(garuda);
        TestContext.Out.WriteLine(LearnReadiness.Table(garuda, rows));
        Assert.That(rows.Single(r => r.Reads == "*State public members").Status, Is.EqualTo(Readiness.Ok));
        // The State exposes the song spots as vectors while the overrides pin bearings: not the same name, so not pinnable by name.
        Assert.That(rows.Single(r => r.Reads == "*StateOverrides").Detail, Does.Contain("pinnable: MistralSongTarget").And.Contain("not: SuparnaSongSpot, ChiradaSongSpot"));
        Assert.That(rows.Single(r => r.Reads == "plans.json").Status, Is.EqualTo(Readiness.Ok));
        Assert.That(rows.Single(r => r.Reads == "orders for the player's slot").Status, Is.EqualTo(Readiness.Degraded));
        Assert.That(rows.Single(r => r.Reads == "mit timeline + plan").Status, Is.EqualTo(Readiness.Ok));
        var dsr = Catalog.Scenarios.First(s => s.GetType().Name == "TopP5DeltaScenario");
        var dsrRows = LearnReadiness.Check(dsr);
        TestContext.Out.WriteLine(LearnReadiness.Table(dsr, dsrRows));
        Assert.That(dsrRows.Single(r => r.Reads == "plans.json").Status, Is.EqualTo(Readiness.Missing));
    }
}
