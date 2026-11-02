using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Ucob.P1Twintania;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP1TwintaniaStateOverrides
{
    public IReadOnlyList<IReadOnlyList<PartyRole>>? TwisterTargets { get; set; }
    public IReadOnlyList<IReadOnlyList<PartyRole>>? HatchTargets { get; set; }
    public PartyRole? LiquidHellTarget { get; set; }

    public PartyRole?[] FireballTargets { get; } = new PartyRole?[2];
    // A role that must be among that Generate's targets.
    public PartyRole?[] HatchIncludes { get; } = new PartyRole?[UcobP1TwintaniaState.HatchCounts.Length];
}
