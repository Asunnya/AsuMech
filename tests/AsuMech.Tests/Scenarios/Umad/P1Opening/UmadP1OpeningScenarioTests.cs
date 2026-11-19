using System.Numerics;
using AsuMech.Scenarios.Umad.P1Opening;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Tests.NegativeRun;
using ActionId = AsuMech.Scenarios.Umad.UmadConstants.ActionId;
using Scenario = AsuMech.Scenarios.Umad.P1Opening.UmadP1OpeningScenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class UmadP1OpeningScenarioTests
{
    // Melee A's laser line spot is (2.5, 3); its tower drops there.
    [Test]
    public void StayingInYourOwnTowerWithTheLaserVulnKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<UmadP1OpeningStateOverrides>(o => o.LaserOn = MeleeDpsA)
            .TeleportAt(Scenario.TowersAt + 0.5f, to: new Vector2(2.5f, 3f))
            .ShouldKill(ActionId.Explosion, MeleeDpsA);

    [Test]
    public void StandingInTheHalfTheWillHitsKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<UmadP1OpeningStateOverrides>(o => o.FirstWillEast = true)
            .TeleportAt(Scenario.WillAt[0] - 0.5f, to: new Vector2(6f, 4f))
            .ShouldKill(ActionId.IntemperateWill, RegenHealer);

    // The first gravity puddles sit 3.5y either side of (0, -11).
    [Test]
    public void ARockDroppedOnTheGravityPuddlesWipes()
        => Negative<Scenario>(RegenHealer)
            .Overrides<UmadP1OpeningStateOverrides>(o => o.FirstGravitasOnDps = true)
            .TeleportAt(Scenario.VitrophyreAt[0] - 0.4f, to: new Vector2(0f, -11f))
            .ShouldKillSomeone(ActionId.GravitationalExplosion);

    [Test]
    public void AnUnsoakedTowerWipes()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<UmadP1OpeningStateOverrides>(o => o.LaserOn = MeleeDpsA)
            .MoveBotAt(Scenario.TowersAt + 0.3f, MeleeDpsB, new Vector2(0f, -15f))
            .MoveBotAt(Scenario.TowersAt + 0.3f, PhysRangedDps, new Vector2(0f, -15f))
            .MoveBotAt(Scenario.TowersAt + 0.3f, CasterDps, new Vector2(0f, -15f))
            .TeleportAt(Scenario.TowersAt + 0.3f, to: new Vector2(0f, -15f))
            .ShouldKillSomeone(ActionId.UnmitigatedExplosion);

    [Test]
    public void HyperdriveWithoutAnInvulnKillsTheTank()
        => Negative<Scenario>(OffTank)
            .WithMitigationChecks()
            .TeleportAt(Scenario.FirstHyperdriveAt[0] - 2f, to: new Vector2(0f, -5f))
            .ShouldKill(ActionId.Hyperdrive, OffTank);
}
