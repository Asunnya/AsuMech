using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeLayoutFunctions : ILayoutFunctions
{
    // Calls that find nothing yet, as while the zone is still streaming in.
    public int ColliderCallsBeforeStreamed { get; set; }
    public List<Vector3> ColliderDropCenters { get; } = [];

    public int DisableSpawnAreaColliders(Vector3 center, float radius)
    {
        ColliderDropCenters.Add(center);
        return ColliderDropCenters.Count > ColliderCallsBeforeStreamed ? 1 : 0;
    }

    public void PlaySharedGroupTimeline(string sgbPath, uint index) { }

    public string DescribeActiveLayers() => "fake";
    public string DescribeLiveEffects() => "fake";
}
