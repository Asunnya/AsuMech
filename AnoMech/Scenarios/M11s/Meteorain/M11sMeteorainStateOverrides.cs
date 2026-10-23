using System.Collections.Generic;

namespace AnoMech.Scenarios.M11s.Meteorain;

// Pins the random rolls; null keeps the roll.
public sealed class M11sMeteorainStateOverrides
{
    // Each set's two shade sides: 0 north, 1 east, 2 south, 3 west.
    public IReadOnlyList<(int, int)>? ShadeSides { get; set; }
}
