using AnoMech.Core.Native.Implementations.Pointers;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;

namespace AnoMech.Core.Native.Implementations;

internal sealed unsafe class RsvFunctions : IRsvFunctions
{
    private const int HeaderLength = 4 + 0x30;

    // RsvReceive dereferences the LayoutWorld singleton, so it must be up. Best-effort like RSF.
    public void Add(byte[] record)
    {
        var fn = ResolvedStringPointers.RsvReceive;
        if (fn == null || record == null || record.Length < HeaderLength || LayoutWorld.Instance() == null) return;
        fixed (byte* p = record)
            fn(p);
    }
}
