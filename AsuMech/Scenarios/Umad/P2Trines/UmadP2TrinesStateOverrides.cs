namespace AsuMech.Scenarios.Umad.P2Trines;

// Pins the random rolls; null keeps the roll.
public sealed class UmadP2TrinesStateOverrides
{
    public bool? CleaveWest { get; set; }
    // Indexes into UmadP2TrinesScenario.Trines (1-6; the middle one always lands last).
    public int[]? FirstSet { get; set; }
}
