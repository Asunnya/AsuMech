using AsuMech.Core.SimObjects;

namespace AsuMech.Core.Native.Interfaces;

// Mirrors the sim party into the _PartyList addon.
public interface IPartyHud
{
    void Refresh(SimParty party);
    void Clear();
}
