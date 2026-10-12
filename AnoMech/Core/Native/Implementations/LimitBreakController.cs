using AnoMech.Core.Native.Interfaces;
using NativeLimitBreakController = FFXIVClientStructs.FFXIV.Client.Game.UI.LimitBreakController;

namespace AnoMech.Core.Native.Implementations;

internal sealed unsafe class LimitBreakController : ILimitBreakController
{
    public LimitBreakBars? Read()
    {
        var lb = NativeLimitBreakController.Instance();
        return lb == null ? null : new LimitBreakBars(lb->BarCount, lb->CurrentUnits, lb->BarUnits);
    }
}
