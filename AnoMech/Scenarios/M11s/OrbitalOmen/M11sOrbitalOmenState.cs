using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Scenarios.M11s.OrbitalOmen;

public sealed class M11sOrbitalOmenState
{
    public const int Pairs = 4;
    public static readonly int[] Offsets = [-15, -5, 5, 15];

    // Each pair fires one line from a north portal (north-south, at that x) and one from an east
    // portal (east-west, at that z). The game picks one of a fixed set of orders; the second pair
    // always crosses at (±5, ±5). Only these eight have been seen.
    public static readonly (int North, int East)[][] Patterns =
    [
        [(-15, 15), (5, -5), (15, -15), (-5, 5)],
        [(15, 15), (-5, -5), (-15, 5), (5, -15)],
        [(-5, 5), (5, -5), (-15, 15), (15, -15)],
        [(5, -15), (-5, -5), (15, 5), (-15, 15)],
        [(15, -15), (5, 5), (-5, 15), (-15, -5)],
        [(-15, 15), (-5, 5), (15, -15), (5, -5)],
        [(15, 15), (5, 5), (-15, -5), (-5, -15)],
        [(-15, -5), (-5, 5), (5, -15), (15, 15)],
    ];

    public IReadOnlyList<int> NorthLines { get; }
    public IReadOnlyList<int> EastLines { get; }

    public M11sOrbitalOmenState(Rng rng, M11sOrbitalOmenStateOverrides? overrides = null)
    {
        overrides ??= new M11sOrbitalOmenStateOverrides();
        var pattern = rng.Next(Patterns.Length);
        var pairs = Patterns[overrides.Pattern ?? pattern];
        NorthLines = pairs.Select(p => p.North).ToList();
        EastLines = pairs.Select(p => p.East).ToList();
    }

    public static int NorthSlotOffset(int x) => System.Array.IndexOf(Offsets, x);
}
