using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeMapEffects(SimTimeline timeline) : IMapEffects
{
    public bool Loaded { get; set; }

    public bool Apply(uint packetFlags, byte index)
    {
        if (Loaded)
            timeline.Add("map_effect", id: index.ToString(), name: "MapEffect",
                detail: $"state={packetFlags & 0xFFFF} timeline={packetFlags >> 16} flags=0x{packetFlags:X}");
        return Loaded;
    }

    public bool ApplySnapshot(ReadOnlySpan<ushort> states, bool zoneIn)
    {
        if (Loaded)
            timeline.Add("map_effect_snapshot", id: zoneIn ? "5" : "1", name: "MapEffectSnapshot",
                detail: $"states={string.Join(",", states.ToArray())}");
        return Loaded;
    }
    public void LogAllSlots(string label) { }

    public void Dispose() { }
}
