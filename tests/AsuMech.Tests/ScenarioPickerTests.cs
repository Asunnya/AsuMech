using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using NUnit.Framework;

namespace AsuMech.Tests;

public class ScenarioPickerTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    private static IScenario Grouped => Catalog.Scenarios.First(s => ScenarioPicker.StratGroups(s).Count > 1);
    private static IScenario MultiStrat => Catalog.Scenarios.First(s => ScenarioPicker.StratGroups(s).Count == 0 && s.AiStrats.Count > 1);
    private static IScenario MultiWaymark => Catalog.Scenarios.First(s => s.Phase.Zone.WaymarkPresets.Count > 1);

    [Test]
    public void SelectResetsSoloKeepsTheStratAndRemembers()
    {
        var remembered = new List<IScenario>();
        var picker = new ScenarioPicker(remembered.Add);
        picker.Select(MultiStrat);
        picker.SelectStrat(1);
        picker.Solo = true;
        picker.Select(MultiStrat);
        Assert.That(picker.Strat, Is.EqualTo(1));
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
    public void SessionDropsTheRoleAndGuestDropsTheStratUntilItEnds()
    {
        var picker = new ScenarioPicker();
        picker.Select(MultiStrat);
        picker.ChooseRole(PartyRole.MainTank);
        picker.SelectStrat(1);
        picker.Reconcile(inSession: true, guest: true);
        Assert.That(picker.RoleOverride, Is.Null);
        Assert.That(picker.Strat, Is.EqualTo(0));
        picker.Reconcile(inSession: false, guest: false);
        Assert.That(picker.RoleOverride, Is.EqualTo(PartyRole.MainTank));
        Assert.That(picker.Strat, Is.EqualTo(1));
    }

    [Test]
    public void TheRoleAndStratComeBackInTheNextSession()
    {
        var picks = new PickMemory();
        var saves = 0;
        var first = new ScenarioPicker(null, picks, () => saves++);
        first.Select(MultiStrat);
        first.SelectStrat(1);
        first.ChooseRole(PartyRole.CasterDps);
        Assert.That(saves, Is.EqualTo(2));

        var next = new ScenarioPicker(null, picks);
        next.Select(MultiStrat);
        Assert.That(next.Strat, Is.EqualTo(1));
        Assert.That(next.RoleOverride, Is.EqualTo(PartyRole.CasterDps));
    }

    [Test]
    public void AScenarioNotOpenedYetTakesTheFightsLastStratByName()
    {
        var (picked, other, name) = Catalog.Scenarios
            .Where(s => ScenarioPicker.StratGroups(s).Count == 0 && s.AiStrats.Count > 1)
            .SelectMany(s => Catalog.Scenarios.Where(o => o != s && o.Phase.Zone == s.Phase.Zone && ScenarioPicker.StratGroups(o).Count == 0)
                .SelectMany(o => o.AiStrats.Skip(1).Where(a => s.AiStrats.Skip(1).Any(b => b.Name == a.Name)).Select(a => (s, o, a.Name))))
            .First();
        var picker = new ScenarioPicker();
        picker.Select(picked);
        picker.SelectStrat(picked.AiStrats.ToList().FindIndex(a => a.Name == name));
        picker.Select(other);
        Assert.That(other.AiStrats[picker.Strat].Name, Is.EqualTo(name));
    }

    [Test]
    public void AStratThatNoLongerExistsFallsBackToTheFirst()
    {
        var picks = new PickMemory();
        picks.Strats[MultiStrat.FullName()] = new StratPick(null, "a strat that was removed");
        var picker = new ScenarioPicker(null, picks);
        picker.Select(MultiStrat);
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
