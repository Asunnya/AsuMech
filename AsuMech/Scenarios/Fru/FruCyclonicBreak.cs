using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru;

// Fatebreaker's Cyclonic Break, from the cones' bait on: a cone at every player, the same cones
// again, then twice more, each set turned one cone's width clockwise. The element lands with the
// second set: Sinsmite spreads on everyone, or Sinsmoke pairs on the DPS.
public static class FruCyclonicBreak
{
    public const float RepeatAfterBait = 2f;
    public const float ElementAfterBait = 2.14f;
    public const float FirstTurnAfterBait = 4.07f;
    public const float SecondTurnAfterBait = 6.14f;

    private const float ConeWidth = MathF.PI / 8f;

    public static void Bait(SimWorld world, SimEnemy caster, bool fire)
    {
        var origin = caster.Position;
        var rotations = world.Party.ActiveMembers().Where(m => m.IsAlive())
            .Select(m => new Placement(origin, 0f).Face(m.Position).Rotation).ToList();
        foreach (var rotation in rotations)
            caster.CastFromHelper(Actions.CyclonicBreakBait, at: new Placement(origin, rotation));
        world.Events.Add(RepeatAfterBait, () => Repeat(caster, origin, rotations, 0f));
        world.Events.Add(ElementAfterBait, () => Element(world, caster, fire));
        world.Events.Add(FirstTurnAfterBait, () => Repeat(caster, origin, rotations, -ConeWidth));
        world.Events.Add(SecondTurnAfterBait, () => Repeat(caster, origin, rotations, -2f * ConeWidth));
    }

    private static void Repeat(SimEnemy caster, System.Numerics.Vector3 origin, List<float> rotations, float turn)
    {
        foreach (var rotation in rotations)
            caster.CastFromHelper(Actions.CyclonicBreakRepeat, at: new Placement(origin, rotation + turn));
    }

    // UNVERIFIED: retail moves a dead DPS's Sinsmoke onto a random support; here it is skipped.
    private static void Element(SimWorld world, SimEnemy caster, bool fire)
    {
        foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
        {
            if (!fire) caster.CastFromHelper(Actions.CyclonicSinsmite, member, at: new Placement(member.Position, 0f));
            else if (member is ISimPartyMember { Role: >= PartyRole.MeleeDpsA })
                caster.CastFromHelper(Actions.CyclonicSinsmoke, member, at: new Placement(member.Position, 0f));
        }
    }
}
