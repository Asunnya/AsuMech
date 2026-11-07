using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3Blackfire;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP3BlackfireStateOverrides
{
    // Where Nael lands; Twintania and Bahamut keep their places around her.
    public Direction? NaelFrom { get; set; }
    public PartyRole? LiquidHellTarget { get; set; }
    // A role that must get a Megaflare stack marker, and one that must not (a tower).
    public PartyRole? StackIncludes { get; set; }
    public PartyRole? TowerIncludes { get; set; }

    public IReadOnlyList<PartyRole>? StackTargets { get; set; }
    public PartyRole? BeamTarget { get; set; }
    public IReadOnlyList<PartyRole>? HypernovaTargets { get; set; }
}
