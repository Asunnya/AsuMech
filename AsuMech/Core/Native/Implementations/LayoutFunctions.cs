using AsuMech.Core.Native.Implementations.Interop;
using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Core.Native.Implementations;

internal sealed class LayoutFunctions : ILayoutFunctions
{
    public string DescribeActiveLayers() => LayoutQuery.DescribeActiveLayers();

    public string DescribeLiveEffects() => LayoutQuery.DescribeLiveEffects();
}
