using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.DeepAerial;

// Pins the random rolls; null keeps the roll.
public sealed class M10sDeepAerialStateOverrides
{
    public bool? BossesNorth { get; set; }
    // The player's tether in rounds 1–3 and in rounds 4–6: round * 2 + (water ? 1 : 0), round counted within its half.
    public int? PlayerFirstTether { get; set; }
    public int? PlayerSecondTether { get; set; }
    public IReadOnlyList<(PartyRole Fire, PartyRole Water)>? Tethers { get; set; }
}
