using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Uwu.UltimatePredation;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Uwu.UwuConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Every roll is pinned: Garuda NW (-3,-3), Ultima SW (-12,12), Ifrit NE (13.7,-13.7), Titan E offset
// to (17,-3), bombs in Geometry order, Feather Rain on MT OT M1 R C, both Landslides baited by MT,
// Infernal Fetters on M1. The only safe cardinal is S, so the AI's spots below are its own.
//
//   20.12  Titan's Landslides snapshot, knockback only, landing at 21.15.
//   20.22  Wicked Wheel (7y on Garuda) and Crimson Cyclone (18y wide, NE through SW) snapshot;
//          the party stands at (2.4,18).
//   22.13  Titan's Awaken Landslides snapshot, knocking back at 23.16.
//   22.28  Ceruleum Vent (8y on Ultima), then at 22.48 Wicked Tornado (7-20y donut on Garuda) and the
//          Awaken Cyclones down x=0 and z=0 (10y wide); the party stands at (6,18).
//   25.25  Feather Rain takes its targets' spots, snapshots 27.42.
//   39-48  Eruptions under R (-4,15) and C (4,15), then out along both sides to (±13,-8.5).
//   52.75  Radiant Plumes snapshot, one at (0,18).
//   53-66  Bombs Bury (3y) at (-3,-5) (3,-5) (6,0) (3,5) (-3,5) (-6,0), each Bursting (5y) ~5.5s later.
//   69.34  Suparna and Chirada's Wicked Wheels at (±15,0).
//   75.57  Viscous Aetheroplasm (4y on MT, at (5,-7.5)), then Homing Lasers on MT at 76.34.
public class UltimatePredationScenarioTests
{
    private static readonly PartyRole[] FeatherRainTargets = [MainTank, OffTank, MeleeDpsA, PhysRangedDps, CasterDps];

    // Bury time and spot of each bomb; Burst snapshots at bury + 2.15 + 3.2.
    private static readonly (float Bury, float BurstSnapshot, Vector2 Spot)[] Bombs =
    [
        (53.19f, 58.54f, new(-3, -5)),
        (55.34f, 60.45f, new(3, -5)),
        (57.25f, 62.44f, new(6, 0)),
        (59.24f, 64.49f, new(3, 5)),
        (61.29f, 66.44f, new(-3, 5)),
        (63.24f, 68.59f, new(-6, 0)),
    ];

    private static void Pin(UltimatePredationStateOverrides o)
    {
        o.Garuda = DirectionEnum.NW;
        o.Ultima = DirectionEnum.SW;
        o.Ifrit = DirectionEnum.NE;
        o.Titan = DirectionEnum.E;
        o.TitanOffsetRight = true;
        o.BoulderStart = 0;
        o.FeatherRainTargets = FeatherRainTargets;
        o.UltimaLandslideBait = MainTank;
        o.TitanLandslideBait = MainTank;
        o.InfernalFettersDps = MeleeDpsA;
    }

    private static NegativeRun<UltimatePredationScenario> Predation(PartyRole player)
        => Negative<UltimatePredationScenario>(player).Overrides<UltimatePredationStateOverrides>(Pin);

    private static void ShouldSurvive(PartyRole player, params Takeover[] takeovers)
    {
        var run = ScenarioRun.Execute(typeof(UltimatePredationScenario), 0, Random.Shared.Next(), new ScenarioRunOptions
        {
            PlayerRole = player,
            Overrides = o => Pin((UltimatePredationStateOverrides)o),
            Takeovers = takeovers,
        });
        Assert.That(run.Passed, run.ToString);
    }

    [Test]
    public void DiesWalkingIntoWall()
        => Predation(RegenHealer)
            .TeleportAt(5f, to: new(0, 20.5f))
            .ShouldKill(TheEnvironment, RegenHealer);

    // Clear of the Crimson Cyclone band Garuda itself stands in.
    [Test]
    public void DiesToWickedWheel()
        => Predation(RegenHealer)
            .TeleportAt(20.15f, to: new(-7, -7))
            .ShouldKill(ActionId.WickedWheelAwaken, RegenHealer);

    [Test]
    public void LeavingWickedWheelAfterSnapshotStillDies()
        => Predation(RegenHealer)
            .TeleportAt(20.15f, to: new(-7, -7))
            .TeleportAt(20.3f, to: new(2.4f, 18))
            .ShouldKill(ActionId.WickedWheelAwaken, RegenHealer);

    [Test]
    public void DiesToCrimsonCyclone()
        => Predation(RegenHealer)
            .TeleportAt(20.15f, to: new(8, -8))
            .ShouldKill(ActionId.CrimsonCyclone, RegenHealer);

    // Thrown west along Titan's straight line, 30y from (-12,-3).
    [Test]
    public void LandslideKnocksIntoWall()
        => Predation(RegenHealer)
            .TeleportAt(19.5f, to: new(-12, -3))
            .ShouldKill(TheEnvironment, RegenHealer);

    [Test]
    public void DiesToCeruleumVent()
        => Predation(RegenHealer)
            .TeleportAt(22.2f, to: new(-12, 12))
            .ShouldKill(ActionId.CeruleumVent, RegenHealer);

    // 15.6y from Garuda, outside both Awaken Cyclone lanes.
    [Test]
    public void DiesToWickedTornado()
        => Predation(RegenHealer)
            .TeleportAt(22.3f, to: new(8, 8))
            .ShouldKill(ActionId.WickedTornado, RegenHealer);

