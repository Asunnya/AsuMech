using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Umad.P3DecisiveBattle;

namespace AsuMech.Tests;

public class UmadP3DecisiveBattleStateTests
{
    private static IEnumerable<UmadP3DecisiveBattleState> Seeds(UmadP3DecisiveBattleStateOverrides? overrides = null)
        => Enumerable.Range(0, 300).Select(seed => new UmadP3DecisiveBattleState(new Rng(seed), overrides));

    [Test]
    public void FireAndWaterFlankTheWindCrystalWithTheEmptySpotOppositeIt()
    {
        foreach (var state in Seeds())
        {
            Assert.That(new[] { state.WindSpot, state.FireSpot, state.WaterSpot }.Distinct().Count(), Is.EqualTo(3));
            Assert.That(new[] { (state.WindSpot + 1) % 4, (state.WindSpot + 3) % 4 }, Is.EquivalentTo(new[] { state.FireSpot, state.WaterSpot }));
        }
    }

    [Test]
    public void EachElementGoesToOneSupportAndOneDpsWithNoOneHoldingBoth()
    {
        foreach (var state in Seeds())
        {
            Assert.That(state.Entropy.Support.IsDps() || state.DynamicFluid.Support.IsDps(), Is.False);
            Assert.That(state.Entropy.Dps.IsDps() && state.DynamicFluid.Dps.IsDps());
            Assert.That(state.Entropy.Support, Is.Not.EqualTo(state.DynamicFluid.Support));
            Assert.That(state.Entropy.Dps, Is.Not.EqualTo(state.DynamicFluid.Dps));
            Assert.That(state.Headwinds.Count, Is.InRange(2, 5));
        }
    }

    [Test]
    public void AForcedRoleHoldsItsElement()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            foreach (var state in Seeds(new UmadP3DecisiveBattleStateOverrides { EntropyOn = role }))
                Assert.That(state.HoldsEntropy(role));
            foreach (var state in Seeds(new UmadP3DecisiveBattleStateOverrides { DynamicFluidOn = role }))
                Assert.That(state.HoldsDynamicFluid(role));
        }
    }

    [Test]
    public void PinnedCrystalsAndImplosionHold()
    {
        for (var spot = 0; spot < 4; spot++)
            foreach (var state in Seeds(new UmadP3DecisiveBattleStateOverrides { WindCrystalSpot = spot, FireClockwiseOfWind = true, FireShort = true, Latitudinal = false }))
            {
                Assert.That(state.WindSpot, Is.EqualTo(spot));
                Assert.That(state.FireSpot, Is.EqualTo((spot + 1) % 4));
                Assert.That(state.FireShort && !state.Latitudinal);
            }
    }
}
