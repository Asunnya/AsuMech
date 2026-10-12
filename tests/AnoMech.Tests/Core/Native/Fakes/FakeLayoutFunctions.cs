using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeLayoutFunctions : ILayoutFunctions
{
    public int SuppressLayer(ushort layerKey) => 0;
    public void ReassertSuppressedLayers() { }
    public void ClearSuppressedLayers() { }

    public string DescribeActiveLayers() => "fake";
    public string DescribeLiveEffects() => "fake";
}
