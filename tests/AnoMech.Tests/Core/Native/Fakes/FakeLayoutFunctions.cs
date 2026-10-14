using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeLayoutFunctions : ILayoutFunctions
{
    public string DescribeActiveLayers() => "fake";
    public string DescribeLiveEffects() => "fake";
}
