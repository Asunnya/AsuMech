namespace AnoMech.Core.Native.Interfaces;

// The client's RSV table: resolves the placeholder action/status names newer content ships with,
// which the server only sends inside the duty.
public interface IRsvFunctions
{
    // record = one raw RSV packet payload: [u32 valueLength][0x30-byte key][value].
    void Add(byte[] record);
}
