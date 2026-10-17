using System.Numerics;
using AnoMech.Scenarios.M10s.InsaneAir;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M10s.InsaneAir.M10sInsaneAirScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M10sInsaneAirScenarioTests
{
    // Variant B. Red Hot: Vertical at (0,13), Snap, Re-entry, Snap. Deep Blue: Snap at (0,-13),
    // Re-entry at (13,-13), Snap, Vertical. Deep Blue's light party is MT, H1, M1, R1.
    private static void Pinned(M10sInsaneAirStateOverrides o) => o.Variant = 2;

    [Test]
    public void ANonTankNearestTheBoardTakesTheTankBuster()
        => Negative<Scenario>(ShieldHealer)
            .Overrides<M10sInsaneAirStateOverrides>(Pinned)
            .TeleportAt(Scenario.StopAt[0] + 0.5f, to: new Vector2(0f, 14.5f))
            .ShouldKill(ActionId.VerticalBlastHit, ShieldHealer);

    [Test]
    public void SharingASnapConeKillsBoth()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M10sInsaneAirStateOverrides>(Pinned)
            .TeleportAt(Scenario.StopAt[0] + 0.6f, to: new Vector2(-4.16f, -14.72f))
            .ShouldKill(ActionId.PlungingSnapCone, MainTank, MeleeDpsA);

    [Test]
    public void LeavingTheReentryStackKillsTheRest()
        => Negative<Scenario>(PhysRangedDps)
            .Overrides<M10sInsaneAirStateOverrides>(Pinned)
            .TeleportAt(Scenario.StopAt[1] + 0.5f, to: new Vector2(13f, -5f))
            .ShouldKill(ActionId.ReentryPlungeCone, MainTank, RegenHealer, MeleeDpsA);
}
