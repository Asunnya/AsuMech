using AsuMech.Core.Native.Implementations.Pointers;
using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Core.Native.Implementations;

internal sealed unsafe class RsfFunctions : IRsfFunctions
{
    private const int RecordLength = 0x48;

    // Best-effort: a missed signature or short record is skipped, never breaks a scenario run.
    public void Add(byte[] record)
    {
        var fn = ResolvedStringPointers.RsfReceive;
        if (fn == null || record == null || record.Length < RecordLength) return;
        fixed (byte* p = record)
            fn(p);
    }
}
