using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3Quickmarch;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP3QuickmarchStateOverrides
{
    // Where the three dragons dive from.
    public Direction? DiveFrom { get; set; }
    public PartyRole? EarthShakerDps { get; set; }
    // A role that must be among the four spreads, or under one of the four puddles.
    public PartyRole? SpreadIncludes { get; set; }
    public PartyRole? PuddleIncludes { get; set; }

    public IReadOnlyList<PartyRole>? TwisterTargets { get; set; }
    public IReadOnlyList<PartyRole>? PuddleTargets { get; set; }
    public IReadOnlyList<PartyRole>? SpreadTargets { get; set; }
}
