using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Tests;

internal sealed class FakeLayoutFunctions : ILayoutFunctions
{
    public string DescribeActiveLayers() => "fake";
    public string DescribeLiveEffects() => "fake";
}
