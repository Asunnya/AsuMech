using System.Collections.Generic;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P5GoldenBahamut;

public sealed class UcobP5GoldenBahamutStateOverrides
{
    // The tank who invulns the second Akh Morn; the other invulns the third.
    public PartyRole FirstInvulnTank { get; set; } = PartyRole.MainTank;

    // Per Exaflare set, where its lanes roll to; null entries stay random.
    public Direction?[] ExaflareDirections { get; } = new Direction?[UcobP5GoldenBahamutState.ExaflareSets];

    internal IReadOnlyList<IReadOnlyList<float>>? ExaflareLaneOrders { get; set; }
    internal IReadOnlyList<PartyRole>? MornAfahTargets { get; set; }
}
