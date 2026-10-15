namespace AnoMech.Scenarios.Fru.P2DiamondDust;

// User-forced rolls for FruP2DiamondDustState; null leaves a field randomized.
public sealed class FruP2DiamondDustStateOverrides
{
    // Axe Kick (out) or Scythe Kick (in).
    public bool? Axe { get; set; }
    public bool? Stillness { get; set; }
}
