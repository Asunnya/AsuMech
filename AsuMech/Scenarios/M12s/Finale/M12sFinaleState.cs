using System.Collections.Generic;
using System.Numerics;

namespace AsuMech.Scenarios.M12s.Finale;

// Nothing here rolls: the finale plays the same every pull.
public sealed class M12sFinaleState
{
    public static IReadOnlyList<Vector3> FourClones { get; } =
        [new(10f, 0f, 0f), new(-10f, 0f, 0f), new(0f, 0f, -10f), new(0f, 0f, 10f)];

    public static IReadOnlyList<Vector3> EightClones { get; } =
    [
        new(10f, 0f, 5f), new(10f, 0f, -5f), new(-10f, 0f, -5f), new(-10f, 0f, 5f),
        new(5f, 0f, -10f), new(-5f, 0f, -10f), new(-5f, 0f, 10f), new(5f, 0f, 10f),
    ];
}
