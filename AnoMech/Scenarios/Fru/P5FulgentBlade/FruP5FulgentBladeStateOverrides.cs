namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// User-forced rolls for FruP5FulgentBladeState; null leaves a field randomized.
public sealed class FruP5FulgentBladeStateOverrides
{
    // 0 = south, 1 = north, 2 = east, 3 = west pocket.
    public int? PositionIndex { get; set; }
    public int? RotationIndex { get; set; }
    public bool? EastFirst { get; set; }
}
