using System.Collections.Generic;

namespace AnoMech.Scenarios.Ucob.P5Exaflares;

// User-controlled overrides for UcobP5ExaflaresState's randomized fields. Direction is where
// the whole set of lanes travels TO; null leaves it randomized at scenario start.
public sealed class UcobP5ExaflaresStateOverrides
{
    public Direction? Direction { get; set; }

    // Lane offsets in firing order, two per pair.
    internal IReadOnlyList<float>? LaneOrder { get; set; }
}
