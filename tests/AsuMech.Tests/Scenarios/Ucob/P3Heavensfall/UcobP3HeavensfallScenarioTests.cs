using System.Numerics;
using AsuMech.Scenarios.Ucob.P3Heavensfall;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
// Nael lands north between Bahamut Prime (northwest) and Twintania (northeast).
public class UcobP3HeavensfallScenarioTests
{
    private static void NaelNorthBetween(UcobP3HeavensfallStateOverrides o)
    {
        o.NaelStep = 0;
        o.Formation = HeavensfallFormation.NaelBetween;
        o.BurstsClockwise = true;
    }

    [Test]
    public void StandingInMegaflareDiveKills()
        => Negative<UcobP3HeavensfallScenario>(MeleeDpsA)
            .Overrides<UcobP3HeavensfallStateOverrides>(NaelNorthBetween)
            .TeleportAt(15.5f, to: new Vector2(7.07f, 7.07f))
            .ShouldKill(ActionId.MegaflareDive, MeleeDpsA);

    [Test]
    public void StayingOnYourTwisterSetsItOff()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthBetween(o);
                o.TwisterIncludes = CasterDps;
            })
            .FreezeAt(17f)
            .ShouldKill(ActionId.TwisterBlast, CasterDps);

    [Test]
    public void StayingInYourMegaflarePuddleKills()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthBetween(o);
                o.MegaflarePuddleIncludes = CasterDps;
            })
            .FreezeAt(19f)
            .ShouldKill(ActionId.MegaflarePuddle, CasterDps);

    [Test]
    public void LeavingYourTowerUnsoakedWipes()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(NaelNorthBetween)
            .TeleportAt(25.5f, to: new Vector2(0f, 0f))
            .ShouldKill(ActionId.MegaflareStrike, AllBut());

    [Test]
    public void StandingInTheFirstSliceKills()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(NaelNorthBetween)
            .TeleportAt(30.5f, to: new Vector2(7.07f, -7.07f))
            .ShouldKill(ActionId.ThermionicBurst, CasterDps);

    [Test]
    public void TakingTheFireballAloneKills()
        => Negative<UcobP3HeavensfallScenario>(CasterDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                NaelNorthBetween(o);
                o.FireballTarget = CasterDps;
            })
            .TeleportAt(37f, to: new Vector2(10f, 0f))
            .ShouldKill(ActionId.Fireball, CasterDps);

    [Test]
    public void TheBotsClearEveryFormationAndSweep([Values] HeavensfallFormation formation, [Values] bool clockwise, [Range(0, 3)] int seed)
        => Negative<UcobP3HeavensfallScenario>(PhysRangedDps)
            .Overrides<UcobP3HeavensfallStateOverrides>(o =>
            {
                o.Formation = formation;
                o.BurstsClockwise = clockwise;
            })
            .Seed(seed)
            .ShouldKillNoOneElse();
}