    // Inside Wicked Tornado's 7y safe hole, each in one lane.
    [TestCase(-8f, -1f)]
    [TestCase(-1f, -8f)]
    public void DiesToCrimsonCycloneAwaken(float x, float z)
        => Predation(RegenHealer)
            .TeleportAt(22.3f, to: new(x, z))
            .ShouldKill(ActionId.CrimsonCycloneAwaken, RegenHealer);

    // Bots move off their spots before the snapshot; only the one who stays is hit.
    [Test]
    public void DiesToFeatherRain()
        => Predation(PhysRangedDps)
            .FreezeAt(25f)
            .ShouldKill(ActionId.FeatherRain, PhysRangedDps);

    // R is one of the two farthest from Ifrit when the first Eruptions drop.
    [Test]
    public void DiesToEruption()
        => Predation(PhysRangedDps)
            .FreezeAt(39.5f)
            .ShouldKill(ActionId.EruptionPuddle, PhysRangedDps);

    [Test]
    public void DiesToRadiantPlume()
        => Predation(RegenHealer)
            .TeleportAt(52.5f, to: new(0, 18))
            .ShouldKill(ActionId.RadiantPlumePuddle, RegenHealer);

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(5)]
    public void DiesToBury(int bomb)
        => Predation(RegenHealer)
            .TeleportAt(Bombs[bomb].Bury - 0.2f, to: Bombs[bomb].Spot)
            .ShouldKill(ActionId.Bury, RegenHealer);

    [TestCase(0)]
    [TestCase(4)]
    [TestCase(5)]
    public void DiesToBurst(int bomb)
        => Predation(RegenHealer)
            .TeleportAt(Bombs[bomb].BurstSnapshot - 0.2f, to: Bombs[bomb].Spot)
            .ShouldKill(ActionId.Burst, RegenHealer);

    [TestCase(15f)]
    [TestCase(-15f)]
    public void DiesToSisterWickedWheel(float x)
        => Predation(RegenHealer)
            .TeleportAt(69.1f, to: new(x, 0))
            .ShouldKill(ActionId.WickedWheel, RegenHealer);

    [Test]
    public void NonTankInViscousAetheroplasmDies()
        => Predation(RegenHealer)
            .TeleportAt(75.4f, to: new(5, -7.5f))
            .ShouldKill(ActionId.ViscousAetheroplasmEffect, RegenHealer);

    // Everyone takes Ultima's initial hit with MT at (5,-12), so each gets an explosion at 75.57; stacked
    // on MT's (5,-7.5) all eight survive the first full stack and its vuln kills them on the next.
    [Test]
    public void PartySharingInitialViscousAetheroplasmDiesToSecondExplosion()
        => AllBut(RegenHealer)
            .Aggregate(
                Predation(RegenHealer)
                    .TeleportAt(64.2f, to: new(5, -12))
                    .TeleportAt(73.6f, to: new(-5, -7.5f))
                    .TeleportAt(75.3f, to: new(5, -7.5f)),
                (run, bot) => run.MoveBotAt(64.2f, bot, new(5, -12)).MoveBotAt(75.3f, bot, new(5, -7.5f)))
            .ShouldKill(ActionId.ViscousAetheroplasmEffect, PerRole.All);

    // Back out before MT's Feather Rain lands on the spot.
    [Test]
    public void NonTankInHomingLasersDies()
        => Predation(RegenHealer)
            .TeleportAt(76.2f, to: new(5, -7.5f))
            .TeleportAt(76.5f, to: new(-5, -14))
            .ShouldKill(ActionId.HomingLasers, RegenHealer);

    // OT holds MT's spot through both snapshots; its AI takes it back to the stack at 77.
    [Test]
    public void TanksSurviveEachOthersBusters()
        => ShouldSurvive(RegenHealer, new Takeover(75.4f, new Vector2(5, -7.5f), Bot: OffTank));

    [Test]
    public void ReplayStateRoundTrips()
    {
        UltimatePredationState? host = null;
        UltimatePredationState? replayed = null;
        ScenarioRun.Execute(typeof(UltimatePredationScenario), 0, Random.Shared.Next(), new ScenarioRunOptions
        {
            StopAt = 1f,
            Probe = probe =>
            {
                if (!probe.Crossed(0.5f)) return;
                var scenario = probe.Game.Scenarios.OfType<UltimatePredationScenario>().Single();
                host = scenario.LastState;
                replayed = (UltimatePredationState?)scenario.StartReplay(scenario.BuildReplayStateMessage()!, 0, MainTank, probe.World);
            },
        });

        Assert.That(host, Is.Not.Null);
        Assert.That(replayed, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(replayed!.GarudaPlacement, Is.EqualTo(host!.GarudaPlacement));
            Assert.That(replayed.TitanPlacement, Is.EqualTo(host.TitanPlacement));
            Assert.That(replayed.IfritPlacement, Is.EqualTo(host.IfritPlacement));
            Assert.That(replayed.UltimaPlacement, Is.EqualTo(host.UltimaPlacement));
            Assert.That(replayed.ResolvedSafeCardinal, Is.EqualTo(host.ResolvedSafeCardinal));
            Assert.That(replayed.ResolvedSafeFirstSet, Is.EqualTo(host.ResolvedSafeFirstSet));
            Assert.That(replayed.ResolvedSafeSecondSet, Is.EqualTo(host.ResolvedSafeSecondSet));
        });
    }
}
