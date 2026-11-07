using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// Twisters dropped under players; once armed, the first touch launches everyone near it.
public sealed class UcobTwisters(SimWorld world)
{
    public const float TriggerRadius = 2f; // UNVERIFIED
    public const float Arms = 0.9f;
    public const float Fades = 5.56f;
    private const float Gone = 6.62f;

    private readonly List<Twister> twisters = [];
    private SimEnemy? owner;

    private sealed class Twister(Vector2 at, SimEventObject? eobj, float armsAt, float fadesAt)
    {
        public Vector2 At { get; } = at;
        public SimEventObject? EObj { get; } = eobj;
        public float ArmsAt { get; } = armsAt;
        public float FadesAt { get; } = fadesAt;
        public bool Spent { get; set; }
    }

    public void Drop(SimEnemy twintania, IEnumerable<Vector2> spots)
    {
        var now = world.Events.Elapsed;
        foreach (var at in spots)
        {
            var eobj = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.Twister,
                Placement = new Placement(new Vector3(at.X, 0f, at.Y), 0f),
                TimelineState = 1,
            });
            var twister = new Twister(at, eobj, now + Arms, now + Fades);
            twisters.Add(twister);
            world.Events.Add(Fades, () => eobj?.FadeOut());
            world.Events.Add(Gone, () =>
            {
                eobj?.Despawn();
                twisters.Remove(twister);
            });
        }
        owner = twintania;
    }

    public void Tick()
    {
        var now = world.Events.Elapsed;
        foreach (var twister in twisters.Where(t => !t.Spent && now >= t.ArmsAt && now < t.FadesAt).ToList())
        {
            if (!world.Party.ActiveMembers().Any(m => m.IsAlive() && Vector2.Distance(new Vector2(m.Position.X, m.Position.Z), twister.At) <= TriggerRadius)) continue;
            twister.Spent = true;
            twister.EObj?.SetState(0);
            owner?.CastFromHelper(UcobActions.TwisterBlast, at: new Placement(new Vector3(twister.At.X, 0f, twister.At.Y), 0f));
        }
    }

    public void Clear()
    {
        foreach (var twister in twisters) twister.EObj?.Despawn();
        twisters.Clear();
    }
}
