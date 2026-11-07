using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// Twintania's neurolinks: whoever stands in one carries the Neurolink status.
public sealed class UcobNeurolinks
{
    private readonly List<Vector2> spots = [];
    private readonly List<SimEventObject> eobjs = [];

    public IReadOnlyList<Vector2> Spots => spots;

    public void Drop(SimWorld world, Vector2 at)
    {
        spots.Add(at);
        var eobj = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.Neurolink,
            Placement = new Placement(new Vector3(at.X, 0f, at.Y), 0f),
            TimelineState = 1,
        });
        if (eobj != null) eobjs.Add(eobj);
    }

    public bool Contains(SimCharacter member)
        => spots.Any(n => Vector2.Distance(new Vector2(member.Position.X, member.Position.Z), n) <= Geometry.NeurolinkRadius);

    public void Tick(SimParty party)
    {
        for (var slot = 0; slot < 8; slot++)
        {
            if (party.Get(slot) is not { } member || !member.IsAlive()) continue;
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
