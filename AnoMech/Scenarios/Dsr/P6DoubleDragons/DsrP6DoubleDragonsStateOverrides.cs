using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Dsr.P6DoubleDragons;

// Pins the random rolls; null keeps the roll.
public sealed class DsrP6DoubleDragonsStateOverrides
{
    public IReadOnlyCollection<PartyRole>? NidhoggTethers { get; set; }
    public BreathGlow? Glow { get; set; }
    public PartyRole? VowTarget { get; set; }
    public bool? HraesvelgrStackOnRegen { get; set; }
    public bool? WingsNorth { get; set; }
    public bool? PlumesNear { get; set; }
    public bool? NidhoggDivesEast { get; set; }
    public bool? NidhoggFromSouth { get; set; }
    public IReadOnlyCollection<PartyRole>? Spreads { get; set; }
    public IReadOnlyCollection<PartyRole>? Stacks { get; set; }
    public PartyRole? AkhMornTarget { get; set; }
    public int? HraesvelgrLane { get; set; }
    public bool? HraesvelgrFromSouth { get; set; }
    public (int X, int Z)? SecondSetCorner { get; set; }
    public bool? HotWing { get; set; }
    public IReadOnlyCollection<PartyRole>? SecondNidhoggTethers { get; set; }
    public BreathGlow? SecondGlow { get; set; }
    public bool? SecondHraesvelgrStackOnRegen { get; set; }
    public bool? SecondWingsNorth { get; set; }
    public bool? SecondPlumesNear { get; set; }
    public bool? SecondHotWing { get; set; }
    public bool? NidhoggCauterizesWest { get; set; }
}
