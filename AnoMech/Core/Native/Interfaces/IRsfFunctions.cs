namespace AnoMech.Core.Native.Interfaces;

// The client's RSF table: resolves obfuscated file paths of duty content, which the server only
// sends inside the duty.
public interface IRsfFunctions
{
    // record = one raw 0x48-byte RSF packet payload.
    void Add(byte[] record);
}
