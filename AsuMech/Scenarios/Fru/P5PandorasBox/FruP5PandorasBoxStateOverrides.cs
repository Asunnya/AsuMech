using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P5PandorasBox;

// User-forced rolls for FruP5PandorasBoxState; null leaves a field randomized.
public sealed class FruP5PandorasBoxStateOverrides
{
    // MainTank or OffTank.
    public PartyRole? LimitBreakTank { get; set; }
}
