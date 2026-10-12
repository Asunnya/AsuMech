using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeWaymarks : IWaymarks
{
    public bool Place(IReadOnlyList<(WaymarkSlot Slot, Vector3 Position)> markers) => true;
    public void ClearAll() { }
}
