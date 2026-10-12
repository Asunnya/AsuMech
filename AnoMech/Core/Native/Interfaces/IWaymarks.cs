using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Core.Native.Interfaces;

// Field markers, written client-side only.
public interface IWaymarks
{
    // World space; replaces the whole set, so slots not listed are cleared. False when the
    // MarkingController isn't available.
    bool Place(IReadOnlyList<(WaymarkSlot Slot, Vector3 Position)> markers);
    void ClearAll();
}
