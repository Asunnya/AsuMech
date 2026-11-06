using AsuMech.Core.Native.Interfaces;
using AsuMech.Core.SimObjects;

namespace AsuMech.Tests;

internal sealed class FakePartyHud : IPartyHud
{
    public void Refresh(SimParty party) { }
    public void Clear() { }
}
