namespace AnoMech.Scenarios.M10s.InsaneAir;

public sealed class M10sInsaneAirState
{
    public const int Rounds = 4;

    public InsaneAirVariant Variant { get; }

    public M10sInsaneAirState(Rng rng, M10sInsaneAirStateOverrides? overrides = null)
    {
        overrides ??= new M10sInsaneAirStateOverrides();
        var variant = rng.Next(InsaneAirVariant.All.Length);
        Variant = InsaneAirVariant.All[overrides.Variant is { } v && v >= 0 && v < InsaneAirVariant.All.Length ? v : variant];
    }
}
