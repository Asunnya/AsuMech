namespace AnoMech.Scenarios.M10s.ArenaSplit;

// Pins the random rolls; null keeps the roll.
public sealed class M10sArenaSplitStateOverrides
{
    public bool? DeepBlueWest { get; set; }
    public bool? DoubleDip { get; set; }
    // Wall the wave comes from: 0 north, 1 east, 3 west.
    public int? WaveFrom { get; set; }
    public bool? PlayerPyrotation { get; set; }
}
