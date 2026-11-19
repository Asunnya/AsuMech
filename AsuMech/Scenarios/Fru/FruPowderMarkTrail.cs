using System.Linq;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru;

// Powder Mark Trail goes off as it runs out or its holder dies: a Burn Mark on the holder and
// another on the player nearest them.
public static class FruPowderMarkTrail
{
    public static void Detonate(SimWorld world, SimEnemy? caster, StatusLost lost)
    {
        if (lost.StatusId != StatusId.PowderMarkTrail || caster is null) return;
        var holder = lost.Holder;
        caster.CastFromHelper(Actions.BurnMark, at: new Placement(holder.Position, 0f));
        var nearest = world.Party.ActiveMembers()
            .Where(m => m.IsAlive() && !ReferenceEquals(m, holder))
            .MinBy(m => System.Numerics.Vector3.DistanceSquared(m.Position, holder.Position));
        if (nearest is not null) caster.CastFromHelper(Actions.BurnMark, at: new Placement(nearest.Position, 0f));
    }
}
