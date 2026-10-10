using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P2Thordan;

// Pins the random rolls; null keeps the roll. Bearings are compass degrees (0 = north).
public sealed class DsrP2ThordanStateOverrides
{
    public float? ThordanBearing { get; set; }
    public float? SafeLineBearing { get; set; }
    public float? GuerriqueBearing { get; set; }
    public IReadOnlyList<PartyRole>? Defamations { get; set; }

    public bool? SanctityClockwise { get; set; }
    public float? DarkKnightBearing { get; set; }
    public PartyRole? FirstPlungeTarget { get; set; }
    public PartyRole? SecondPlungeTarget { get; set; }
    public IReadOnlyList<PartyRole>? MeteorTargets { get; set; }
    // The player always gets a meteor, partnered with a random member of their role.
    public bool? MeteorOnMe { get; set; }
    public IReadOnlyList<(float Bearing, float Radius)>? FirstTowers { get; set; }
    public IReadOnlyList<bool>? BroadSwingRightFirst { get; set; }
}
