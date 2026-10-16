using System.Numerics;
using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.Native.Implementations;

internal sealed unsafe class LayoutFunctions : ILayoutFunctions
{
    public int DisableSpawnAreaColliders(Vector3 center, float radius) => DirectorFunctions.DisableSpawnAreaColliders(center, radius);

    public void PlaySharedGroupTimeline(string sgbPath, uint index)
    {
        foreach (var p in LayoutQuery.FindAllBySgbPath(sgbPath))
        {
            var sg = (FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group.SharedGroupLayoutInstance*)p;
            if (sg->IsTimelineIndexValid(index)) sg->PlayTimeline(index, 0);
        }
    }

    public string DescribeActiveLayers() => LayoutQuery.DescribeActiveLayers();

    public string DescribeLiveEffects() => LayoutQuery.DescribeLiveEffects();
}
