using System.Numerics;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

internal sealed class FakeVfxHandle : IStaticVfxProxy
{
    public void SetScale(Vector3 scale) { }
    public void Remove() { }
}
