using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Tests;

internal sealed class FakeVfxFunctions : IVfxFunctions
{
    public IStaticVfxProxy? SpawnStatic(string path, Placement placement, Vector3 scale) => new FakeVfxHandle();
}
