using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Tests;

// Solo in the inn the real gauge is empty.
internal sealed class FakeLimitBreakController : ILimitBreakController
{
    public LimitBreakBars? Read() => default(LimitBreakBars);
}
