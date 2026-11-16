using System.Collections.Generic;

namespace AsuMech.Scenarios.M12s.Opener;

// Pins the random rolls; null keeps the roll.
public sealed class M12sOpenerStateOverrides
{
    public bool? PurpleEast { get; set; }
    // Each pair's two orbs; set together with a matching PurpleEast.
    public IReadOnlyList<IReadOnlyList<MortalOrb>>? Orbs { get; set; }
}
