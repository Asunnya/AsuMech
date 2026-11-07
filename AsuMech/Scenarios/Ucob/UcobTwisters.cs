using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// Twintania's twisters: once armed, the first player to touch one sets it off. Built with a world,
// a touch casts Twister's blast from Twintania; without, the caller resolves it.
public sealed class UcobTwisters
{
    public const float TriggerRadius = 1.25f;
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
        public CircleObstacle? Obstacle { get; set; }
    }

    // Bots steer this far around a twister; null leaves them walking straight.
    public float? BotsAvoidRadius { get; init; }

    private readonly SimWorld? world;
    private readonly List<Twister> twisters = [];
    private readonly List<SimEventObject> eobjs = [];
    private readonly List<(SimWorld World, CircleObstacle Obstacle)> obstacles = [];
    private SimEnemy? owner;

    public UcobTwisters() { }

    public UcobTwisters(SimWorld world) => this.world = world;

    public void Drop(SimEnemy twintania, IEnumerable<Vector2> spots)
    {
        owner = twintania;
        foreach (var at in spots) Drop(world!, at);
    }

    public void Drop(SimWorld on, Vector2 at)
    {
        var now = on.Events.Elapsed;
        var eobj = on.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.Twister,
            Placement = new Placement(new Vector3(at.X, 0f, at.Y), 0f),
            TimelineState = 1,
        });
        if (eobj != null) eobjs.Add(eobj);
        var twister = new Twister(at, eobj, now + Arms, now + Fades);
        twisters.Add(twister);
        if (BotsAvoidRadius is { } radius)
        {
            twister.Obstacle = on.Obstacles.Add(new CircleObstacle(at, radius));
            obstacles.Add((on, twister.Obstacle));
        }
        on.Events.Add(Fades, () =>
        {
            eobj?.FadeOut();
            RemoveObstacle(on, twister);
        });
        on.Events.Add(Gone, () =>
        {
            eobj?.Despawn();
            twisters.Remove(twister);
        });
    }

    public void Tick() => Tick(world!, (at, _) => owner?.CastFromHelper(UcobActions.TwisterBlast, at: new Placement(new Vector3(at.X, 0f, at.Y), 0f)));

    // `blast` gets the twister's spot and who touched it.
    public void Tick(SimWorld on, Action<Vector2, SimCharacter> blast)
    {
        var now = on.Events.Elapsed;
        foreach (var twister in twisters.Where(t => !t.Spent && now >= t.ArmsAt && now < t.FadesAt).ToList())
        {
            SimCharacter? toucher = null;
            for (var slot = 0; slot < 8 && toucher == null; slot++)
                if (on.Party.Get(slot) is { } member && member.IsAlive()
                    && Vector2.Distance(new Vector2(member.Position.X, member.Position.Z), twister.At) <= Geometry.TwisterTriggerRadius)
                    toucher = member;
            if (toucher == null) continue;
            twister.Spent = true;
            twister.EObj?.Despawn();
            RemoveObstacle(on, twister);
            blast(twister.At, toucher);
        }
    }

    private void RemoveObstacle(SimWorld on, Twister twister)
    {
        if (twister.Obstacle is not { } obstacle) return;
        on.Obstacles.Remove(obstacle);
        obstacles.Remove((on, obstacle));
        twister.Obstacle = null;
    }

    public void Clear()
    {
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        foreach (var (world, obstacle) in obstacles) world.Obstacles.Remove(obstacle);
        obstacles.Clear();
        twisters.Clear();
    }
}
