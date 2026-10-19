namespace AnoMech.Scenarios.M10s.SickestTakeoff;

// Pins the random rolls; null keeps the roll.
public sealed class M10sSickestTakeoffStateOverrides
{
    // Wall the wave comes from: 0 north, 1 east, 2 south, 3 west.
    public int? WaveFrom { get; set; }
    // Deep Blue's lane across the wave: -1 right, 0 middle, 1 left, facing Deep Blue.
    public int? Lane { get; set; }
    public bool? EightOrbs { get; set; }
    public bool? DoubleDip { get; set; }
}
