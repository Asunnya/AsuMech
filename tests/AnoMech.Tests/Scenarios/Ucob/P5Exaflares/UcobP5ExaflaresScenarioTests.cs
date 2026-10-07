using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Ucob.P5Exaflares;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Ucob.UcobConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Every roll is pinned: lanes fire as pairs (4, -12), (-20, 12), (-4, 20). With the lanes rolling
// N, lane offset is x and hit k lands at z = 20 - 8k, 6y wide; adjacent spots are 8y apart.
//
//    8.98 first pair erupts at z=20, then every 1.47s: 10.45 z=12, 11.92 z=4, 13.39 z=-4, ...
//   11.98 second pair, 14.98 third pair, same cadence.
//   Each eruption snapshots who is in it and kills them 0.6s later.
public class UcobP5ExaflaresScenarioTests
{
    private static readonly float[] LaneOrder = [4f, -12f, -20f, 12f, -4f, 20f];

    private static void Pin(UcobP5ExaflaresStateOverrides o, Direction direction)
    {
        o.Direction = direction;
        o.LaneOrder = LaneOrder;
    }

    private static NegativeRun<UcobP5ExaflaresScenario> Exaflares(PartyRole player, Direction? direction = null)
        => Negative<UcobP5ExaflaresScenario>(player)
            .Overrides<UcobP5ExaflaresStateOverrides>(o => Pin(o, direction ?? Direction.N));

    private static void ShouldSurvive(PartyRole player, params Takeover[] takeovers)
    {
        var run = ScenarioRun.Execute(typeof(UcobP5ExaflaresScenario), 0, Random.Shared.Next(), new ScenarioRunOptions
        {
            PlayerRole = player,
            Overrides = o => Pin((UcobP5ExaflaresStateOverrides)o, Direction.N),
            Takeovers = takeovers,
        });
        Assert.That(run.Passed, run.ToString);
    }

    // `along` runs with the lanes, from -20 where they start; `offset` is the lane's.
    private static Vector2 OnLane(Direction direction, float offset, float along)
    {
        var theta = direction.RadiansFromNorth;
        var travel = new Vector2(MathF.Sin(theta), -MathF.Cos(theta));
        var lateral = new Vector2(MathF.Cos(theta), MathF.Sin(theta));
        return lateral * offset + travel * along;
    }

    [Test]
    public void DiesWalkingIntoWall()
        => Exaflares(RegenHealer)
            .TeleportAt(5f, to: new(0, 22))
            .ShouldKill(TheEnvironment, RegenHealer);

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    public void DiesToFirstEruption(int directionIndex)
    {
        var direction = Direction.All[directionIndex];
        Exaflares(RegenHealer, direction)
            .TeleportAt(5f, to: OnLane(direction, 4, -19))
            .ShouldKill(ActionId.ExaflareFirst, RegenHealer);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    public void DiesToRollingEruption(int directionIndex)
    {
        var direction = Direction.All[directionIndex];
        Exaflares(RegenHealer, direction)
            .TeleportAt(5f, to: OnLane(direction, 4, 4))
            .ShouldKill(ActionId.ExaflareRest, RegenHealer);
    }

    // Caught by the 10.45 snapshot, then out onto lane 4's burned first spot before the kill lands.
    [Test]
    public void LeavingAfterTheSnapshotStillDies()
        => Exaflares(MeleeDpsA)
            .TeleportAt(10.3f, to: new(4, 12))
            .TeleportAt(10.5f, to: new(4, 20))
            .ShouldKill(ActionId.ExaflareRest, MeleeDpsA);

    // Into lane 4's z=12 spot just after its snapshot; nothing else reaches it afterwards.
    [Test]
    public void LingeringFlameIsHarmless()
        => ShouldSurvive(MeleeDpsA, new Takeover(10.6f, new Vector2(4, 12)));

    [Test]
    public void StandingOnABurnedLaneIsSafe()
        => ShouldSurvive(MeleeDpsA, new Takeover(9.2f, new Vector2(4, 20)));
}
