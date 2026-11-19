using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Umad.P3DecisiveBattle;

// Pins the random rolls; null keeps the roll. A forced role only binds within its own role group.
public sealed class UmadP3DecisiveBattleStateOverrides
{
    public bool? FireShort { get; set; }
    public PartyRole? EntropyOn { get; set; }
    public PartyRole? DynamicFluidOn { get; set; }
    public bool? Latitudinal { get; set; }
    public int? WindCrystalSpot { get; set; }
    public bool? FireClockwiseOfWind { get; set; }
}
