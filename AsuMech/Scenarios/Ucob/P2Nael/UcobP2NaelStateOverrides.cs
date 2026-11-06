using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P2Nael;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP2NaelStateOverrides
{
    public IReadOnlyList<PartyRole>? MeteorOrder { get; set; }
    public IReadOnlyList<PartyRole>? IceOrder { get; set; }
    public IReadOnlyList<PartyRole>? FireTargets { get; set; }
    public IReadOnlyList<IReadOnlyList<PartyRole>>? ThunderTargets { get; set; }
    public IReadOnlyList<IReadOnlyList<PartyRole>>? DoomTargets { get; set; }
    public IReadOnlyList<IReadOnlyList<PartyRole>>? WingsTargets { get; set; }
    public IReadOnlyList<PartyRole>? BeamTargets { get; set; }
    public PartyRole? RavenDiveTarget { get; set; }
    public IReadOnlyList<int>? DragonSlots { get; set; }

    // Index into that quote's two choices.
    public int?[] Quotes { get; } = new int?[UcobP2NaelState.QuoteChoices.Length];
    public PartyRole?[] FirePicks { get; } = new PartyRole?[UcobP2NaelState.FireTethers];
    // A role that must be among that set's targets.
    public PartyRole?[] ThunderIncludes { get; } = new PartyRole?[UcobP2NaelState.ThunderCounts.Length];
    public PartyRole?[] DoomIncludes { get; } = new PartyRole?[UcobP2NaelState.DoomCounts.Length];
}
