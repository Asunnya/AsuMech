using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Uwu.UltimateSuppression;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Uwu.UwuConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Every roll is pinned: Light Pillar on H2, Mistral Songs on M1 and M2, Eruptions on R and C, Gaol on
// H1, Flaming Crush on M1, Thermal Low on MT and H2, spread spots in order, lasers centre / right /
// left, Feather Rain on MT OT M1 R C, Garuda and Titan's Landslide both on MT.
//
//   17.20  First Eruptions snapshot under R and C; Razor Plumes spawn on the diagonals at 17.95,
//          2y contact, until 42.97, sweeping round at radius 17 from 20.82.
//   20.81  Mistral Songs hit the nearest in Chirada's (6,6) and Suparna's (-6,-6) cones; the tanks
//          stand in front at (-6,8).
//   21.32  H1 is gaoled at (8,8) until 30.24; the last Eruptions snapshot on it at 23.25.
//   24.81  Feather Rain snapshots on its targets' 22.57 spots, again at 26.80 and 43.17.
//   25.40  First Light Pillar, under H2.
//   27.40  Aetherochemical Lasers from Ultima (13.7,-13.7), 8y wide: centre, then right at 31.41
//          (along z=-13.7), then left at 35.54 (along x=13.7).
//   34.74  Titan's Landslides from (-13.7,13.7) snapshot, knocking 30y back at 35.77.
//   37.92  Mesohigh resolves on whoever holds Garuda's (-13.7,-13.7) tether; MT intercepts it from
//          (-4,-7).
//   38.78  Flaming Crush on M1 needs the 7 others but MT; the stack is at (6.7,0).
//   41.20  Featherlance, 8y round each plume, now pulled back to the diagonals at radius 20.6.
public class UltimateSuppressionScenarioTests
{
    private static readonly PartyRole[] FeatherRainTargets = [MainTank, OffTank, MeleeDpsA, PhysRangedDps, CasterDps];

    private static void Pin(UltimateSuppressionStateOverrides o)
    {
        o.Assignments = [ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps, RegenHealer];
        o.FlamingCrush = MeleeDpsA;
        o.ThermalLowHealer = ShieldHealer;
        o.SuppressionSpotOrder = [0, 1, 2, 3, 4, 5];
        o.Lasers = [ActionId.AetherochemicalLaserCenter, ActionId.AetherochemicalLaserRight, ActionId.AetherochemicalLaserLeft];
        o.FeatherRainTargets = FeatherRainTargets;
        o.GarudaFacing = MainTank;
        o.LandslideBait = MainTank;
    }

    private static NegativeRun<UltimateSuppressionScenario> Suppression(PartyRole player)
        => Negative<UltimateSuppressionScenario>(player).Overrides<UltimateSuppressionStateOverrides>(Pin);

    private static void SurvivesUntil(float stopAt, PartyRole player, params Takeover[] takeovers)
        => SurvivesUntil(stopAt, player, Pin, takeovers);

