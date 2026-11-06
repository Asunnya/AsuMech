using System.Numerics;
using AsuMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace AsuMech.Core.Native.Implementations;

internal sealed unsafe class StaticVfxProxy(VfxObject* vfx) : IStaticVfxProxy
{
    // Flag 0x2 marks it dirty, as at spawn.
    public void SetScale(Vector3 scale)
    {
        vfx->Scale = scale;
        vfx->Flags |= 0x2;
    }

    public void Remove() => vfx->CleanupRender();
}
