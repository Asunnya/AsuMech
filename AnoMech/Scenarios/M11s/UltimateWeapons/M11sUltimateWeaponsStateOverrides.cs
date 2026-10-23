using System.Collections.Generic;

namespace AnoMech.Scenarios.M11s.UltimateWeapons;

// Pins the random rolls; null keeps the roll.
public sealed class M11sUltimateWeaponsStateOverrides
{
    // The side Dance of Domination's lines leave open: 0 south, 1 east, 2 north, 3 west.
    public int? OpenSide { get; set; }
    public bool? AxeTrophy { get; set; }
    // The first weapon on the west half instead of the east.
    public bool? Mirrored { get; set; }
    // All six, in the order they strike.
    public IReadOnlyList<Weapon>? Weapons { get; set; }
}
