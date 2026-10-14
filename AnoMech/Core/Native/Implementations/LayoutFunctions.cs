using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.Native.Implementations;

internal sealed class LayoutFunctions : ILayoutFunctions
{
    public string DescribeActiveLayers() => LayoutQuery.DescribeActiveLayers();

    public string DescribeLiveEffects() => LayoutQuery.DescribeLiveEffects();
}
