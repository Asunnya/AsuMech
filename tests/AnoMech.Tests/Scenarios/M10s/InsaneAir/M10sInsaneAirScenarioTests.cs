using System.Globalization;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;
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

    [Test]
    public void XtremeSpectacularDressesTheArenaAndClearsItBeforeInsaneAir()
    {
        SimTimeline? timeline = null;
        var run = ScenarioRun.Execute(typeof(Scenario), 0, 1, new ScenarioRunOptions
        {
            Probe = _ =>
            {
                if (timeline != null) return;
                timeline = ((FakeBattleCharas)Natives.BattleCharas).Timeline;
                ((FakeMapEffects)Natives.MapEffects).Loaded = true;
            },
        });
        Assert.That(run.Passed, run.ToString());

        var arena = timeline!.Rows
            .Where(r => r["kind"] == "map_effect" && r["id"] == MapEffect.ArenaSlot.ToString())
            .Select(r => (T: float.Parse(r["t"], CultureInfo.InvariantCulture), Detail: r["detail"]))
            .ToList();
        Assert.That(arena.Select(a => a.Detail), Is.EqualTo(new[]
        {
            $"state={MapEffect.SpectacularOn & 0xFFFF} timeline={MapEffect.SpectacularOn >> 16} flags=0x{MapEffect.SpectacularOn:X}",
            $"state={MapEffect.SpectacularOff & 0xFFFF} timeline={MapEffect.SpectacularOff >> 16} flags=0x{MapEffect.SpectacularOff:X}",
        }));
        Assert.That(arena[0].T, Is.InRange(Scenario.XtremeCastAt, Scenario.XtremeHitsAt));
        Assert.That(arena[1].T, Is.InRange(Scenario.XtremeHitsAt, Scenario.InsaneAirAt));
    }

    [Test]
    public void XtremeSpectacularCoversTheWholeArena()
    {
        AoeQuery? xtreme = null;
        void Capture(AoeQuery q)
        {
            if (q.ActionId == ActionId.XtremeSpectacular) xtreme ??= q;
        }
        AoeQuery.Evaluated += Capture;
        try
        {
            var run = ScenarioRun.Execute(typeof(Scenario), 0, 1);
            Assert.That(run.Passed, run.ToString());
            Assert.That(xtreme, Is.Not.Null);
            foreach (var corner in new[] { new Vector3(-19.5f, 0f, -19.5f), new Vector3(19.5f, 0f, -19.5f), new Vector3(-19.5f, 0f, 19.5f), new Vector3(19.5f, 0f, 19.5f) })
                Assert.That(xtreme!.Value.SignedDistance(corner), Is.LessThan(0f), $"corner {corner}");
            Assert.That(xtreme!.Value.SignedDistance(new Vector3(-30f, 0f, 0f)), Is.GreaterThan(0f), "west of the arena");
        }
        finally
        {
            AoeQuery.Evaluated -= Capture;
        }
    }
}
