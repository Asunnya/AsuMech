namespace AsuMech.Scenarios.Fru.P5ParadiseLost;

public sealed class FruP5ParadiseLostState(FruP5ParadiseLostStateOverrides overrides)
{
    public bool PlayOutEnrage { get; } = overrides.PlayOutEnrage;
}
