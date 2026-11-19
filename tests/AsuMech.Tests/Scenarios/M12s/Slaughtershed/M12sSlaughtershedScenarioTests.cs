using System.Numerics;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.Slaughtershed.M12sSlaughtershedScenario;
using Overrides = AsuMech.Scenarios.M12s.Slaughtershed.M12sSlaughtershedStateOverrides;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sSlaughtershedScenarioTests
{
    private static float FirstRound => Scenario.SlaughtershedAt[0];

    [Test]
    public void BaitingEveryOrbOnYourSideKillsOnTheSecond()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<Overrides>(o => o.PurpleEast = true)
            .Seed(1)
            .TeleportAt(Scenario.OrbsAppearAt[0] + 1f, to: new Vector2(-4f, -13f))
            .ShouldKill(ActionId.MortalSlayerGreen, MeleeDpsA);

    [Test]
    public void StandingInTheMiddleThroughTheCleavesKills()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<Overrides>(o => o.SerpentineRound = 0)
            .TeleportAt(FirstRound + Scenario.BurstHitsAfter + 0.5f, to: new Vector2(8f, -3f))
            .ShouldKill(ActionId.SerpentineScourgeRect, PhysRangedDps);

    [Test]
    public void TakingTheKnucklesFromTheMiddleThrowsYouOffTheStage()
        => Negative<Scenario>(CasterDps)
            .Overrides<Overrides>(o => o.SerpentineRound = 2)
            .TeleportAt(FirstRound + Scenario.BurstHitsAfter + 0.5f, to: new Vector2(0f, -3f))
            .ShouldKill(TheEnvironment, CasterDps);

    [Test]
    public void LeavingTheStackShortKillsIt()
        => Negative<Scenario>(MainTank)
            .Overrides<Overrides>(o => o.PlayerSpreads = false)
            .TeleportAt(FirstRound + Scenario.StackSpreadAfter - 0.3f, to: new Vector2(0f, -3f))
            .ShouldKillSomeone(ActionId.CurtainCallFourthWallFusion);
}