    private static void SurvivesUntil(float stopAt, PartyRole player, Action<UltimateSuppressionStateOverrides> pin, params Takeover[] takeovers)
    {
        var run = ScenarioRun.Execute(typeof(UltimateSuppressionScenario), 0, Random.Shared.Next(), new ScenarioRunOptions
        {
            PlayerRole = player,
            StopAt = stopAt,
            Overrides = o => pin((UltimateSuppressionStateOverrides)o),
            Takeovers = takeovers,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void DiesWalkingIntoWall()
        => Suppression(MeleeDpsB)
            .TeleportAt(5f, to: new(0, 20.5f))
            .ShouldKill(ArenaWall, MeleeDpsB);

    // R's own first Eruption, with C's beside it.
    [Test]
    public void DiesToEruption()
        => Suppression(PhysRangedDps)
            .FreezeAt(14.4f)
            .ShouldKill(ActionId.EruptionPuddle, PhysRangedDps);

    // The last Eruptions are baited onto the gaol at (8,8).
    [Test]
    public void UngaoledPlayerInGaolsEruptionsDies()
        => Suppression(ShieldHealer)
            .TeleportAt(20.4f, to: new(8, 7))
            .ShouldKill(ActionId.EruptionPuddle, ShieldHealer);

    [Test]
    public void GaoledPlayerSurvivesEruption()
        => SurvivesUntil(24.5f, RegenHealer);

    [Test]
    public void GaoledPlayerSurvivesFeatherRain()
        => SurvivesUntil(28f, RegenHealer, o =>
        {
            Pin(o);
            o.FeatherRainTargets = [RegenHealer, MainTank, OffTank, PhysRangedDps, CasterDps];
        });

    // Bots move off their spots before each snapshot; only the one who stays is hit.
    [TestCase(22.5f)]
    [TestCase(24.6f)]
    [TestCase(41.1f)]
    public void DiesToFeatherRain(float freezeAt)
        => Suppression(MeleeDpsA)
            .FreezeAt(freezeAt)
            .ShouldKill(ActionId.FeatherRain, MeleeDpsA);

    // Thrown along Titan's middle line, 30y from 10y out.
    [Test]
    public void LandslideKnocksIntoWall()
        => Suppression(MeleeDpsB)
            .TeleportAt(34.5f, to: new(-5.4f, 8.1f))
            .ShouldKill(ArenaWall, MeleeDpsB);

    // Thrown out between two sweeping plumes.
    [Test]
    public void LeavingLandslideAfterSnapshotStillKnockedBack()
        => Suppression(MeleeDpsB)
            .TeleportAt(34.5f, to: new(-5.4f, 8.1f))
            .TeleportAt(34.9f, to: new(-12.6f, 5.8f))
            .ShouldKill(ArenaWall, MeleeDpsB);

    [Test]
    public void DiesToLightPillar()
        => Suppression(ShieldHealer)
            .FreezeAt(24.6f)
            .ShouldKill(ActionId.LightPillarCircle, ShieldHealer);

    [TestCase(ActionId.AetherochemicalLaserCenter, 27.2f, 0f, 0f)]
    [TestCase(ActionId.AetherochemicalLaserRight, 31.2f, 0f, -13.7f)]
    [TestCase(ActionId.AetherochemicalLaserLeft, 35.3f, 13.7f, -6f)]
    public void DiesToAetherochemicalLaser(uint laser, float at, float x, float z)
        => Suppression(MeleeDpsB)
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(laser, MeleeDpsB);

    // Nearer Chirada than the tanks covering M1.
    [Test]
    public void NonTankFirstInMistralSongDies()
        => Suppression(PhysRangedDps)
            .TeleportAt(20.3f, to: new(3, 6.8f))
            .ShouldKill(ActionId.MistralSongSuparnaChirada, PhysRangedDps);

    [TestCase(MainTank)]
    [TestCase(RegenHealer)]
    public void TankOrGaolFirstInMistralSongSurvives(PartyRole player)
        => SurvivesUntil(22f, player, new Takeover(20.3f, new Vector2(3, 6.8f)));

    [Test]
    public void DiesToRazorPlume()
        => Suppression(MeleeDpsB)
            .TeleportAt(19f, to: new(12, 11))
            .ShouldKill(ActionId.Featherlance, MeleeDpsB);

    // On the plumes' circle, which a plume reaches within a quarter turn.
    [Test]
    public void DiesToSweepingRazorPlume()
        => Suppression(MeleeDpsB)
            .TeleportAt(21f, to: new(0, 17))
            .ShouldKill(ActionId.Featherlance, MeleeDpsB);

    [Test]
    public void DiesToFeatherlance()
        => Suppression(MeleeDpsB)
            .TeleportAt(41f, to: new(10, 10))
            .ShouldKill(ActionId.Featherlance, MeleeDpsB);

    [Test]
    public void FlamingCrushUnderstackedKillsStack()
        => Suppression(MeleeDpsB)
            .TeleportAt(38.6f, to: new(0, 10))
            .ShouldKill(ActionId.FlamingCrush, OffTank, RegenHealer, ShieldHealer, MeleeDpsA, PhysRangedDps, CasterDps);

    // Halfway down the beam from MT to Garuda.
    [Test]
    public void MesohighWithoutThermalLowDies()
        => Suppression(MeleeDpsB)
            .TeleportAt(37.5f, to: new(-9, -10.4f))
            .ShouldKill(ActionId.Mesohigh, MeleeDpsB);

    // Back in the stack in time for Flaming Crush.
    [Test]
    public void MesohighWithThermalLowSurvives()
        => SurvivesUntil(40f, ShieldHealer,
            new Takeover(37.5f, new Vector2(-9, -10.4f)),
            new Takeover(38f, new Vector2(6.7f, 0)));
}
