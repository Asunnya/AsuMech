using System.Linq;
using System.Numerics;
using AnoMech.Scenarios.M10s.Opener;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.Opener.M10sOpenerScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M10sOpenerScenarioTests
{
    // Red Hot leaps to the west wall; M1, M2, R1 and R2 are tethered in that order.
    private static void Pinned(M10sOpenerStateOverrides o)
    {
        o.Wall = 3;
        o.FloaterOrder = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
        o.PyrotationTarget = RegenHealer;
    }

    [Test]
    public void TakingHotImpactAloneKillsTheTank()
        => Negative<Scenario>(OffTank)
            .Overrides<M10sOpenerStateOverrides>(Pinned)
            .TeleportAt(12f, to: new Vector2(0f, 15f))
            .ShouldKill(ActionId.HotImpact, MainTank);

    [Test]
    public void AFloaterTooCloseToRedHotDiesToTheCharge()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M10sOpenerStateOverrides>(Pinned)
            .TeleportAt(29.5f, to: new Vector2(-15f, 8f))
            .ShouldKill(ActionId.FlameFloaterFirst, MeleeDpsA);

    [Test]
    public void StandingInTheChargeNearRedHotKills()
        => Negative<Scenario>(MainTank)
            .Overrides<M10sOpenerStateOverrides>(Pinned)
            .TeleportAt(29.5f, to: new Vector2(-19.5f, 8f))
            .ShouldKill(ActionId.FlameFloaterFirst, MainTank);

    [Test]
    public void StretchedFloatersSurviveTheirCharges()
        => Negative<Scenario>(CasterDps)
            .Overrides<M10sOpenerStateOverrides>(Pinned)
            .At(41f, p => Assert.That(new[] { MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps }.Select(r => p.Member(r)!.HasStatus(StatusId.FireResistanceDownII)), Is.All.True))
            .TeleportAt(52.5f, to: new Vector2(-11f, 4.5f))
            .ShouldKill(ActionId.AlleyOopInfernoSpread, CasterDps, MeleeDpsB);

    [Test]
    public void StandingInAChargesFireKills()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M10sOpenerStateOverrides>(Pinned)
            .TeleportAt(33f, to: new Vector2(-19.5f, 8f))
            .ShouldKill(ActionId.FlameFloater, RegenHealer);

    [Test]
    public void OverlappingSpreadsKillBoth()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M10sOpenerStateOverrides>(Pinned)
            .TeleportAt(52.5f, to: new Vector2(-12.5f, -7f))
            .ShouldKill(ActionId.AlleyOopInfernoSpread, RegenHealer, ShieldHealer);

    [Test]
    public void StayingInFrontOfRedHotDiesToCutbackBlaze()
        => Negative<Scenario>(CasterDps)
            .Overrides<M10sOpenerStateOverrides>(Pinned)
            .FreezeAt(57f)
            .ShouldKill(ActionId.CutbackBlazeCleave, CasterDps);

    [Test]
    public void PyrotationTakenAloneKillsTheHealer()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M10sOpenerStateOverrides>(Pinned)
            .TeleportAt(69.35f, to: new Vector2(-2f, -12f))
            .ShouldKill(ActionId.PyrotationStack, RegenHealer);
}
