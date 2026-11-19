using System;
using System.Numerics;

namespace AsuMech.Scenarios.Fru;

public static class FruP1Geometry
{
    // Octants clockwise from north.
    public static Vector3 Point(float octant, float radius)
        => new(MathF.Sin(octant * MathF.PI / 4) * radius, 0, -MathF.Cos(octant * MathF.PI / 4) * radius);
}
