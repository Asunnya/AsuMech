using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Core.Native.Interfaces;

// World VFX placed client-side by the fork's scenarios; peers don't see them.
public interface IVfxFunctions
{
    // Placement is world space. Null when the path doesn't exist or the spawn failed: a bad path
    // crashes on the file thread, so it is checked first.
    IStaticVfxProxy? SpawnStatic(string path, Placement placement, Vector3 scale);
}
