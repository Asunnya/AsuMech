using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P5PandorasBox;

public sealed class FruP5PandorasBoxState
{
    public PartyRole LimitBreakTank { get; }

    public FruP5PandorasBoxState(Rng rng, FruP5PandorasBoxStateOverrides overrides)
    {
        LimitBreakTank = overrides.LimitBreakTank ?? (rng.NextBool() ? PartyRole.MainTank : PartyRole.OffTank);
    }
}
