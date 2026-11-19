using System.Numerics;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.IdyllicDream;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.M12s.M12sConstants;
using static AsuMech.Tests.NegativeRun;
using Scenario = AsuMech.Scenarios.M12s.IdyllicDream.M12sIdyllicDreamScenario;

namespace AsuMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M12sIdyllicDreamScenarioTests
{
    [Test]
    public void StandingUnderTheBossWhenTheClonesRepeatIsKicked()
        => Negative<Scenario>(MeleeDpsA)
            .TeleportAt(Scenario.ReplayAt, to: Vector2.Zero)
            .ShouldKill(ActionId.SnakingKickReplay, MeleeDpsA);

    [Test]
    public void TheGapBetweenThePlatformsKills()
        => Negative<Scenario>(PhysRangedDps)
            .TeleportAt(Scenario.TowersShowAt, to: Vector2.Zero)
            .ShouldKill(TheEnvironment, PhysRangedDps);

    [Test]
    public void TakingYourDefamationWithYourGroupKillsThem()
        => Negative<Scenario>(CasterDps)
            .Overrides<M12sIdyllicDreamStateOverrides>(o => { o.PlayerCloneDirection = 4; o.CardinalBossClonesDefa = true; })
            .TeleportAt(Scenario.SlotAt[0] + 0.5f, to: new Vector2(0f, -16.5f))
            .ShouldKillSomeone(ActionId.IdyllicManaBurst);

    [Test]
    public void LeavingYourTowerSetsItOffOnEveryone()
        => Negative<Scenario>(MainTank)
            .TeleportAt(Scenario.SplitAt[1] - 1f, to: new Vector2(-14f, 0f))
            .ShouldKill(ActionId.IdyllicUnmitigatedExplosion, PerRole.All);

    [Test]
    public void MovingWhileHotBloodedBurns()
        => Negative<Scenario>(OffTank)
            .At(Scenario.TowerStatusesAt + 1f, p => p.Member(OffTank)!.AddStatus(StatusId.HotBlooded, 5f))
            .TeleportAt(Scenario.TowerStatusesAt + 2f, to: new Vector2(14f, -8f))
            .ShouldKill(ActionId.PyreticWurm, OffTank);

    [Test]
    public void DoomWithNoEsunaKills()
        => Negative<Scenario>(MeleeDpsB)
            .Seed(1)
            .At(142f, p => p.Member(MeleeDpsB)!.AddStatus(StatusId.Doom, 8f))
            .ShouldKill(ActionId.LindwurmsDarkII, MeleeDpsB);

    [Test]
    public void ALoneTankLeavesTheOtherAloneInTheReplayedStack()
        => Negative<Scenario>(OffTank)
            .Overrides<M12sIdyllicDreamStateOverrides>(o => o.CardinalClonesFirst = true)
            .TeleportAt(Scenario.ReenactAt[0] - 3f, to: new Vector2(12f, -12f))
            .ShouldKill(ActionId.ReenactedHeavySlam, MainTank);

    [Test]
    public void TwoInAPortentLineBothDie()
        => Negative<Scenario>(CasterDps)
            .At(Scenario.PortentsResolveAt - 0.5f, StepBehindTheNearbyPortentsTarget)
            .ShouldKillSomeone(ActionId.LindwurmsThunderII);

    private static void StepBehindTheNearbyPortentsTarget(ScenarioProbe p)
    {
        var members = PerRole.All.Select(p.Member).OfType<AsuMech.Core.SimObjects.SimCharacter>().ToList();
        var holder = members.First(m => m.HasStatus(StatusId.NearbyPortent));
        var target = members.Where(m => m != holder).OrderBy(m => Vector3.Distance(m.Position, holder.Position)).First();
        var extra = members.First(m => m != holder && m != target && !m.HasStatus(StatusId.LindwurmsPortent));
        extra.SetPosition(target.Position + Vector3.Normalize(target.Position - holder.Position));
    }

    [Test]
    public void StandingOnTheKickedPlatformKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M12sIdyllicDreamStateOverrides>(o => o.KickPlatform = -1)
            .TeleportAt(Scenario.SplitAt[2] + 0.5f, to: new Vector2(-14f, 0f))
            .ShouldKill(ActionId.SnakingKickReplay, RegenHealer);
}

public class M12sIdyllicDreamTowerMarksTests
{
    [Test]
    public void TheTowersLeaveTwoOfEachMarkAndTheTethersLockOnTheirTakers()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var counts = new Dictionary<ushort, int>();
            var held = 0;
            var options = new ScenarioRunOptions
            {
                WriteArtifactsOnFailure = false,
                Probe = p =>
                {
                    if (p.Crossed(Scenario.TowerStatusesAt + 0.5f))
                        foreach (var id in new[] { StatusId.NearbyPortent, StatusId.FarawayPortent, StatusId.HotBlooded, StatusId.Doom })
                            counts[id] = PerRole.All.Count(r => p.Member(r)?.HasStatus(id) == true);
                    if (p.Crossed(Scenario.TethersLockAt + 0.5f))
                        held = p.World.Children.OfType<AsuMech.Core.SimObjects.SimTether>().Count(t => t.IsActive && t.TetherId == TetherId.Bound);
                },
            };
            var run = ScenarioRun.Execute(typeof(Scenario), 0, seed, options);
            Assert.That(run.Passed, run.ToString);
            Assert.That(counts.Values, Is.All.EqualTo(2), $"seed {seed}");
            Assert.That(held, Is.EqualTo(8), $"seed {seed}");
        }
    }
}
