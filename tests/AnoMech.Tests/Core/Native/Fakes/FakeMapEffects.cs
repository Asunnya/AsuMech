using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeMapEffects : IMapEffects
{
    public bool Loaded { get; set; }

    public bool Apply(uint packetFlags, byte index) => Loaded;
    public bool ApplySnapshot(ReadOnlySpan<ushort> states, bool zoneIn) => Loaded;
    public void LogAllSlots(string label) { }
}
