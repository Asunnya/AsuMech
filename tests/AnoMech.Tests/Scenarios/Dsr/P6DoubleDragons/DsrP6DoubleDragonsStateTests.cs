using System;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P6DoubleDragons;

namespace AnoMech.Tests;

public class DsrP6DoubleDragonsStateTests
{
    [Test]
    public void TethersSplitTheNonTanksThreeAndThree([Range(0, 49)] int seed)
    {
        var state = new DsrP6DoubleDragonsState(new Rng(seed));
        Assert.That(state.NidhoggTethers, Has.Count.EqualTo(3));
        Assert.That(state.HraesvelgrTethersAt(0).Count(), Is.EqualTo(3));
        Assert.That(state.NidhoggTethers.Concat(state.HraesvelgrTethersAt(0)).Any(r => r.IsTank()), Is.False);
    }

    [Test]
    public void MortalVowStartsOnADps([Range(0, 49)] int seed)
        => Assert.That(new DsrP6DoubleDragonsState(new Rng(seed)).VowTarget.IsDps());

    [Test]
    public void WrothFlamesHandsOutFourSpreadsAndTwoStacks([Range(0, 49)] int seed)
    {
        var state = new DsrP6DoubleDragonsState(new Rng(seed));
        Assert.That(state.Spreads, Has.Count.EqualTo(4));
        Assert.That(state.Stacks, Has.Count.EqualTo(2));
        Assert.That(state.Spreads.Intersect(state.Stacks), Is.Empty);
    }

    [Test]
    public void EachOrbSetHoldsOneOrbPerRowAndColumn([Range(0, 49)] int seed)
    {
        var state = new DsrP6DoubleDragonsState(new Rng(seed));
        for (var set = 0; set < 3; set++)
        {
            var orbs = state.OrbSet(set);
            Assert.That(orbs.Select(o => o.X).Distinct().Count(), Is.EqualTo(3));
            Assert.That(orbs.Select(o => o.Z).Distinct().Count(), Is.EqualTo(3));
        }
    }

    [Test]
    public void OrbSetsSitOnTheCentreAndTwoOppositeCorners()
    {
        var state = new DsrP6DoubleDragonsState(new Rng(1), new DsrP6DoubleDragonsStateOverrides { SecondSetCorner = (-1, 1) });
        Assert.That(state.OrbSet(0).All(o => Math.Abs(o.X) <= 6f && Math.Abs(o.Z) <= 6f));
        Assert.That(state.OrbSet(1).All(o => o.X is >= -19f and <= -7f && o.Z is >= 7f and <= 19f));
        Assert.That(state.OrbSet(2).All(o => o.X is >= 7f and <= 19f && o.Z is >= -19f and <= -7f));
    }

    [Test]
    public void AkhAfahStacksLandOnBothHealers([Values] bool onRegen)
    {
        var state = new DsrP6DoubleDragonsState(new Rng(1), new DsrP6DoubleDragonsStateOverrides { HraesvelgrStackOnRegen = onRegen });
        Assert.That(new[] { state.HraesvelgrStackTarget(0), state.NidhoggStackTarget(0) }, Is.EquivalentTo(new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer }));
    }
}
