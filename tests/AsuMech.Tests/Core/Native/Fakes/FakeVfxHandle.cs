using System.Numerics;
using AsuMech.Core.Native.Interfaces;

namespace AsuMech.Tests;

internal sealed class FakeVfxHandle : IStaticVfxProxy
{
    public void SetScale(Vector3 scale) { }
    public void Remove() { }
}
