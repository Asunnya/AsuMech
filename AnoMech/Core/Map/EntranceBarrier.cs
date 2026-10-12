using System.Numerics;

namespace AnoMech.Core.Map;

// A duty's spawn wall: a layout SharedGroup (wall VFX, sound and collision) bound to an EObj
// placement. The server spawns that EObj at zone-in and opens the wall at commence with
// DirectorEObjMod. Position is scenario-local. Values can be read from replay file
public readonly record struct EntranceBarrier(uint EObjId, uint EObjLayoutId, uint WallLayoutId, Vector3 Position);
