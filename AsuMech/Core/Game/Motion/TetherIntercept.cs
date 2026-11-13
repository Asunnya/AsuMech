using System;
using System.Numerics;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.SimObjects;

namespace AsuMech.Core.Game.Motion;

// Where to walk into a tether's line from a given spot: the nearest point on it, parked `margin`
// yalms short of either end (the midpoint when the line is shorter than that), slid along the
// line to clear obstacles. Null once an end is gone.
public static class TetherIntercept
{
    public static Vector2? Point(SimTether? tether, Vector3 from, float margin, ObstacleField obstacles)
    {
        if (tether is not { A: { } a, B: { } b } || !a.IsAlive() || !b.IsAlive()) return null;
        var src = new Vector2(a.Position.X, a.Position.Z);
        var seg = new Vector2(b.Position.X, b.Position.Z) - src;
        var len = seg.Length();
        if (len < 1e-6f) return src;
        var rel = new Vector2(from.X, from.Z) - src;
        var inset = margin / len;
        var (tMin, tMax) = 2f * inset >= 1f ? (0.5f, 0.5f) : (inset, 1f - inset);
        var t = Math.Clamp(Vector2.Dot(rel, seg) / (len * len), tMin, tMax);
        return obstacles.NearestClearOnSegment(src, src + seg, t, tMin, tMax);
    }
}
