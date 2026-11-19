using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Fru.P1UtopianSky;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Pinned: Cyclonic Break and Utopian Sky lightning, the N-S line safe, Sinbound Fire III on the tanks.
//
//   10.05 Cyclonic Break cones at each player on the clock: MT N 5y, R2 NE 16y, SH E 15y, M2 SE 6y,
//         OT S 12y, M1 SW 8y, RH W 13y, R NW 12y; the same cones 2s later, then turned 22.5° and 45°
//         clockwise. 12.19 Sinsmite on everyone, a half-cone clockwise of their spoke.
//   36.1  Burn Mark on MT (0,-17) and the nearest, OT (9.9,-9.9).
//   44.9  Blasting Zone from the images on the NE-SW, E-W and NW-SE lines.
//   45.58 Sinbound Thunder III: MT's light party at the north end, OT's at the south.
public class FruP1UtopianSkyScenarioTests
{
    private static NegativeRun<FruP1UtopianSkyScenario> UtopianSky(PartyRole player, Action<FruP1UtopianSkyStateOverrides>? tweak = null)
        => Negative<FruP1UtopianSkyScenario>(player)
            .Overrides<FruP1UtopianSkyStateOverrides>(o =>
            {
                o.CyclonicFire = false;
                o.UtopianFire = false;
                o.SafeAxis = 0;
                o.FireOnTanks = true;
                tweak?.Invoke(o);
            });

    [Test]
    public void StayingInTheBaitedConeDies()
        => UtopianSky(MainTank)
            .FreezeAt(10.2f)
            .ShouldKill(ActionId.CyclonicBreakRepeat, MainTank);

    [Test]
    public void TwoInOneBaitConeDie()
        => UtopianSky(MainTank)
            .TeleportAt(9.8f, to: new(7, 0))
            .ShouldKill(ActionId.CyclonicBreakBait, MainTank, ShieldHealer);

    [Test]
    public void SinsmiteSpreadsOverlappingDie()
        => UtopianSky(MeleeDpsA)
            .TeleportAt(11.9f, to: new(-11f, -4.6f))
            .ShouldKill(ActionId.CyclonicSinsmite, MeleeDpsA, RegenHealer);

    [Test]
    public void SinsmokeWithoutAPartnerDies()
        => UtopianSky(PhysRangedDps, o => o.CyclonicFire = true)
            .TeleportAt(11.9f, to: new(-11.09f, -4.59f))
            .ShouldKill(ActionId.CyclonicSinsmoke, PhysRangedDps);

    [Test]
    public void StandingNearestThePowderMarkDies()
        => UtopianSky(ShieldHealer)
            .TeleportAt(34.5f, to: new(0, -13))
            .ShouldKill(ActionId.BurnMark, ShieldHealer);

    [Test]
    public void StandingOnAnImagesLineDies()
        => UtopianSky(MeleeDpsA)
            .TeleportAt(44.5f, to: new(-10, 0))
            .ShouldKill(ActionId.BlastingZone, MeleeDpsA);

    [Test]
    public void SinboundThunderOverlappingDies()
        => UtopianSky(PhysRangedDps)
            .TeleportAt(45.2f, to: new(-1, -18))
            .ShouldKill(ActionId.SinboundThunderIII, MainTank, PhysRangedDps);

    [Test]
    public void SinboundFireAloneDies()
        => UtopianSky(MainTank, o => o.UtopianFire = true)
            .TeleportAt(45.2f, to: new(0, 5))
            .ShouldKill(ActionId.SinboundFireIII, MainTank);
}

public class FruP1UtopianSkyStateTests
{
    [Test]
    public void ImagesCutEveryLineButTheSafeOne([Values(0, 1, 2, 3)] int safeAxis, [Range(0, 9)] int seed)
    {
        var state = new FruP1UtopianSkyState(new Rng(seed), new FruP1UtopianSkyStateOverrides { SafeAxis = safeAxis });
        Assert.That(state.ActiveImages.Select(o => o % 4).Order(), Is.EqualTo(Enumerable.Range(0, 4).Where(a => a != safeAxis)));
    }

    [Test]
    public void SinboundFireTargetsOnePerLightParty([Values] bool onTanks)
    {
        var state = new FruP1UtopianSkyState(new Rng(1), new FruP1UtopianSkyStateOverrides { FireOnTanks = onTanks });
        Assert.That(state.FireTargets.Select(r => (int)r % 2).Order(), Is.EqualTo(new[] { 0, 1 }));
    }
}
