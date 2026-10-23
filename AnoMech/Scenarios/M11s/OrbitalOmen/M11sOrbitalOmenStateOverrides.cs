using System.Collections.Generic;

namespace AnoMech.Scenarios.M11s.OrbitalOmen;

// Pins the random rolls; null keeps the roll.
public sealed class M11sOrbitalOmenStateOverrides
{
    // Offsets (-15, -5, 5 or 15) of the north portals' lines, in firing order.
    public IReadOnlyList<int>? NorthLines { get; set; }
    // Offsets of the east portals' lines, in firing order.
    public IReadOnlyList<int>? EastLines { get; set; }
}
