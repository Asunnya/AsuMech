using System;

namespace AnoMech.Core.Native.Interfaces;

// The zone's MapEffect slots (the server's type-257 packets). Apply returns false while the
// slot's SharedGroup hasn't streamed in yet; the caller retries.
public interface IMapEffects : IDisposable
{
    // While false, Apply refuses: nothing outside a sim may reach the native path.
    bool Loaded { get; set; }

    bool Apply(uint packetFlags, byte index);

    // Every slot's resting state at once, like the server's zone-in and reset snapshot; plays no
    // timeline. zoneIn: retail's zone-in header rather than the reset one.
    bool ApplySnapshot(ReadOnlySpan<ushort> states, bool zoneIn);

    void LogAllSlots(string label);
}
