namespace AnoMech.Scenarios.Uwu.PrimalRoulette;

public class PrimalRouletteStateOverrides
{
    // Index into PrimalRouletteState.Orders; null rolls one.
    public int? Order { get; set; }
    public bool ViscousOnPlayer { get; set; }
}
