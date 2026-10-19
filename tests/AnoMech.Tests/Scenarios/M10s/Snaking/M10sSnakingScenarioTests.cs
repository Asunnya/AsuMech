using System.Numerics;
using AnoMech.Scenarios.M10s.Snaking;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.Snaking.M10sSnakingScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M10sSnakingScenarioTests
{
    // Fire takes the nearest, so light party two (OT, H2, M2, R2) is fire; Deep Blue's corner is south.
    private static void Pinned(M10sSnakingStateOverrides o)
    {
        o.FireOnTheNearest = true;
        o.DeepBlueSouth = true;
        o.DoubleDip = false;
        o.WaveFromNorth = true;
    }

    [Test]
    public void FireGoesToTheFourNearestRedHot()
        => Negative<Scenario>(MainTank)
            .Overrides<M10sSnakingStateOverrides>(Pinned)
            .TeleportAt(Scenario.SnakingCastAt + 1f, to: new Vector2(7f, 1f))
            .At(Scenario.SnakingHitAt + 0.5f, p =>
            {
                Assert.That(p.Member(MainTank)!.HasStatus(StatusId.Firesnaking), Is.True);
                Assert.That(new[] { OffTank, ShieldHealer, MeleeDpsB, CasterDps }.Count(r => p.Member(r)!.HasStatus(StatusId.Firesnaking)), Is.EqualTo(3));
                Assert.That(new[] { RegenHealer, MeleeDpsA, PhysRangedDps }.Select(r => p.Member(r)!.HasStatus(StatusId.Watersnaking)), Is.All.True);
            })
            .ShouldKill(ActionId.HotImpactOnFire, MainTank);

    [Test]
    public void OverlappingSpreadsKillBoth()
        => Negative<Scenario>(MeleeDpsB)
            .Overrides<M10sSnakingStateOverrides>(Pinned)
            .TeleportAt(Scenario.InfernoHitAt - 0.5f, to: new Vector2(12.5f, 4f))
            .ShouldKill(ActionId.AlleyOopInfernoSpread, MeleeDpsB, OffTank);

    [Test]
    public void SharingAProteanKillsBoth()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M10sSnakingStateOverrides>(Pinned)
            .TeleportAt(Scenario.FirstConesAt - 0.5f, to: new Vector2(-19.32f, 5.55f))
            .ShouldKill(ActionId.ReverseAlleyOopFirst, RegenHealer, MainTank);

    [Test]
    public void TakingHotImpactAloneKillsTheFireTank()
        => Negative<Scenario>(MainTank)
            .Overrides<M10sSnakingStateOverrides>(Pinned)
            .TeleportAt(30f, to: new Vector2(-10f, -10f))
            .ShouldKill(ActionId.HotImpactOnFire, OffTank);

    [Test]
    public void StayingForTheDoubleDipLeavesDamageDown()
        => Negative<Scenario>(MainTank)
            .Overrides<M10sSnakingStateOverrides>(o => { Pinned(o); o.DoubleDip = true; })
            .FreezeAt(Scenario.FirstConesAt)
            .At(Scenario.SecondConesAt + 1f, p => Assert.That(p.Member(MainTank)!.HasStatus(StatusId.DamageDown), Is.True))
            .ShouldKill(ActionId.HotImpactOnFire, OffTank);
}
