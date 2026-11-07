using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P3GrandOctet;

// Pins the random rolls; null keeps the roll.
public sealed class UcobP3GrandOctetStateOverrides
{
    // 0 = north, clockwise in steps of 45°.
    public int? BahamutSlot { get; set; }
    public PartyRole? TwintaniaTarget { get; set; }
    // A role that must be among the Megaflare stacks.
    public PartyRole? StackInclude { get; set; }

    // Bahamut, Nael, Twintania, then the dragons in UcobActions.Cauterize order.
    public IReadOnlyList<int>? Slots { get; set; }
    // Nael, the five dragons in dive order, Bahamut, Twintania.
    public IReadOnlyList<PartyRole>? Markers { get; set; }
    public IReadOnlyList<PartyRole>? StackTargets { get; set; }
    public IReadOnlyList<PartyRole>? TwisterTargets { get; set; }
}
