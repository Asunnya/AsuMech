using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P1Twintania;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Tests;

public class UcobP1TwintaniaStateTests
{
    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    [Test]
    public void EachGenerateMarksDistinctDps([Range(0, 49)] int seed)
    {
        var state = new UcobP1TwintaniaState(new Rng(seed));
        Assert.That(state.HatchTargets.Select(t => t.Count), Is.EqualTo(UcobP1TwintaniaState.HatchCounts));
        foreach (var targets in state.HatchTargets)
        {
            Assert.That(targets, Is.Unique);
            Assert.That(targets, Is.SubsetOf(Dps));
        }
    }

    [Test]
    public void APickedRoleIsAmongThatGeneratesTargets([ValueSource(nameof(Dps))] PartyRole role, [Range(0, 4)] int set)
    {
        var overrides = new UcobP1TwintaniaStateOverrides();
        overrides.HatchIncludes[set] = role;
        for (var seed = 0; seed < 20; seed++)
            Assert.That(new UcobP1TwintaniaState(new Rng(seed), overrides).HatchTargets[set], Does.Contain(role));
    }

    [Test]
    public void TheTargetedLiquidHellSkipsThePlayersWaitingInANeurolink([Range(0, 49)] int seed)
    {
        var state = new UcobP1TwintaniaState(new Rng(seed));
        Assert.That(state.HatchTargets[UcobP1TwintaniaState.HatchesDuringTargetedLiquidHell], Does.Not.Contain(state.LiquidHellTarget));
    }

    [Test]
    public void FireballsMarkNonTanks([Range(0, 49)] int seed)
    {
        var state = new UcobP1TwintaniaState(new Rng(seed));
        Assert.That(state.FireballTargets, Has.Count.EqualTo(2));
        Assert.That(state.FireballTargets, Has.None.EqualTo(MainTank).And.None.EqualTo(OffTank));
    }

    [Test]
    public void APickedFireballTargetIsKept([Values(RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps)] PartyRole role)
    {
        var overrides = new UcobP1TwintaniaStateOverrides();
        overrides.FireballTargets[1] = role;
        Assert.That(new UcobP1TwintaniaState(new Rng(3), overrides).FireballTargets[1], Is.EqualTo(role));
    }

    [Test]
    public void EachTwisterCastDropsFourUnderDifferentPlayers([Range(0, 49)] int seed)
    {
        var state = new UcobP1TwintaniaState(new Rng(seed));
        Assert.That(state.TwisterTargets, Has.Count.EqualTo(3));
        foreach (var targets in state.TwisterTargets)
        {
            Assert.That(targets, Has.Count.EqualTo(UcobTwisters.Count));
            Assert.That(targets, Is.Unique);
        }
    }
}
