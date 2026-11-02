using System;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Ucob.P2Nael;

namespace AnoMech.Tests;

public class UcobP2NaelStateTests
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    [Test]
    public void EachQuoteComesFromItsOwnPair([Range(0, 49)] int seed)
    {
        var state = new UcobP2NaelState(new Rng(seed));
        for (var i = 0; i < UcobP2NaelState.QuoteChoices.Length; i++)
            Assert.That(UcobP2NaelState.QuoteChoices[i], Does.Contain(state.Quotes[i]));
    }

    [Test]
    public void APickedQuoteIsKept([Range(0, 2)] int quote, [Values(0, 1)] int pick)
    {
        var overrides = new UcobP2NaelStateOverrides();
        overrides.Quotes[quote] = pick;
        Assert.That(new UcobP2NaelState(new Rng(7), overrides).Quotes[quote], Is.EqualTo(UcobP2NaelState.QuoteChoices[quote][pick]));
    }

    [Test]
    public void TheThirdFireSparesWhoeverTookTheSecondAlone([Range(0, 99)] int seed)
    {
        var state = new UcobP2NaelState(new Rng(seed));
        Assert.That(state.FireTargets, Has.Count.EqualTo(UcobP2NaelState.FireTethers));
        Assert.That(state.FireTargets[2], Is.Not.EqualTo(state.FireTargets[UcobP2NaelState.FireTakenAlone]));
    }

    [Test]
    public void EveryoneTakesOneMeteorStreamAndThreeIceballs([Range(0, 49)] int seed)
    {
        var state = new UcobP2NaelState(new Rng(seed));
        Assert.That(state.MeteorOrder, Is.EquivalentTo(Everyone));
        Assert.That(state.IceOrder, Is.EquivalentTo(Everyone));
    }

    [Test]
    public void DoomsAndThundersMarkDistinctPlayers([Range(0, 49)] int seed)
    {
        var state = new UcobP2NaelState(new Rng(seed));
        Assert.That(state.DoomTargets.Select(t => t.Count), Is.EqualTo(UcobP2NaelState.DoomCounts));
        Assert.That(state.ThunderTargets.Select(t => t.Count), Is.EqualTo(UcobP2NaelState.ThunderCounts));
        foreach (var set in state.DoomTargets.Concat(state.ThunderTargets)) Assert.That(set, Is.Unique);
    }

    [Test]
    public void WingsSkipThatSetsDoomsAndThunders([Range(0, 99)] int seed)
    {
        var state = new UcobP2NaelState(new Rng(seed));
        for (var set = 0; set < state.WingsTargets.Count; set++)
        {
            Assert.That(state.WingsTargets[set], Has.Count.EqualTo(UcobP2NaelState.WingsCounts[set]));
            Assert.That(state.WingsTargets[set].Intersect(state.DoomTargets[set]), Is.Empty);
            if (set > 0) Assert.That(state.WingsTargets[set].Intersect(state.ThunderTargets[set + 1]), Is.Empty);
        }
    }

    [Test]
    public void PickedRolesAreAmongTheirSets([ValueSource(nameof(Everyone))] PartyRole role)
    {
        var overrides = new UcobP2NaelStateOverrides();
        overrides.FirePicks[0] = role;
        overrides.ThunderIncludes[3] = role;
        overrides.DoomIncludes[1] = role;
        var state = new UcobP2NaelState(new Rng(11), overrides);
        Assert.That(state.FireTargets[0], Is.EqualTo(role));
        Assert.That(state.ThunderTargets[3], Does.Contain(role));
        Assert.That(state.DoomTargets[1], Does.Contain(role));
    }

    [Test]
    public void FiveDragonsTakeDistinctBearings([Range(0, 49)] int seed)
    {
        var slots = new UcobP2NaelState(new Rng(seed)).DragonSlots;
        Assert.That(slots, Has.Count.EqualTo(5).And.Unique);
        Assert.That(slots.All(s => s is >= 0 and < 8));
    }
}
