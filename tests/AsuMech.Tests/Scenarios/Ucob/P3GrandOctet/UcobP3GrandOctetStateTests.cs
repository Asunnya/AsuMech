using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3GrandOctet;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Tests;

public class UcobP3GrandOctetStateTests
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    [Test]
    public void TheEightTakeTheEightSpots([Range(0, 49)] int seed)
    {
        var state = new UcobP3GrandOctetState(new Rng(seed));
        var slots = new[] { state.BahamutSlot, state.NaelSlot, state.TwintaniaSlot }.Concat(state.DragonSlots);
        Assert.That(slots, Is.EquivalentTo(Enumerable.Range(0, UcobP3GrandOctetState.SlotCount)));
    }

    [Test]
    public void EveryoneCarriesOneBossMarker([Range(0, 49)] int seed)
    {
        var state = new UcobP3GrandOctetState(new Rng(seed));
        var markers = state.DragonTargets.Prepend(state.NaelTarget).Append(state.BahamutTarget).Append(state.TwintaniaTarget);
        Assert.That(markers, Is.EquivalentTo(Everyone));
    }

    [Test]
    public void TheDragonsDiveInTurnAwayFromBahamut([Range(0, 49)] int seed)
    {
        var state = new UcobP3GrandOctetState(new Rng(seed));
        Assert.That(state.DiveOrder, Is.EquivalentTo(Enumerable.Range(0, UcobP3GrandOctetState.DragonCount)));
        var steps = state.DiveOrder.Select(d => state.StepsFromBahamut(state.DragonSlots[d])).ToList();
        Assert.That(steps, Is.Ordered.Ascending);
        Assert.That(steps, Is.All.InRange(1, 7));
    }

    [Test]
    public void ACardinalBahamutSendsThemCounterclockwise([Range(0, 7)] int slot)
    {
        var state = new UcobP3GrandOctetState(new Rng(slot), new UcobP3GrandOctetStateOverrides { BahamutSlot = slot });
        Assert.That(state.BahamutSlot, Is.EqualTo(slot));
        Assert.That(state.Counterclockwise, Is.EqualTo(slot % 2 == 0));
        Assert.That(state.StepsFromBahamut(slot + state.Step), Is.EqualTo(1));
    }

    [Test]
    public void FourStacksAndFourTwisters([Range(0, 49)] int seed)
    {
        var state = new UcobP3GrandOctetState(new Rng(seed));
        Assert.That(state.StackTargets, Has.Count.EqualTo(UcobP3GrandOctetState.StackCount).And.Unique);
        Assert.That(state.TwisterTargets, Has.Count.EqualTo(UcobP3GrandOctetState.TwisterCount).And.Unique);
    }

    [Test]
    public void PickedRolesAreKept([ValueSource(nameof(Everyone))] PartyRole role, [Range(0, 4)] int seed)
    {
        var state = new UcobP3GrandOctetState(new Rng(seed), new UcobP3GrandOctetStateOverrides { TwintaniaTarget = role, StackInclude = role });
        Assert.That(state.TwintaniaTarget, Is.EqualTo(role));
        Assert.That(state.StackTargets, Does.Contain(role));
        Assert.That(state.DragonTargets.Append(state.NaelTarget).Append(state.BahamutTarget), Does.Not.Contain(role));
    }
}
