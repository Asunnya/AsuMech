using System;
using System.Numerics;

namespace AnoMech.Core.Map;

// A duty's spawn wall: a layout SharedGroup (wall VFX, sound and collision) bound to an EObj
// placement. The server spawns that EObj at zone-in and opens the wall at commence with
// DirectorEObjMod. Position is global, as the server sends it; the player spawns there.
public readonly record struct EntranceBarrier(uint EObjId, uint EObjLayoutId, uint WallLayoutId, Vector3 Position, float Rotation)
{
    // For a zone whose wall EObj was never captured: the player spawns 16y south of `origin` and
    // the spawn wall's colliders are dropped instead of opening it.
    public static EntranceBarrier Unknown(Vector3 origin) => new(0, 0, 0, origin + new Vector3(0f, 0f, 16f), MathF.PI);

    public bool IsKnown => EObjId != 0;
}
