using System.Numerics;

namespace AsuMech.Core.Game.Geometry;

// The walkable floor of an arena in scenario-local XZ coordinates; Y is ignored.
public interface IArenaShape
{
    bool IsOutside(Vector3 local);
}
