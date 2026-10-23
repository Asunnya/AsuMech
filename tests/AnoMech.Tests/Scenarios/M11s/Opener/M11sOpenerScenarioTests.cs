using System.Numerics;
using AnoMech.Scenarios.M11s;
using AnoMech.Scenarios.M11s.Opener;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M11s.M11sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M11s.Opener.M11sOpenerScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M11sOpenerScenarioTests
{
    // Scythe trophy; first set scythe (0,12), sword (-10.4,-6), axe (10.4,-6); stack first on H1.
    private static void Pinned(M11sOpenerStateOverrides o)
    {
        o.AxeTrophyFirst = false;
        o.FirstWeapons = [Weapon.Scythe, Weapon.Sword, Weapon.Axe];
        o.FirstWeaponStep = 0;
        o.SecondWeapons = [Weapon.Axe, Weapon.Scythe, Weapon.Sword];
        o.SecondWeaponStep = 6;
        o.StackFirst = true;
        o.CrushingCometTarget = RegenHealer;
    }

    [Test]
    public void StandingInATanksConeKills()
        => Negative<Scenario>(ShieldHealer)
            .Overrides<M11sOpenerStateOverrides>(Pinned)
            .TeleportAt(Scenario.RawSteelHitAt - 0.5f, to: new Vector2(-4f, -4.5f))
            .ShouldKill(ActionId.RawSteelScytheCone, ShieldHealer);

    [Test]
    public void OverlappingAxeSpreadsKillBoth()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sOpenerStateOverrides>(o => { Pinned(o); o.AxeTrophyFirst = true; })
            .TeleportAt(Scenario.RawSteelHitAt - 0.5f, to: new Vector2(-6f, 12f))
            .ShouldKill(ActionId.Impact, MeleeDpsA, RegenHealer);

    [Test]
    public void LeavingTheScythesFootKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sOpenerStateOverrides>(Pinned)
            .TeleportAt(Scenario.StrikeAt[0][0] + 0.2f, to: new Vector2(6f, 6f))
            .ShouldKill(ActionId.AssaultEvolvedScythe, MeleeDpsA);

    [Test]
    public void StandingOnTheSwordsCrossKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sOpenerStateOverrides>(Pinned)
            .TeleportAt(Scenario.StrikeAt[0][1] + 0.2f, to: new Vector2(-15.6f, -3f))
            .ShouldKill(ActionId.AssaultEvolvedSword, MeleeDpsA);

    [Test]
    public void StandingInTheAxesCircleKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sOpenerStateOverrides>(Pinned)
            .TeleportAt(Scenario.StrikeAt[0][2] + 0.2f, to: new Vector2(10f, -6f))
            .ShouldKill(ActionId.AssaultEvolvedAxe, MeleeDpsA);

    [Test]
    public void StandingStillUnderTheCometsKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sOpenerStateOverrides>(Pinned)
            .FreezeAt(Scenario.CometiteAt[0][0] - 0.3f)
            .ShouldKill(ActionId.Cometite, MeleeDpsA);

    [Test]
    public void TakingCrushingCometAloneKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M11sOpenerStateOverrides>(Pinned)
            .TeleportAt(Scenario.CometCastAt[0] + 4f, to: new Vector2(15f, 15f))
            .ShouldKill(ActionId.CrushingComet, RegenHealer);

    [Test]
    public void OverlappingCometSpreadsKillBoth()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sOpenerStateOverrides>(o => { Pinned(o); o.StackFirst = false; })
            .TeleportAt(Scenario.CometCastAt[0] + 4.5f, to: new Vector2(-5f, -6f))
            .ShouldKill(ActionId.Comet, MeleeDpsA, MainTank);
}
