using System.Numerics;
using AnoMech.Scenarios.M10s.DeepVarial;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.DeepVarial.M10sDeepVarialScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M10sDeepVarialScenarioTests
{
    // The wave pushes south, so Deep Blue's wall is the south one; its lane is the east one.
    private static void Pinned(M10sDeepVarialStateOverrides o, bool playerFire)
    {
        o.PlayerFire = playerFire;
        o.WaveFromNorth = true;
        o.VarialSpread = false;
        o.Lane = 1;
        o.TakeoffSpread = false;
    }

    [Test]
    public void DeepVarialLeavesDamageDownAndTheFirstPuddlesBurst()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M10sDeepVarialStateOverrides>(o => { Pinned(o, playerFire: false); o.VarialSpread = true; })
            .TeleportAt(6f, to: new Vector2(0f, 8f))
            .At(Scenario.DeepVarialHitAt + 0.8f, p => Assert.That(p.Member(MeleeDpsA)!.HasStatus(StatusId.DamageDown), Is.True))
            .TeleportAt(Scenario.FirstSteamAt + 0.5f, to: new Vector2(12.6f, 0f))
            .ShouldKill(ActionId.SteamBurst, MeleeDpsA);

    [Test]
    public void StandingInTheBaitsLandingThenBaitingKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M10sDeepVarialStateOverrides>(o => Pinned(o, playerFire: true))
            .TeleportAt(Scenario.JumpAt[0] - 1f, to: new Vector2(17f, 17f))
            .ShouldKill(ActionId.HotAerialThird, MeleeDpsA);

    [Test]
    public void StandingInDeepBluesLaneKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M10sDeepVarialStateOverrides>(o => Pinned(o, playerFire: false))
            .TeleportAt(Scenario.TakeoffAt + 2f, to: new Vector2(15f, -10f))
            .ShouldKill(ActionId.SickestTakeoffLane, RegenHealer);

    [Test]
    public void LeavingTheBaitSliceKills()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M10sDeepVarialStateOverrides>(o => Pinned(o, playerFire: true))
            .TeleportAt(Scenario.CutbackHitAt - 1f, to: new Vector2(-15f, -15f))
            .ShouldKill(ActionId.CutbackBlazeCleave, PhysRangedDps);

    [Test]
    public void SplittingTheStackKillsTheRest()
        => Negative<Scenario>(MainTank)
            .Overrides<M10sDeepVarialStateOverrides>(o => Pinned(o, playerFire: false))
            .TeleportAt(Scenario.TakeoffOrbsAt - 1.5f, to: new Vector2(-5f, 15f))
            .ShouldKill(ActionId.AwesomeSlab, RegenHealer, MeleeDpsA, PhysRangedDps);

    [Test]
    public void TheWaterTankStandsFurthestFromDeepBlueForDeepImpact()
    {
        foreach (var lane in new[] { -1, 0, 1 })
        foreach (var north in new[] { true, false })
        {
            var checkedIt = false;
            var run = ScenarioRun.Execute(typeof(Scenario), 0, 7, new ScenarioRunOptions
            {
                Overrides = o =>
                {
                    var overrides = (M10sDeepVarialStateOverrides)o;
                    overrides.Lane = lane;
                    overrides.WaveFromNorth = north;
                },
                Probe = p =>
                {
                    if (!p.Crossed(Scenario.DeepImpactHitAt - 0.1f)) return;
                    var waterTank = new[] { MainTank, OffTank }.Select(r => p.Member(r)!).Single(t => t.HasStatus(StatusId.Watersnaking));
                    var furthest = p.World.Party.ActiveMembers().OrderByDescending(m => Vector3.Distance(m.Position, Scenario.DeepBlueLands)).First();
                    Assert.That(furthest, Is.SameAs(waterTank), $"lane {lane}, north {north}");
                    checkedIt = true;
                },
            });
            Assert.That(run.Passed, run.ToString());
            Assert.That(checkedIt, Is.True);
        }
    }
}
