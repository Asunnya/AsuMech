namespace AsuMech.Scenarios.Fru.P1UtopianSky;

// User-forced rolls for FruP1UtopianSkyState; null leaves a field randomized.
public sealed class FruP1UtopianSkyStateOverrides
{
    public bool? CyclonicFire { get; set; }
    public bool? UtopianFire { get; set; }
    public int? SafeAxis { get; set; }
    public bool? FireOnTanks { get; set; }
}
