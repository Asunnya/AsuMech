using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P5Death;

// Pins the random rolls; null keeps the roll.
public sealed class DsrP5DeathStateOverrides
{
    // Compass bearing of Ser Guerrique, the warrior: 0, 90, 180 or 270.
    public float? WarriorBearing { get; set; }
    public IReadOnlyCollection<PartyRole>? Dooms { get; set; }
    public float? ThordanBearing { get; set; }
    public float? EyeBearing { get; set; }
    // The first doom (in party order) without a circle takes the square when true.
    public bool? FirstDoomSquare { get; set; }
    // Where the player stands in the conga line, 0 = leftmost facing the warrior; null keeps their role's spot.
    public int? PlayerCongaSlot { get; set; }
}
