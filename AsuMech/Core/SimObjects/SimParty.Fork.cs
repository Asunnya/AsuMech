using System.Numerics;

namespace AsuMech.Core.SimObjects;

public sealed partial class SimParty
{
    public void Knockback(Vector3 source, float distance)
        => ForEachActive(m => m.Knockback(source, distance));
}
