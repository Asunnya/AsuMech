using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P5Wrath;

// Pins the random rolls; null keeps the roll.
public sealed class DsrP5WrathStateOverrides
{
    // Compass bearing of Vedrfolnir, the twister dragon: 0, 90, 180 or 270.
    public float? DragonBearing { get; set; }
    public bool? MageOnDragonSide { get; set; }
    // Ignasse's tether first, then Vellguine's.
    public IReadOnlyList<PartyRole>? Tethered { get; set; }
    public PartyRole? Defamation { get; set; }
    public PartyRole? Divebomb { get; set; }
    public IReadOnlyList<PartyRole>? Lightning { get; set; }
    public PartyRole? LiquidHeaven { get; set; }
    public PartyRole? Eruptions { get; set; }
}
