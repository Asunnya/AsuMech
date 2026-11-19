using System;
using System.Numerics;

namespace AsuMech.Core.Game.Geometry;

public sealed class RectArena(float halfWidth, float halfDepth) : IArenaShape
{
    public float HalfWidth { get; } = halfWidth;
    public float HalfDepth { get; } = halfDepth;

    public bool IsOutside(Vector3 local) => MathF.Abs(local.X) > HalfWidth || MathF.Abs(local.Z) > HalfDepth;
}
