using System.Numerics;

namespace AnoMech.Core.Map;

// A duty's spawn wall: a layout SharedGroup (wall VFX, sound and collision) bound to an EObj
// placement. The server spawns that EObj at zone-in and opens the wall at commence with
// DirectorEObjMod. Position is scenario-local. Values can be read from replay file
public readonly record struct EntranceBarrier(uint EObjId, uint EObjLayoutId, uint WallLayoutId, Vector3 Position)
{
    // For a zone whose wall EObj was never captured: the player spawns at (0,0,16) and the spawn
    // wall's colliders are dropped instead of opening it.
    public static EntranceBarrier Unknown => new(0, 0, 0, new Vector3(0f, 0f, 16f));

    public bool IsKnown => EObjId != 0;
}
