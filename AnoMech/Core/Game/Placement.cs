using System;
using System.Numerics;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.Game;

public readonly record struct Placement(Vector3 Position, float Rotation)
{
    // Advances Position by `distance` units along the facing direction (Rotation 0 → +Z).
    // Negative distance moves backward.
    public Placement MoveForward(float distance) =>
        this with { Position = Position + new Vector3(MathF.Sin(Rotation), 0f, MathF.Cos(Rotation)) * distance };

    public Placement MoveRight(float distance) =>
        new(Position + (new Vector3(-MathF.Cos(Rotation), 0, MathF.Sin(Rotation)) * distance), Rotation);

    // Rotates Position around world origin in the XZ plane by `angle` radians
    // and advances Rotation by the same amount.
    public Placement RotateAroundOrigin(float angle)
    {
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        return this with
        {
            Position = new Vector3(
                Position.X * cos - Position.Z * sin,
                Position.Y,
                Position.X * sin + Position.Z * cos),
            Rotation = Rotation - angle
        };
    }

    public Vector2 Position2 => new(Position.X, Position.Z);

    // Returns a copy rotated to look toward `source` in the XZ plane
    // (Rotation 0 → +Z, matching MoveForward). Position is unchanged.
    // Returns unchanged when `source` coincides with Position.
    public Placement Face(Vector3 source)
    {
        var dx = source.X - Position.X;
        var dz = source.Z - Position.Z;
        if (dx * dx + dz * dz < 1e-6f) return this;
        return this with { Rotation = MathF.Atan2(dx, dz) };
    }

    // `point` within 45° of the facing (the front 90° arc). False when it coincides with Position.
    public bool IsLookingAt(Vector3 point) => FacingCos(point) >= CosHalfArc;

    // `point` within 45° of straight behind (the back 90° arc). False when it coincides with Position.
    public bool IsLookingAwayFrom(Vector3 point) => FacingCos(point) <= -CosHalfArc;

    private const float CosHalfArc = 0.70710677f;

    // NaN on top of Position, so both arc tests fail.
    private float FacingCos(Vector3 point)
    {
        var dx = point.X - Position.X;
        var dz = point.Z - Position.Z;
        var distance = MathF.Sqrt(dx * dx + dz * dz);
        return distance < 0.01f ? float.NaN : (dx * MathF.Sin(Rotation) + dz * MathF.Cos(Rotation)) / distance;
    }

    // Squared distance to `other` in the XZ plane (height ignored), matching the
    // horizontal convention of Face / MoveForward. Squared so callers can compare
    // against a threshold without the sqrt.
    public float DistanceSq(IPositioned other)
    {
        return DistanceSq(other.Position);
    }

    public float DistanceSq(Vector3 other)
    {
        var dx = other.X - Position.X;
        var dz = other.Z - Position.Z;
        return dx * dx + dz * dz;
    }

    public Placement MulX(float mul)
    {
        return new Placement(Position with { X = Position.X * mul }, Rotation);
    }

    public Placement MulRot(float mul)
    {
        return new Placement(Position, Rotation * mul);
    }

    public Placement AddToRotation(float add)
    {
        return this with { Rotation = Rotation + add };
    }
}
