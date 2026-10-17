using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.Opener;

// Pins the random rolls; null keeps the roll.
public sealed class M10sOpenerStateOverrides
{
    // Wall Red Hot leaps to: 0 north, 1 east, 2 south, 3 west.
    public int? Wall { get; set; }
    // The four tethered players, first to fourth.
    public IReadOnlyList<PartyRole>? FloaterOrder { get; set; }
    public PartyRole? PyrotationTarget { get; set; }
    // The player's own Flame Floater number, 1-4, or 0 to stay untethered.
    public int? PlayerFloaterNumber { get; set; }
}
