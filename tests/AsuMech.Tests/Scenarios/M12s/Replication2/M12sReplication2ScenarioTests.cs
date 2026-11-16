using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.Replication2;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.Replication2.M12sReplication2Scenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sReplication2ScenarioTests
{
    [Test]
    public void EveryRoleGetsItsOwnCloneAndTheBossClonesTwoOfEachMechanic()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M12sReplication2State(new Rng(seed));
            Assert.That(state.CloneSpot.Values, Is.EquivalentTo(Enumerable.Range(0, M12sReplication2State.PlayerClones)));
            foreach (var mechanic in Enum.GetValues<CloneMechanic>())
                Assert.That(state.CloneMechanics.Count(m => m == mechanic), Is.EqualTo(2));
            Assert.That(state.FirstHolders.Distinct().Count(), Is.EqualTo(M12sReplication2State.BossClones + 1));
        }
    }

    [Test]
    public void ThePlayerGetsTheClonePicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        for (var spot = 0; spot < M12sReplication2State.PlayerClones; spot++)
        {
            var state = new M12sReplication2State(new Rng(spot * 11 + (int)player), new M12sReplication2StateOverrides { PlayerSpot = spot }, player);
            Assert.That(state.CloneSpot[player], Is.EqualTo(spot));
            Assert.That(state.CloneSpot.Values.Distinct().Count(), Is.EqualTo(M12sReplication2State.PlayerClones));
        }
    }

    [Test]
    public void StandingNextToADefamationKills()
        => Negative<Scenario>(MainTank)
            .Overrides<M12sReplication2StateOverrides>(o => o.PlayerSpot = 6)
            .TeleportAt(Scenario.ManaBurstAt - 0.5f, to: new Vector2(-15f, 2f))
            .ShouldKill(ActionId.ManaBurst, MainTank);

    [Test]
    public void StayingInFrontOfLindwurmForSnakingKickKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M12sReplication2StateOverrides>(o => o.PlayerSpot = 2)
            .TeleportAt(Scenario.SnakingKickCastAt + 3f, to: new Vector2(0f, -4f))
            .ShouldKill(ActionId.SnakingKick, RegenHealer);

    // The south clone took no tether, so it replays a defamation.
    [Test]
    public void TheSouthCloneReplaysAnUntelegraphedDefamation()
        => Negative<Scenario>(CasterDps)
            .Overrides<M12sReplication2StateOverrides>(o => o.PlayerSpot = 7)
            .TeleportAt(Scenario.ReplayAt[0] + 0.5f, to: new Vector2(0f, 8f))
            .ShouldKill(ActionId.ManaBurstReplay, CasterDps);

    [Test]
    public void TheNortheastCloneReplaysItsConeThroughTheMiddle()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M12sReplication2StateOverrides>(o => o.PlayerSpot = 3)
            .TeleportAt(Scenario.ReplayAt[1] + Scenario.ReplayDefamationAfter + 0.1f, to: new Vector2(0f, 0f))
            .ShouldKill(ActionId.HemorrhagicProjectionReplay, PhysRangedDps);

    private static IEnumerable<int> Spots() => Enumerable.Range(0, M12sReplication2State.PlayerClones);

    // Whichever clone the player is tied to, the strat clears with nobody hit by a cone or a defamation.
    [TestCaseSource(nameof(Spots))]
    public void EveryCloneClears(int spot)
        => Negative<Scenario>(MeleeDpsA)
            .Seed(spot * 7 + 1)
            .Overrides<M12sReplication2StateOverrides>(o => o.PlayerSpot = spot)
            .At(Scenario.DashAt, p =>
            {
                foreach (var role in Enum.GetValues<PartyRole>())
                    Assert.That(p.Member(role)!.IsAlive(), Is.True, $"{role} died");
            })
            .TeleportAt(Scenario.DashAt + 0.2f, to: new Vector2(0f, 25f))
            .ShouldKill(TheEnvironment, MeleeDpsA);
}
