using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3Heavensfall;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP3HeavensfallStateOverrides
{
    public HeavensfallFormation? Formation { get; set; }
    // Nael's bearing in 45° steps clockwise from north.
    public int? NaelStep { get; set; }
    public IReadOnlyList<int>? TowerSlots { get; set; }
    public bool? BurstsClockwise { get; set; }
    // A role that must be among that mechanic's targets.
    public PartyRole? DiveIncludes { get; set; }
    public PartyRole? TwisterIncludes { get; set; }
    public PartyRole? MegaflarePuddleIncludes { get; set; }
    public PartyRole? HypernovaTarget { get; set; }
    public PartyRole? FireballTarget { get; set; }
}
