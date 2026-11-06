using System;
using System.Collections.Generic;
using System.Numerics;

namespace AnoMech.Core.Game.Geometry;

// A square grid over the arena, in scenario-local XZ, with the cells a bot may not stand in and a
// cost for the ones next to them, and a path finder over it. Built per request by NavPlanner from
// the obstacles and the hazards of the moment; the steering in ObstacleField keeps handling what
// appears mid-walk.
public sealed class NavGrid
{
    public const float CellSize = 0.5f;
    // A cell this close to a blocked one costs more, so a path hugs a hazard only when it must.
    private const float NearBlockedYalms = 1f;
    private const float NearBlockedCost = 1.5f;

    private readonly bool[,] blocked;
    private readonly float[,] extra;

    public float HalfExtent { get; }
    public int Cells { get; }

    public NavGrid(float halfExtent)
    {
        HalfExtent = halfExtent;
        Cells = Math.Max(2, (int)MathF.Ceiling(halfExtent * 2f / CellSize));
        blocked = new bool[Cells, Cells];
        extra = new float[Cells, Cells];
    }

    public NavGrid Copy()
    {
        var copy = new NavGrid(HalfExtent);
        Array.Copy(blocked, copy.blocked, blocked.Length);
        Array.Copy(extra, copy.extra, extra.Length);
        return copy;
    }

    public Vector2 Center(int ix, int iz) => new(-HalfExtent + (ix + 0.5f) * CellSize, -HalfExtent + (iz + 0.5f) * CellSize);

    public (int X, int Z) CellOf(Vector2 p)
        => (Math.Clamp((int)MathF.Floor((p.X + HalfExtent) / CellSize), 0, Cells - 1), Math.Clamp((int)MathF.Floor((p.Y + HalfExtent) / CellSize), 0, Cells - 1));

    public bool IsBlocked(int ix, int iz) => blocked[ix, iz];
    public bool IsBlocked(Vector2 p) { var (x, z) = CellOf(p); return blocked[x, z]; }

    public int BlockedCount
    {
        get
        {
            var n = 0;
            for (var x = 0; x < Cells; x++)
            for (var z = 0; z < Cells; z++)
                if (blocked[x, z]) n++;
            return n;
        }
    }

    // Marks every cell whose centre `inside` says is off limits, then the cost band around them.
    public void Block(Func<Vector2, bool> inside)
    {
        for (var x = 0; x < Cells; x++)
        for (var z = 0; z < Cells; z++)
            if (!blocked[x, z] && inside(Center(x, z))) blocked[x, z] = true;
    }

    public void FinishCosts()
    {
        var band = (int)MathF.Ceiling(NearBlockedYalms / CellSize);
        for (var x = 0; x < Cells; x++)
        for (var z = 0; z < Cells; z++)
        {
            if (blocked[x, z]) continue;
            var near = false;
            for (var dx = -band; dx <= band && !near; dx++)
            for (var dz = -band; dz <= band; dz++)
            {
                var nx = x + dx;
                var nz = z + dz;
                if (nx < 0 || nz < 0 || nx >= Cells || nz >= Cells) continue;
                if (blocked[nx, nz]) { near = true; break; }
            }
            extra[x, z] = near ? NearBlockedCost : 0f;
        }
    }

    // True when no blocked cell lies on the straight line between the two points, sampled every half cell.
    public bool SegmentClear(Vector2 a, Vector2 b)
    {
        var length = Vector2.Distance(a, b);
        var steps = Math.Max(1, (int)MathF.Ceiling(length / (CellSize * 0.5f)));
        for (var i = 0; i <= steps; i++)
            if (IsBlocked(Vector2.Lerp(a, b, (float)i / steps))) return false;
        return true;
    }

    // The corners of the shortest grid path from `from` to `to`, `to` last, pulled straight where
    // the line of sight allows; null when the destination can't be reached. A start inside a
    // blocked cell leaves through its nearest free neighbour; a blocked destination is unreachable.
    public List<Vector2>? FindPath(Vector2 from, Vector2 to)
    {
        var start = CellOf(from);
        var goal = CellOf(to);
        if (blocked[goal.X, goal.Z]) return null;
        if (SegmentClear(from, to)) return [to];
        var n = Cells;
        var g = new float[n, n];
        var cameFrom = new int[n, n];
        for (var x = 0; x < n; x++)
        for (var z = 0; z < n; z++)
        {
            g[x, z] = float.MaxValue;
            cameFrom[x, z] = -1;
        }
        var open = new PriorityQueue<(int X, int Z), float>();
        g[start.X, start.Z] = 0f;
        open.Enqueue(start, Heuristic(start, goal));
        var closed = new bool[n, n];
        var found = false;
        while (open.TryDequeue(out var current, out _))
        {
            if (closed[current.X, current.Z]) continue;
            closed[current.X, current.Z] = true;
            if (current == goal) { found = true; break; }
            for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                var nx = current.X + dx;
                var nz = current.Z + dz;
                if (nx < 0 || nz < 0 || nx >= n || nz >= n || blocked[nx, nz] || closed[nx, nz]) continue;
                // No cutting a blocked corner diagonally.
                if (dx != 0 && dz != 0 && (blocked[current.X + dx, current.Z] || blocked[current.X, current.Z + dz])) continue;
                var step = (dx != 0 && dz != 0 ? 1.41421356f : 1f) * CellSize + extra[nx, nz];
                var tentative = g[current.X, current.Z] + step;
                if (tentative >= g[nx, nz]) continue;
                g[nx, nz] = tentative;
                cameFrom[nx, nz] = current.X * n + current.Z;
                open.Enqueue((nx, nz), tentative + Heuristic((nx, nz), goal));
            }
        }
        if (!found) return null;
        var cells = new List<(int X, int Z)>();
        var at = goal;
        while (at != start)
        {
            cells.Add(at);
            var prev = cameFrom[at.X, at.Z];
            if (prev < 0) break;
            at = (prev / n, prev % n);
        }
        cells.Reverse();
        var points = new List<Vector2>(cells.Count + 1);
        foreach (var c in cells) points.Add(Center(c.X, c.Z));
        if (points.Count > 0) points[^1] = to;
        return Pull(from, points);
    }

    private float Heuristic((int X, int Z) a, (int X, int Z) b)
    {
        var dx = Math.Abs(a.X - b.X);
        var dz = Math.Abs(a.Z - b.Z);
        return (Math.Max(dx, dz) + 0.41421356f * Math.Min(dx, dz)) * CellSize;
    }

    // Keeps only the corners: from each kept point, the farthest later point still in line of sight.
    private List<Vector2> Pull(Vector2 from, List<Vector2> points)
    {
        var result = new List<Vector2>();
        var anchor = from;
        var i = 0;
        while (i < points.Count)
        {
            var farthest = i;
            for (var j = points.Count - 1; j > i; j--)
                if (SegmentClear(anchor, points[j])) { farthest = j; break; }
            result.Add(points[farthest]);
            anchor = points[farthest];
            i = farthest + 1;
        }
        return result;
    }
}
