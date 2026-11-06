using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Tests;

internal sealed class FakeActionTimelinePreload : IActionTimelinePreload
{
    public void Preload(IEnumerable<(ushort Id, string Key)> rows, string tag) { }
}
