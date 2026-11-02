using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using NUnit.Framework;

namespace AnoMech.Tests;

public class ScenarioPickerTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    private static IScenario Grouped => Catalog.Scenarios.First(s => ScenarioPicker.StratGroups(s).Count > 1);
    private static IScenario MultiStrat => Catalog.Scenarios.First(s => ScenarioPicker.StratGroups(s).Count == 0 && s.AiStrats.Count > 1);
    private static IScenario MultiWaymark => Catalog.Scenarios.First(s => s.Phase.Zone.WaymarkPresets.Count > 1);

    [Test]
    public void SelectResetsStratAndSoloAndRemembers()
    {
        var remembered = new List<IScenario>();
        var picker = new ScenarioPicker(remembered.Add);
        picker.Select(MultiStrat);
        picker.SelectStrat(1);
        picker.Solo = true;
        picker.Select(MultiStrat);
        Assert.That(picker.Strat, Is.EqualTo(0));
        Assert.That(picker.Solo, Is.False);
        Assert.That(remembered, Has.Count.EqualTo(2));
    }

    [Test]
    public void RestoreDoesNotPersist()
    {
        var remembered = new List<IScenario>();
        var picker = new ScenarioPicker(remembered.Add);
        Assert.That(picker.Restore(MultiStrat.FullName(), Catalog.Scenarios), Is.True);
        Assert.That(picker.Scenario, Is.SameAs(MultiStrat));
        Assert.That(remembered, Is.Empty);
        Assert.That(picker.Restore("nope", Catalog.Scenarios), Is.False);
    }

    [Test]
    public void GroupedScenarioSnapsToItsFirstRegionAndRemembersThePick()
    {
        var scenario = Grouped;
        var groups = ScenarioPicker.StratGroups(scenario);
        var picker = new ScenarioPicker();
        picker.Select(scenario);
        Assert.That(picker.StratGroup, Is.EqualTo(groups[0]));
        picker.SelectGroup(groups[1]);
        picker.Select(MultiStrat);
        picker.Select(scenario);
        Assert.That(picker.StratGroup, Is.EqualTo(groups[1]));
        if (picker.RegionStrats().Count > 0)
            Assert.That(scenario.AiStrats[picker.Strat].Group, Is.EqualTo(groups[1]));
    }

    [Test]
    public void SelectionCarriesNoStratWhenTheRegionHasNone()
    {
        var picker = new ScenarioPicker();
        picker.Select(Grouped);
        Assert.That(picker.Selection!.Strat, Is.EqualTo(picker.HasStartableStrat ? picker.Strat : null));
    }

    [Test]
    public void WaymarkIsRememberedPerZone()
    {
        var scenario = MultiWaymark;
        var other = Catalog.Scenarios.First(s => s.Phase.Zone != scenario.Phase.Zone);
        var picker = new ScenarioPicker();
        picker.Select(scenario);
        picker.SelectWaymark(1);
        picker.Select(other);
        Assert.That(picker.Waymark, Is.EqualTo(0));
        picker.Select(scenario);
        Assert.That(picker.Waymark, Is.EqualTo(1));
    }

    [Test]
    public void SessionDropsTheRoleAndGuestDropsTheStrat()
    {
        var picker = new ScenarioPicker();
        picker.Select(MultiStrat);
        picker.RoleOverride = PartyRole.MainTank;
        picker.SelectStrat(1);
        picker.Reconcile(inSession: true, guest: true);
        Assert.That(picker.RoleOverride, Is.Null);
        Assert.That(picker.Strat, Is.EqualTo(0));
    }

    [Test]
    public void SeedParsesOnlyNumbers()
    {
        var picker = new ScenarioPicker { SeedText = " 42 " };
        Assert.That(picker.Seed, Is.EqualTo(42));
        picker.SeedText = "";
        Assert.That(picker.Seed, Is.Null);
    }
}
