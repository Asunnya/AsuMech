using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;

namespace AnoMech.Scenarios.Umad.P2Forsaken;

// User-controlled overrides for UmadP2ForsakenState's randomized fields. Bound
// by the scenario's settings UI; null/default values leave the field randomized
// at scenario start. The state ctor consumes this directly.
// See TopP5DeltaStateOverrides for the canonical shape.
public sealed class UmadP2ForsakenStateOverrides
{
    public EndAttack?[] EndAttacks { get; } = new EndAttack?[4]; // null = Auto/randomize; [0] has debug-only UI
    public Direction? NewNorth { get; set; }           // null = random direction (debug-only UI)
    public uint? SupportLockon { get; set; }           // null = random; else LockonId.ForsakenChariot / .ForsakenCone
    public PartyRole? SupportStackRole { get; set; }   // null = random support gets Stack
    public PartyRole? DpsStackRole { get; set; }       // null = random dps gets Stack
    public int? Rotation { get; set; }                 // null = random; else +1 / -1 eighths per tower
    // [round * 2] the tower toward GetTowers' first direction, [round * 2 + 1] the second; null = from
    // the round's four shuffled; else that tower's two new lockons, dealt to its soakers in PartyRole order.
    public uint[]?[] TowerLockons { get; } = new uint[]?[UmadP2ForsakenState.TowerRounds * 2];
}
