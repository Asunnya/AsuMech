using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// Neurolinks and Generate's Oviforms; an Oviform bursts on the first player it touches, marked or not.
public sealed class UcobHatches(SimWorld world)
{
    public const float OviformSpeed = 5f;
    // Follow stops at the hitbox edge.
    private const float ContactSlack = 0.3f;
    private const float DespawnAfterBurst = 0.65f;

    private readonly List<Vector2> neurolinks = [];
    private readonly List<SimEventObject> eobjs = [];
    private readonly List<Oviform> oviforms = [];

    private sealed class Oviform(SimEnemy enemy, SimCharacter target)
    {
        public SimEnemy Enemy { get; } = enemy;
        public SimCharacter Target { get; } = target;
        public bool Chasing { get; set; }
        public bool Burst { get; set; }
    }

    public void AddNeurolink(Vector2 at)
    {
        neurolinks.Add(at);
        if (world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.Neurolink,
                Placement = new Placement(new Vector3(at.X, 0f, at.Y), 0f),
                TimelineState = 1,
            }) is { } eobj)
            eobjs.Add(eobj);
    }

    public bool InNeurolink(SimCharacter member)
        => neurolinks.Any(n => Vector2.Distance(Flat(member.Position), n) <= Geometry.NeurolinkRadius);

    // The Oviforms wait on Twintania until Chase.
    public void Generate(SimEnemy twintania, IEnumerable<SimCharacter> targets)
    {
        foreach (var target in targets.Where(t => t.IsAlive()))
        {
            target.AttachLockonVfx(LockonId.Hatch);
            var enemy = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Oviform,
                NameId: BNpcNameId.Oviform,
                Level: Level,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(twintania.Position, twintania.Rotation)));
            if (enemy != null) oviforms.Add(new Oviform(enemy, target));
        }
    }

    public void Chase()
    {
        foreach (var oviform in oviforms.Where(o => !o.Chasing && !o.Burst))
        {
            oviform.Chasing = true;
            oviform.Enemy.Follow(oviform.Target, OviformSpeed);
        }
    }

    public void Tick()
    {
        foreach (var member in AliveMembers())
        {
            var inside = InNeurolink(member);
            if (inside && !member.HasStatus(StatusId.Neurolink)) member.AddStatus(StatusId.Neurolink);
            else if (!inside && member.HasStatus(StatusId.Neurolink)) member.RemoveStatus(StatusId.Neurolink);
        }

        foreach (var oviform in oviforms.Where(o => o.Chasing && !o.Burst).ToList())
        {
            if (!oviform.Target.IsAlive())
            {
                oviform.Burst = true;
                oviform.Enemy.Despawn();
                continue;
            }
            var enemy = oviform.Enemy;
            var toucher = AliveMembers()
                .Where(m => Vector2.Distance(Flat(enemy.Position), Flat(m.Position)) <= enemy.HitboxRadius + m.HitboxRadius + ContactSlack)
                .MinBy(m => Vector2.DistanceSquared(Flat(enemy.Position), Flat(m.Position)));
            if (toucher != null) Burst(oviform, toucher);
        }
    }

    // Whatever is left of an unfinished Generate goes with Twintania.
    public void RecallOviforms()
    {
        foreach (var oviform in oviforms.Where(o => !o.Burst))
        {
            oviform.Burst = true;
            oviform.Enemy.Despawn();
        }
    }

    public void Clear()
    {
        RecallOviforms();
        oviforms.Clear();
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        neurolinks.Clear();
    }

    private void Burst(Oviform oviform, SimCharacter toucher)
    {
        oviform.Burst = true;
        var enemy = oviform.Enemy;
        enemy.Follow(null);
        enemy.Cast(InNeurolink(toucher) ? UcobActions.Hatch : UcobActions.DeepHatch);
        world.Events.Add(DespawnAfterBurst, enemy.Despawn);
    }

    private List<SimCharacter> AliveMembers() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList();

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);
}
