using System.Numerics;
using AnoMech.Scenarios.Dsr.P2Thordan;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Dsr.DsrConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class DsrP2ThordanScenarioTests
{
    private static readonly AnoMech.Core.Game.Party.PartyRole[] Everyone =
        [MainTank, OffTank, RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    [Test]
    public void BroadSwingCutsRightLeftBackOrLeftRightBack()
    {
        Assert.That(DsrP2ThordanScenario.BroadSwingOffset(true, 0), Is.EqualTo(-MathF.PI / 3f));
        Assert.That(DsrP2ThordanScenario.BroadSwingOffset(true, 1), Is.EqualTo(MathF.PI / 3f));
        Assert.That(DsrP2ThordanScenario.BroadSwingOffset(false, 0), Is.EqualTo(MathF.PI / 3f));
        Assert.That(DsrP2ThordanScenario.BroadSwingOffset(false, 2), Is.EqualTo(MathF.PI));
    }

    [Test]
    public void StandingStillInTheStackDiesToMercyConcealed()
        => Negative<DsrP2ThordanScenario>(MeleeDpsA)
            .TeleportAt(13f, to: new Vector2(0f, 6f))
            .ShouldKill(ActionId.AscalonsMercyConcealedCone, MeleeDpsA);

    [Test]
    public void SharingALightningCircleKillsBoth()
        => Negative<DsrP2ThordanScenario>(MainTank)
            .Overrides<DsrP2ThordanStateOverrides>(o =>
            {
                o.SafeLineBearing = 0f;
                o.GuerriqueBearing = 90f;
            })
            .TeleportAt(43f, to: new Vector2(0f, -11.8f))
            .ShouldKill(ActionId.LightningStormHit, MainTank, OffTank);

    [Test]
    public void StandingOnGuerriqueDiesToTheFirstHeavyImpact()
        => Negative<DsrP2ThordanScenario>(CasterDps)
            .Overrides<DsrP2ThordanStateOverrides>(o =>
            {
                o.SafeLineBearing = 90f;
                o.GuerriqueBearing = 90f;
            })
            .TeleportAt(44f, to: new Vector2(12.8f, 0f))
            .ShouldKill(ActionId.HeavyImpact, CasterDps);

    [Test]
    public void TakingTheOneSwordPlungeAloneIsLethal()
        => Negative<DsrP2ThordanScenario>(MeleeDpsA)
            .Overrides<DsrP2ThordanStateOverrides>(o =>
            {
                o.SanctityClockwise = true;
                o.DarkKnightBearing = 45f;
                o.FirstPlungeTarget = MeleeDpsA;
                o.SecondPlungeTarget = OffTank;
            })
            .TeleportAt(112f, to: Flat(AtBearing(147f, 20f)))
            .ShouldKill(ActionId.SacredSever, MeleeDpsA);

    [Test]
    public void DroppingMeteorsOnTheSameSpotSetsOffHolyImpact()
        => Negative<DsrP2ThordanScenario>(MeleeDpsA)
            .Overrides<DsrP2ThordanStateOverrides>(o => o.MeteorTargets = [MeleeDpsA, MeleeDpsB])
            .TeleportAt(137f, to: new Vector2(0f, -19f))
            .ShouldKill(ActionId.HolyImpact, Everyone);

    [Test]
    public void MissingTheKnockbackIntoTheLastTowersWipes()
        => Negative<DsrP2ThordanScenario>(RegenHealer)
            .Overrides<DsrP2ThordanStateOverrides>(o => o.MeteorTargets = [MeleeDpsA, MeleeDpsB])
            .TeleportAt(140f, to: Flat(AtBearing(22.5f, 1f)))
            .ShouldKill(ActionId.EternalConviction, Everyone);

    [Test]
    public void StandingInAnotherDefamationKillsBothDefamationTargets()
        => Negative<DsrP2ThordanScenario>(RegenHealer)
            .Overrides<DsrP2ThordanStateOverrides>(o =>
            {
                o.ThordanBearing = 0f;
                o.Defamations = [RegenHealer, ShieldHealer, MeleeDpsA];
            })
            .TeleportAt(59f, to: new Vector2(0f, 20f))
            .ShouldKill(ActionId.SkywardLeap, RegenHealer, ShieldHealer);

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);
}
