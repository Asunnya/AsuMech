namespace AnoMech.Scenarios.Fru.P2DiamondDust;

// User-forced rolls for FruP2DiamondDustState; null leaves a field randomized.
public sealed class FruP2DiamondDustStateOverrides
{
    // Axe Kick (out) or Scythe Kick (in).
    public bool? Axe { get; set; }
    public bool? Stillness { get; set; }
    // Octants clockwise from north; the first icicles drop on FirstIcicle and FirstIcicle + 4.
    public int? FirstIcicle { get; set; }
    public bool? SupportsMarked { get; set; }
    public int? MirrorOctant { get; set; }
    public int? GazeOctant { get; set; }
}
