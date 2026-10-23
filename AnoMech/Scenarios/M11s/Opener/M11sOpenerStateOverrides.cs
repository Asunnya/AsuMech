using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M11s.Opener;

// Pins the random rolls; null keeps the roll.
public sealed class M11sOpenerStateOverrides
{
    public bool? AxeTrophyFirst { get; set; }
    // Weapons in the order each Assault Evolved resolves them.
    public IReadOnlyList<Weapon>? FirstWeapons { get; set; }
    public IReadOnlyList<Weapon>? SecondWeapons { get; set; }
    // Bearing of each set's first weapon, in 15° steps.
    public int? FirstWeaponStep { get; set; }
    public int? SecondWeaponStep { get; set; }
    public bool? StackFirst { get; set; }
    public PartyRole? CrushingCometTarget { get; set; }
}
