using System.Numerics;
using AnoMech.Scenarios.M10s.SickestTakeoff;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.SickestTakeoff.M10sSickestTakeoffScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M10sSickestTakeoffScenarioTests
{
    // The wave rises at the south wall and carries everyone north; Deep Blue surfs the west lane.
    // Light party one stacks in the east lane (x 13), two in the middle (x 0).
    private static void Pinned(M10sSickestTakeoffStateOverrides o)
    {
        o.WaveFrom = 2;
        o.Lane = -1;
        o.EightOrbs = false;
        o.DoubleDip = false;
    }

    [Test]
    public void StandingInDeepBluesLaneKills()
        => Negative<Scenario>(CasterDps)
            .Overrides<M10sSickestTakeoffStateOverrides>(Pinned)
            .TeleportAt(30.5f, to: new Vector2(-15f, 5f))
            .ShouldKill(ActionId.SickestTakeoffLane, CasterDps);

    [Test]
    public void TheWaveCarriesThePartyAndALoneHealerDiesToTheStack()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M10sSickestTakeoffStateOverrides>(Pinned)
            .At(32.6f, p =>
            {
                Assert.That(p.Member(MainTank)!.Position.Z, Is.InRange(-4.5f, -1.5f));
                Assert.That(p.Member(MainTank)!.Position.X, Is.InRange(11.5f, 14.5f));
            })
            .TeleportAt(33f, to: new Vector2(6.5f, -12f))
            .ShouldKill(ActionId.AwesomeSlab, RegenHealer);

    [Test]
    public void OverlappingSpreadsKillBoth()
        => Negative<Scenario>(CasterDps)
            .Overrides<M10sSickestTakeoffStateOverrides>(o => { Pinned(o); o.EightOrbs = true; })
            .TeleportAt(33.2f, to: new Vector2(16f, 0f))
            .ShouldKill(ActionId.AwesomeSplash, CasterDps, PhysRangedDps);

    [Test]
    public void SharingAProteanKillsBoth()
        => Negative<Scenario>(CasterDps)
            .Overrides<M10sSickestTakeoffStateOverrides>(Pinned)
            .TeleportAt(45.3f, to: new Vector2(2.83f, 2.83f))
            .ShouldKill(ActionId.ReverseAlleyOopFirst, CasterDps, MeleeDpsB);

    [Test]
    public void StayingForDoubleDipLeavesDamageDownAndAFarNonTankDiesToDeepImpact()
        => Negative<Scenario>(CasterDps)
            .Overrides<M10sSickestTakeoffStateOverrides>(o => { Pinned(o); o.DoubleDip = true; })
            .FreezeAt(45f)
            .At(49.2f, p => Assert.That(p.Member(CasterDps)!.HasStatus(StatusId.DamageDown), Is.True))
            .At(49.2f, p => Assert.That(p.Member(MeleeDpsB)!.HasStatus(StatusId.DamageDown), Is.False))
            .TeleportAt(50f, to: new Vector2(0f, 15f))
            .ShouldKill(ActionId.DeepImpactHit, CasterDps);
}
