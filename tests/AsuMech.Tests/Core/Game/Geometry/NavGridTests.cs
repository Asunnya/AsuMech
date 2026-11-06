using System.Numerics;
using AsuMech.Core.Game.Geometry;

namespace AsuMech.Tests;

public class NavGridTests
{
    private static NavGrid Grid(params CircleObstacle[] obstacles)
    {
        var grid = new NavGrid(22f);
        var square = new SquareArena(20f);
        grid.Block(p => square.IsOutside(new Vector3(p.X, 0f, p.Y)));
        foreach (var o in obstacles) grid.Block(p => o.SignedDistance(p) < 0.6f);
        grid.FinishCosts();
        return grid;
    }

    private static void AssertWalkable(NavGrid grid, Vector2 from, List<Vector2> path, params CircleObstacle[] obstacles)
    {
        var at = from;
        foreach (var corner in path)
        {
            Assert.That(grid.SegmentClear(at, corner), $"leg {at} -> {corner} crosses a blocked cell");
            for (var t = 0f; t <= 1f; t += 0.05f)
            {
                var p = Vector2.Lerp(at, corner, t);
                foreach (var o in obstacles) Assert.That(o.SignedDistance(p), Is.GreaterThan(0.2f), $"{p} is inside an obstacle");
            }
            at = corner;
        }
    }

    [Test]
    public void AClearLineIsOneLeg()
    {
        var grid = Grid();
        Assert.That(grid.FindPath(new Vector2(-10f, 0f), new Vector2(10f, 0f)), Is.EqualTo(new[] { new Vector2(10f, 0f) }));
        Assert.That(grid.SegmentClear(new Vector2(-10f, 0f), new Vector2(10f, 0f)));
    }

    [Test]
    public void OneObstacleOnTheLineIsRounded()
    {
        var tornado = new CircleObstacle(Vector2.Zero, 5f);
        var grid = Grid(tornado);
        var from = new Vector2(-12f, 0f);
        var to = new Vector2(12f, 0f);
        Assert.That(grid.SegmentClear(from, to), Is.False);
        var path = grid.FindPath(from, to)!;
        Assert.That(path, Is.Not.Null);
        Assert.That(path.Count, Is.GreaterThanOrEqualTo(2), "at least one corner before the destination");
        Assert.That(path[^1], Is.EqualTo(to));
        AssertWalkable(grid, from, path, tornado);
        var length = path.Prepend(from).Zip(path, Vector2.Distance).Sum();
        Assert.That(length, Is.LessThan(24f * 1.35f), "a round of a 5 y circle costs well under a third more than the straight line");
    }

    [Test]
    public void TwoObstaclesLeaveTheGapOrGoAround()
    {
        var a = new CircleObstacle(new Vector2(0f, -6f), 5f);
        var b = new CircleObstacle(new Vector2(0f, 6f), 5f);
        var grid = Grid(a, b);
        var from = new Vector2(-12f, 0f);
        var to = new Vector2(12f, 2f);
        var path = grid.FindPath(from, to)!;
        Assert.That(path, Is.Not.Null);
        AssertWalkable(grid, from, path, a, b);
        Assert.That(path[^1], Is.EqualTo(to));
    }

    [Test]
    public void ABlockedDestinationHasNoPathAndTheEdgeIsBlocked()
    {
        var tornado = new CircleObstacle(new Vector2(5f, 5f), 5f);
        var grid = Grid(tornado);
        Assert.That(grid.FindPath(new Vector2(-10f, 0f), new Vector2(5f, 5f)), Is.Null);
        Assert.That(grid.IsBlocked(new Vector2(21f, 0f)), "outside the square arena");
        Assert.That(grid.IsBlocked(new Vector2(19f, 0f)), Is.False);
        Assert.That(grid.FindPath(new Vector2(-19f, -19f), new Vector2(19f, 19f)), Is.Not.Null, "corner to corner around the obstacle");
    }
}
