using System.Collections.Generic;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

// Pins the random rolls; null keeps the roll.
public sealed class DsrP7DragonKingStateOverrides
{
    // Per cycle, the party, second party and tank spots' exaflare headings in 45° steps from south.
    public IReadOnlyList<IReadOnlyList<int>>? ExaflareHeadings { get; set; }
    public IReadOnlyList<AscalonElement>? Elements { get; set; }
    public IReadOnlyList<IReadOnlyList<int>>? GigaflareOrder { get; set; }
}
