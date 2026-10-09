using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P3Nidhogg;

// Pins the random rolls; null keeps the roll.
public sealed class DsrP3NidhoggStateOverrides
{
    public IReadOnlyDictionary<PartyRole, int>? Lines { get; set; }
    public IReadOnlyDictionary<PartyRole, DiveArrow>? Arrows { get; set; }
    public IReadOnlyList<bool>? LashFirst { get; set; }
    // Soakers each Darkdragon Dive tower needs, NE, SE, SW, NW.
    public IReadOnlyList<int>? TowerSoakers { get; set; }
    public int? QuietTower { get; set; }
    public IReadOnlyList<PartyRole>? SoulTetherTargets { get; set; }
}
