using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// Twintania's twisters: once armed, the first player to touch one sets it off.
public sealed class UcobTwisters
{
    public const float Arms = 0.9f;
    public const float Fades = 5.56f;
    private const float Gone = 6.62f;

    private sealed class Twister(Vector2 at, SimEventObject? eobj, float armsAt, float fadesAt)
    {
        public Vector2 At { get; } = at;
        public SimEventObject? EObj { get; } = eobj;
        public float ArmsAt { get; } = armsAt;
        public float FadesAt { get; } = fadesAt;
        public bool Spent { get; set; }
    }

    private readonly List<Twister> twisters = [];
    private readonly List<SimEventObject> eobjs = [];

    public void Drop(SimWorld world, Vector2 at)
    {
        var now = world.Events.Elapsed;
        var eobj = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.Twister,
            Placement = new Placement(new Vector3(at.X, 0f, at.Y), 0f),
            TimelineState = 1,
        });
        if (eobj != null) eobjs.Add(eobj);
        var twister = new Twister(at, eobj, now + Arms, now + Fades);
        twisters.Add(twister);
        world.Events.Add(Fades, () => eobj?.FadeOut());
        world.Events.Add(Gone, () =>
        {
            eobj?.SetState(0);
            twisters.Remove(twister);
        });
    }

    // `blast` gets the twister's spot and who touched it.
    public void Tick(SimWorld world, Action<Vector2, SimCharacter> blast)
    {
        var now = world.Events.Elapsed;
        foreach (var twister in twisters.Where(t => !t.Spent && now >= t.ArmsAt && now < t.FadesAt).ToList())
        {
            SimCharacter? toucher = null;
            for (var slot = 0; slot < 8 && toucher == null; slot++)
                if (world.Party.Get(slot) is { } member && member.IsAlive()
                    && Vector2.Distance(new Vector2(member.Position.X, member.Position.Z), twister.At) <= Geometry.TwisterTriggerRadius)
                    toucher = member;
            if (toucher == null) continue;
            twister.Spent = true;
            twister.EObj?.SetState(0);
            blast(twister.At, toucher);
        }
    }

    public void Clear()
    {
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        twisters.Clear();
    }
}
