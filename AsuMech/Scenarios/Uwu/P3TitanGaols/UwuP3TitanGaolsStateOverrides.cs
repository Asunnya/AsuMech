using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Uwu.P3TitanGaols;

public sealed class UwuP3TitanGaolsStateOverrides
{
    public bool PlayerAlwaysInFirstGaols { get; set; }
    public Direction? FirstJump { get; set; }

    // Headless runs pin the rest of the roll. Internal, so settings sync and the summary skip them.
    internal int? SafeSide { get; set; }
    internal IReadOnlyList<PartyRole>? GaolTargets { get; set; }
    internal PartyRole? FirstLandslideBait { get; set; }
    internal PartyRole? SecondLandslideBait { get; set; }
}
