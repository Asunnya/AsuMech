using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;

namespace AnoMech.Core.SimObjects;

// The fork's scenarios (DSR, M9S, UWU) still draw some telegraphs as client-side VFX; peers don't see them.
public sealed partial class SimWorld
{
    // Restores map-effect `index` with `offFlags` on teardown.
    public void ResetMapEffectOnDespawn(byte index, uint offFlags)
        => children.GetOrAdd(() => new SimMapEffectReset(Map)).Record(index, offFlags);

    // `placement` is scenario-local; scale.X = halfWidth, scale.Z = length for rect omens.
    public SimOmen SpawnOmen(string path, Placement placement, Vector3 scale, float durationSeconds)
        => children.Add(new SimOmen(Coordinates, path, placement, scale, durationSeconds));

    // A telegraph from `actionId`'s own Omen sheet entry, for an ability whose real telegraph is
    // duty-scripted rather than driven by a cast.
    public void SpawnActionOmen(uint actionId, Vector3 origin, float rotation, float durationSeconds)
        => children.Add(new SimOmen(Coordinates, actionId, origin, rotation, durationSeconds));
}
