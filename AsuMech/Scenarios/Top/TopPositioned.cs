using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Top;

public static class TopPositioned
{
    public static IPositioned From(Placement placement) => new At(placement.Position, placement.Rotation);

    private readonly record struct At(Vector3 Position, float Rotation) : IPositioned;
}
