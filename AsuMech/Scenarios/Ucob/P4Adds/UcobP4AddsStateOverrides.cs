using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P4Adds;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP4AddsStateOverrides
{
    public PartyRole TwintaniaTank { get; set; } = PartyRole.MainTank;
    // Index into UcobP4AddsState.QuoteChoices.
    public int? FirstQuote { get; set; }
    // Index into the two quotes that open the other way: 0 ends on a spread, 1 on a stack.
    public int? SecondQuote { get; set; }
    public IReadOnlyList<IReadOnlyList<PartyRole>>? HatchTargets { get; set; }
    public IReadOnlyList<IReadOnlyList<PartyRole>>? TwisterTargets { get; set; }

    // A role that must be among that Generate's targets.
    public PartyRole?[] HatchIncludes { get; } = new PartyRole?[UcobP4AddsState.Generates];
    public PartyRole?[] RavenDiveTargets { get; } = new PartyRole?[UcobP4AddsState.Quotes];
    public PartyRole?[] BeamTargets { get; } = new PartyRole?[UcobP4AddsState.Quotes];
}
