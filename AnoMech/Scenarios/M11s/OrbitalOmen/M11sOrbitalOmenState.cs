using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Scenarios.M11s.OrbitalOmen;

public sealed class M11sOrbitalOmenState
{
    public const int Pairs = 4;
    public static readonly int[] Offsets = [-15, -5, 5, 15];

    // Each pair fires one line from a north portal (north-south, at that x) and one from an east
    // portal (east-west, at that z).
    public IReadOnlyList<int> NorthLines { get; }
    public IReadOnlyList<int> EastLines { get; }

    public M11sOrbitalOmenState(Rng rng, M11sOrbitalOmenStateOverrides? overrides = null)
    {
        overrides ??= new M11sOrbitalOmenStateOverrides();
        var north = rng.Shuffle(Offsets).ToList();
        var east = rng.Shuffle(Offsets).ToList();
        NorthLines = overrides.NorthLines ?? north;
        EastLines = overrides.EastLines ?? east;
    }

    // Once a pair has fired, the square where its two lines crossed is safe from every later line.
    public (float X, float Z) FirstCrossing => (NorthLines[0], EastLines[0]);

    public static int NorthSlotOffset(int x) => System.Array.IndexOf(Offsets, x);
}
