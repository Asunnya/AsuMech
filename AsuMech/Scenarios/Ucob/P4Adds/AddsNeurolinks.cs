using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob.P4Adds;

// Twintania drops them in P1 and they stay for the rest of the fight; inside one, a Hatch is
// survivable.
public sealed class AddsNeurolinks(SimWorld world)
{
    public const float Radius = 2f;

    // Where the three always land: at waymarks 1, 2 and 3.
    public static readonly Vector2 North = new(0.1f, -8.5f);
    public static readonly Vector2 West = new(-7.7f, 5.9f);
    public static readonly Vector2 East = new(9.3f, 5.5f);

    private readonly List<Vector2> spots = [];
    private readonly List<SimEventObject> eobjs = [];

    public void Drop(Vector2 at)
    {
        spots.Add(at);
        if (UcobSim.SpawnEObj(world, EObjId.Neurolink, at) is { } eobj) eobjs.Add(eobj);
    }

    public bool Contains(SimCharacter member) => spots.Any(n => Vector2.Distance(UcobSim.Flat(member.Position), n) <= Radius);

    public void Tick()
    {
        foreach (var member in UcobSim.AliveMembers(world.Party).ToList())
        {
            var inside = Contains(member);
            if (inside && !member.HasStatus(StatusId.Neurolink)) member.AddStatus(StatusId.Neurolink);
            else if (!inside && member.HasStatus(StatusId.Neurolink)) member.RemoveStatus(StatusId.Neurolink);
        }
    }

    public void Clear()
    {
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        spots.Clear();
    }
}
