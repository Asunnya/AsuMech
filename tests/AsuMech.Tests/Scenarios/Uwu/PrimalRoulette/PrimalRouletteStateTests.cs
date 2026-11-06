using System.Linq;
using System.Numerics;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Uwu.PrimalRoulette;

namespace AsuMech.Tests;

public class PrimalRouletteStateTests
{
    [Test]
    public void EveryOrderHasEachPrimalOnce()
    {
        foreach (var order in PrimalRouletteState.Orders)
            Assert.That(order.Distinct().Count(), Is.EqualTo(3));
    }

    [Test]
    public void IfritIsNeverLastSoItsDashAlwaysStartsFromTwo()
    {
        foreach (var order in PrimalRouletteState.Orders)
            Assert.That(order[^1], Is.Not.EqualTo(Primal.Ifrit));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void OnlyTheSegmentAfterIfritStartsOnTheIntercard(int order)
    {
        var state = new PrimalRouletteState(new Rng(order), null, new PrimalRouletteStateOverrides { Order = order });
        for (var segment = 0; segment < 3; segment++)
        {
            var afterIfrit = segment > 0 && state.Order[segment - 1] == Primal.Ifrit;
            Assert.That(state.StartAnchor(segment), Is.EqualTo(afterIfrit ? PrimalRouletteState.NorthWest : PrimalRouletteState.North));
        }
    }

    [Test]
    public void TitansTwoMarkersAreFarEnoughApartToDodgeAWeight()
        => Assert.That(Vector2.Distance(PrimalRouletteState.North, PrimalRouletteState.NorthWest), Is.GreaterThan(6f + 2f));

    [Test]
    public void ThreeDifferentPlayersCarryViscousAetheroplasm()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var state = new PrimalRouletteState(new Rng(seed), null, new PrimalRouletteStateOverrides());
            Assert.That(state.ViscousTargets.Distinct().Count(), Is.EqualTo(3));
        }
    }

    [Test]
    public void ThePlayerCanAskToCarryViscousAetheroplasm()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var state = new PrimalRouletteState(new Rng(seed), AsuMech.Core.Game.Party.PartyRole.MeleeDpsB, new PrimalRouletteStateOverrides { ViscousOnPlayer = true });
            Assert.That(state.ViscousTargets, Does.Contain(AsuMech.Core.Game.Party.PartyRole.MeleeDpsB));
            Assert.That(state.ViscousTargets.Distinct().Count(), Is.EqualTo(3));
        }
    }
}
