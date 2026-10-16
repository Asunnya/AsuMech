using System.Numerics;

namespace AnoMech.Core.Native.Interfaces;

// The zone's LGB layout instances.
public interface ILayoutFunctions
{
    // Drops the colliders of SharedGroups near the world-space center; returns how many it found
    // (0 while the zone is still streaming in).
    int DisableSpawnAreaColliders(Vector3 center, float radius);

    // Every zone SharedGroup loaded from that SGB plays the timeline, where it has one at that index.
    void PlaySharedGroupTimeline(string sgbPath, uint index);

    string DescribeActiveLayers();
    string DescribeLiveEffects();
}
