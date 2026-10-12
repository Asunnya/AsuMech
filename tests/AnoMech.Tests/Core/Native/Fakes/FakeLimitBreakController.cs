using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

// Solo in the inn the real gauge is empty.
internal sealed class FakeLimitBreakController : ILimitBreakController
{
    public LimitBreakBars? Read() => default(LimitBreakBars);
}
