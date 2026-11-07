using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3Fellruin;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP3FellruinStateOverrides
{
    // Index into UcobP3FellruinState.QuoteChoices.
    public int? Quote { get; set; }
    public int? BahamutSlot { get; set; }
    public bool? NaelClockwiseOfBahamut { get; set; }
    public PartyRole? RavenDiveTarget { get; set; }
    public PartyRole? MeteorIncludes { get; set; }
    public IReadOnlyList<PartyRole>? MeteorTargets { get; set; }
}
