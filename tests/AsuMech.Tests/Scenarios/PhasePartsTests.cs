using System.Linq;
using AsuMech.Scenarios;
using NUnit.Framework;

namespace AsuMech.Tests.Scenarios;

public class PhasePartsTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    [Test]
    public void AnUltimatesSplitPhaseIsOneEntryHoldingItsParts()
    {
        var fru = Catalog.Zones.First(z => z.Category == "Ultimate" && Catalog.FightOf(z).Any(s => s.Name == "Utopian Sky"));
        var groups = Catalog.PhaseGroupsOf(fru);
        Assert.That(groups.Select(g => g[0].Phase.Name), Is.EqualTo(new[] { "P1", "P2", "P3", "P4", "P5" }));
        Assert.That(groups[0].Select(s => s.Name), Is.EqualTo(new[] { "Utopian Sky", "Turn of the Heavens", "Fall of Faith" }));
        Assert.That(groups[4], Has.Count.EqualTo(7));
    }

    [Test]
    public void EveryScenarioIsInExactlyOnePartListInFightOrder()
    {
        foreach (var zone in Catalog.Zones)
            Assert.That(Catalog.PhaseGroupsOf(zone).SelectMany(g => g), Is.EqualTo(Catalog.FightOf(zone)), zone.Name);
        foreach (var scenario in Catalog.Scenarios)
            Assert.That(Catalog.PartsOf(scenario), Does.Contain(scenario));
    }

    [Test]
    public void ASavageFightKeepsOneEntryPerScenario()
    {
        foreach (var zone in Catalog.Zones.Where(z => z.Category != "Ultimate"))
            Assert.That(Catalog.PhaseGroupsOf(zone).All(g => g.Count == 1), zone.Name);
    }
}
