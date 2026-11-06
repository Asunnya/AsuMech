using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.SimObjects;

namespace AsuMech.Core.Game.Motion;

// Plans a bot's walk around what it must not cross: the arena's edge, the obstacles, and the
// hazards that resolve before it would get past them. A hazard the walk starts in, or ends in, is
// the strat's business (a stack, a tower, a bait) and never blocks; one covering most of the floor
// (a raidwide) can't be walked around and doesn't either. Null when the straight line is clear.
internal sealed class NavPlanner(SimWorld world)
{
    // Body clearance past an edge, so a corner of the path doesn't clip it.
    private const float Margin = 0.6f;
    // A hazard resolving this long after the bot would arrive still counts: arrivals run late.
    private const float HazardLead = 0.5f;
    private const float GridPadding = 2f;
    private const float MaxHalfExtent = 40f;

    // The arena-and-obstacles grid is the same for every bot in a frame; rebuilt when the clock moves.
    private NavGrid? baseGrid;
    private float baseGridAt = float.NaN;
    private int baseGridObstacles = -1;

    public List<Vector2>? Plan(Vector2 from, Vector2 to, float speed)
    {
        if (!world.Obstacles.PlanRoutes) return null;
        var now = world.Events.Elapsed;
        var travel = Vector2.Distance(from, to) / MathF.Max(speed, 1f);
        var from3 = new Vector3(from.X, 0f, from.Y);
        var to3 = new Vector3(to.X, 0f, to.Y);
        var blocking = new List<AoeQuery>();
        foreach (var hazard in world.Hazards)
        {
            if (hazard.ResolveAt < now || hazard.ResolveAt > now + travel + HazardLead) continue;
            var query = hazard.Query();
            if (query.SignedDistance(to3) is not { } atEnd || atEnd <= Margin) continue;
            if (query.SignedDistance(from3) is not { } atStart || atStart <= 0f) continue;
            blocking.Add(query);
        }
        // The straight line, sampled: nothing to plan unless something sits on it.
        if (LineClear(from, to, blocking)) return null;

        var grid = BaseGrid(now);
        if (blocking.Count > 0)
        {
            grid = grid.Copy();
            foreach (var query in blocking)
            {
                if (Covers(grid, query) > 0.5f) continue;
                grid.Block(p => query.SignedDistance(new Vector3(p.X, 0f, p.Y)) < Margin);
            }
            grid.FinishCosts();
        }
        if (grid.SegmentClear(from, to)) return null;
        return grid.FindPath(from, to);
    }

    private bool LineClear(Vector2 a, Vector2 b, List<AoeQuery> blocking)
    {
        var length = Vector2.Distance(a, b);
        var steps = Math.Max(1, (int)MathF.Ceiling(length / 0.5f));
        for (var i = 0; i <= steps; i++)
        {
            var p = Vector2.Lerp(a, b, (float)i / steps);
            foreach (var o in world.Obstacles.All)
                if (o.SignedDistance(p) < Margin) return false;
            foreach (var q in blocking)
                if (q.SignedDistance(new Vector3(p.X, 0f, p.Y)) < Margin) return false;
        }
        return true;
    }

    private NavGrid BaseGrid(float now)
    {
        if (baseGrid is { } cached && baseGridAt == now && baseGridObstacles == world.Obstacles.All.Count) return cached;
        var grid = BuildBase();
        baseGrid = grid;
        baseGridAt = now;
        baseGridObstacles = world.Obstacles.All.Count;
        return grid;
    }

    private NavGrid BuildBase()
    {
        var arena = world.ArenaShape;
        var halfExtent = arena switch
        {
            SquareArena square => square.HalfWidth + GridPadding,
            CircleArena circle => circle.Radius + GridPadding,
            _ => MaxHalfExtent,
        };
        var grid = new NavGrid(halfExtent);
        if (arena is { }) grid.Block(p => arena.IsOutside(new Vector3(p.X, 0f, p.Y)));
        foreach (var obstacle in world.Obstacles.All)
            grid.Block(p => obstacle.SignedDistance(p) < Margin);
        grid.FinishCosts();
        return grid;
    }

    private static float Covers(NavGrid grid, AoeQuery query)
    {
        var inside = 0;
        var n = grid.Cells;
        for (var x = 0; x < n; x += 2)
        for (var z = 0; z < n; z += 2)
        {
            var c = grid.Center(x, z);
            if (query.SignedDistance(new Vector3(c.X, 0f, c.Y)) < 0f) inside++;
        }
        return inside / (float)((n / 2 + 1) * (n / 2 + 1));
    }
}
