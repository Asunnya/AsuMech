using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Motion;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.M11s;

namespace AnoMech.Tests;

public class NavPlannerTests
{
    private static (SimWorld World, SimEnemy Caster) World()
    {
        FakeGame.Install();
        var world = new SimWorld(new EventScheduler());
        world.EnforceArenaBoundary(new SquareArena(20f));
        world.Obstacles.PlanRoutes = true;
        world.Tick(0.016f);
        var caster = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: M11sConstants.BNpcBaseId.Helper, Level: 1, Placement: new Placement(Vector3.Zero, 0f)))!;
        return (world, caster);
    }

    // Impact is a 6 y circle in the Action sheet.
    private static HazardArea CircleAt(SimEnemy caster, Vector3 at, float resolveAt)
        => new(new EnemyAction(M11sConstants.ActionId.Impact) { Effects = [EnemyActionEffects.Damage()] }, caster, null, at, resolveAt);

    [Test]
    public void AnObstacleOnTheWayBendsThePathAndAClearWayDoesNot()
    {
        var (world, _) = World();
        var planner = new NavPlanner(world);
        Assert.That(planner.Plan(new Vector2(-10f, 0f), new Vector2(10f, 0f), 6f), Is.Null, "straight line, nothing to plan");
        world.Obstacles.Add(new CircleObstacle(Vector2.Zero, 5f));
        world.Obstacles.PlanRoutes = false;
        Assert.That(planner.Plan(new Vector2(-10f, 0f), new Vector2(10f, 0f), 6f), Is.Null, "a scenario that didn't opt in keeps the reactive steering");
        world.Obstacles.PlanRoutes = true;
        var path = planner.Plan(new Vector2(-10f, 0f), new Vector2(10f, 0f), 6f);
        Assert.That(path, Is.Not.Null.And.Count.GreaterThanOrEqualTo(2));
        Assert.That(path![^1], Is.EqualTo(new Vector2(10f, 0f)));
    }

    [Test]
    public void AHazardResolvingBeforeTheBotPassesIsAvoidedOneResolvingAfterIsNot()
    {
        var (world, caster) = World();
        var planner = new NavPlanner(world);
        var soon = CircleAt(caster, Vector3.Zero, resolveAt: 1f);
        world.AddHazard(soon);
        Assert.That(planner.Plan(new Vector2(-10f, 0f), new Vector2(10f, 0f), 6f), Is.Not.Null, "it goes off while the bot is in it");
        world.RemoveHazard(soon);
        var late = CircleAt(caster, Vector3.Zero, resolveAt: 30f);
        world.AddHazard(late);
        Assert.That(planner.Plan(new Vector2(-10f, 0f), new Vector2(10f, 0f), 6f), Is.Null, "long gone past it by then");
    }

    [Test]
    public void AHazardTheStratSendsTheBotIntoNeverBlocks()
    {
        var (world, caster) = World();
        var planner = new NavPlanner(world);
        world.AddHazard(CircleAt(caster, new Vector3(10f, 0f, 0f), resolveAt: 1f));
        Assert.That(planner.Plan(new Vector2(-10f, 0f), new Vector2(10f, 0f), 6f), Is.Null, "the destination is the stack");
        world.AddHazard(CircleAt(caster, new Vector3(-10f, 0f, 0f), resolveAt: 1f));
        Assert.That(planner.Plan(new Vector2(-10f, 0f), new Vector2(0f, 10f), 6f), Is.Null, "leaving one it stands in is not a detour");
    }

    // The M11S tank's case: it stands 4.5 y from a tornado whose keep-out is 5 y when the walk
    // starts. Every corner, the first above all, has to be farther out than where it stands.
    [Test]
    public void AStartInsideTheKeepOutLeavesOutward()
    {
        var (world, _) = World();
        var planner = new NavPlanner(world);
        var tornado = new CircleObstacle(Vector2.Zero, 5f);
        world.Obstacles.Add(tornado);
        var from = new Vector2(-4.35f, 1.17f);
        var path = planner.Plan(from, new Vector2(10f, -2f), 6f);
        Assert.That(path, Is.Not.Null);
        TestContext.Out.WriteLine(string.Join(" -> ", path!.Select(c => $"({c.X:0.00},{c.Y:0.00}) r={tornado.SignedDistance(c) + 5f:0.00}")));
        Assert.That(tornado.SignedDistance(path[0]), Is.GreaterThan(tornado.SignedDistance(from)), "the first corner is farther from the tornado than the start");
        foreach (var corner in path) Assert.That(tornado.SignedDistance(corner), Is.GreaterThanOrEqualTo(0f));
        var firstLeg = Vector2.Normalize(path[0] - from);
        Assert.That(Vector2.Dot(firstLeg, Vector2.Normalize(from)), Is.GreaterThan(0f), "the first leg points away from the centre, not across it");
    }
}

