using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P5Exaflares;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Tests.NegativeRun;
using ActionId = AnoMech.Scenarios.Umad.UmadConstants.ActionId;

namespace AnoMech.Tests;

// Pinned: both walls fire lines 1/4, then 2/5, then 3/6. Tiles are 5y apart along each line, so
// 7.07y between neighbours (radius 6): a tile centre is hit by that tile alone. Each tile snapshots
// 4.582s + 0.513s * tile after its wave starts and kills 0.624s later.
//
//   7.58   Wave 0 (left, 1/4). Line 4 tiles: (-10,-15) (-5,-10) (0,-5) (5,0) (10,5) (15,10).
//  10.08   Wave 1 (right, 1/4). Line 4 tiles: (15,-10) (10,-5) (5,0) (0,5) (-5,10) (-10,15).
//  12.58   Wave 2 (left, 2/5). Line 5 tiles: (-5,-20) (0,-15) (5,-10) (10,-5) (15,0) (20,5).
//  15.08   Wave 3 (right, 2/5). Line 2 tiles: (5,-20) (0,-15) (-5,-10) (-10,-5) (-15,0) (-20,5).
//  17.58   Wave 4 (left, 3/6). Line 3 tiles: (-15,-10) (-10,-5) (-5,0) (0,5) (5,10) (10,15).
//          Its safe central lane is 5 < X-Z < 15.
//  20.08   Wave 5 (right, 3/6). Line 3 tiles: (10,-15) (5,-10) (0,-5) (-5,0) (-10,5) (-15,10).
//  25.09   Spread snapshot (r5 each, two coverings lethal); applied at 25.71.
//
// The bots' spread spots vary per seed, so spread tests park the other six bots across the arena
// from the pair. A run pauses 5s after its first death.
public class UmadP5ExaflaresScenarioTests
{
    private static readonly Vector2 PairSpot = new(0f, 19.5f);
    private static readonly (PartyRole Bot, Vector2 Spot)[] FarSpots =
    [
        (OffTank, new(-12f, -12f)),
        (RegenHealer, new(12f, -12f)),
        (ShieldHealer, new(0f, -15f)),
        (MeleeDpsA, new(-15f, 0f)),
        (MeleeDpsB, new(15f, 0f)),
        (PhysRangedDps, new(0f, -5f)),
    ];

    private static NegativeRun<UmadP5ExaflaresScenario> Exaflares(PartyRole player)
        => Negative<UmadP5ExaflaresScenario>(player)
            .Overrides<UmadP5ExaflaresStateOverrides>(o =>
            {
                o.LeftOrder = ExaFlareOrder.Line14_25_36;
                o.RightOrder = ExaFlareOrder.Line14_25_36;
            });

    private static NegativeRun<UmadP5ExaflaresScenario> SpreadPairedWithCaster()
    {
        var run = Exaflares(MainTank)
            .MoveBotAt(25.05f, CasterDps, to: PairSpot)
            .TeleportAt(25.05f, to: PairSpot);
        foreach (var (bot, spot) in FarSpots)
            run = run.MoveBotAt(25.05f, bot, spot);
        return run;
    }

    // Each wave's inner line, third tile.
    [TestCase(8.3f, 0f, -5f)]
    [TestCase(10.8f, 5f, 0f)]
    [TestCase(13.3f, 5f, -10f)]
    [TestCase(15.8f, -5f, -10f)]
    [TestCase(18.3f, -5f, 0f)]
    [TestCase(20.8f, 0f, -5f)]
    public void StandingInWaveLaneDies(float at, float x, float z)
        => Exaflares(MainTank)
            .TeleportAt(at, to: new(x, z))
            .ShouldKill(ActionId.ExaflareHit, MainTank);

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void EachRollingTileKills(int tile)
        => Exaflares(MainTank)
            .TeleportAt(17.28f + 0.513f * tile, to: new(-15f + 5f * tile, -10f + 5f * tile))
            .ShouldKill(ActionId.ExaflareHit, MainTank);

    // 5.5y across the lane from wave 4's (-5,0); 9y from its neighbour tiles.
    [Test]
    public void EdgeOfFireRadiusDies()
        => Exaflares(MainTank)
            .TeleportAt(18.3f, to: new(-1.11f, -3.89f))
            .ShouldKill(ActionId.ExaflareHit, MainTank);

    // (-5,0) snapshots at 18.61 and kills at 19.23; (5,-5) is in the safe lane.
    [Test]
    public void LeavingAfterExaflareSnapshotStillDies()
        => Exaflares(MainTank)
            .TeleportAt(18.5f, to: new(-5f, 0f))
            .TeleportAt(18.8f, to: new(5f, -5f))
            .ShouldKill(ActionId.ExaflareHit, MainTank);

    [Test]
    public void SpreadOverlapKillsBoth()
        => SpreadPairedWithCaster()
            .ShouldKill(ActionId.ExaflareSpread, MainTank, CasterDps);

    [Test]
    public void SeparatingAfterSpreadSnapshotStillKillsBoth()
        => SpreadPairedWithCaster()
            .TeleportAt(25.3f, to: new(-19f, 0f))
            .ShouldKill(ActionId.ExaflareSpread, MainTank, CasterDps);

    [Test]
    public void DiesWalkingOffArena()
        => Exaflares(MainTank)
            .TeleportAt(3f, to: new(0, 20.5f))
            .ShouldKill(TheEnvironment, MainTank);
}
