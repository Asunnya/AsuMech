using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;

namespace AsuMech.Core.Native.Interfaces;

public interface IEventObjects
{
    // Placement is world space and overrides config.Placement. Null when the 40-slot pool is full.
    IEventObjectProxy? Spawn(EventObjectSpawnConfig config, Placement placement);
}
