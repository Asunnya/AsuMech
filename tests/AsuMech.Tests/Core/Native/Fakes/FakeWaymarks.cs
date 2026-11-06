using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Tests;

internal sealed class FakeWaymarks : IWaymarks
{
    public bool Place(IReadOnlyList<(WaymarkSlot Slot, Vector3 Position)> markers) => true;
    public void ClearAll() { }
}
