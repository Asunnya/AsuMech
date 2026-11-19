using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.M12s.Act3;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.Act3.M12sAct3Scenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sAct3ScenarioTests
{
    [Test]
    public void StandingWhereTheFloorFallsKills()
        => Negative<Scenario>(MeleeDpsB)
            .Overrides<M12sAct3StateOverrides>(o => o.CardinalPlatforms = true)
            .TeleportAt(Scenario.BringDownTheHouseAt - 0.5f, to: Vector2.Zero)
            .ShouldKill(ActionId.BringDownTheHouseLarge, MeleeDpsB);

    [Test]
    public void AHealerClosestToASnakeHeadTakesTheLineWithTheRangedBehindAndBothDie()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M12sAct3StateOverrides>(o => o.CardinalPlatforms = true)
            .TeleportAt(Scenario.SplitScourgeHitsAt - 1f, to: new Vector2(-12f, -3f))
            .MoveBotAt(Scenario.SplitScourgeHitsAt - 1f, MainTank, new Vector2(0f, 0f))
            .ShouldKill(ActionId.SplitScourge, RegenHealer, PhysRangedDps);

    [Test]
    public void ATankAmongTheWestmostTakesVenomousScourgeOnItsVulnerability()
        => Negative<Scenario>(MainTank)
            .Overrides<M12sAct3StateOverrides>(o => o.CardinalPlatforms = true)
            .TeleportAt(Scenario.VenomousScourgeAt - 1f, to: new Vector2(-14f, -3f))
            .ShouldKill(ActionId.VenomousScourge, MainTank);

    private static IEnumerable<int> Seeds() => Enumerable.Range(0, 30);

    // The player falls off at the end; until then everyone lives.
    [TestCaseSource(nameof(Seeds))]
    public void TheStratClearsThePlatformsTheHeadsAndTheSpreads(int seed)
        => Negative<Scenario>(PhysRangedDps)
            .Seed(seed)
            .At(Scenario.FixerCastAt, p =>
            {
                foreach (var role in Enum.GetValues<PartyRole>())
                {
                    Assert.That(p.Member(role)!.IsAlive(), Is.True, $"{role} died");
                    Assert.That(p.Member(role)!.HasStatus(StatusId.DamageDown), Is.False, $"{role} walked over a snake");
                }
            })
            .TeleportAt(Scenario.FixerCastAt + 0.5f, to: new Vector2(0f, 18f))
            .ShouldKill(TheEnvironment, PhysRangedDps);
}
