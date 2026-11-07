using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob.P4Adds;

// Twintania's Generate: an Oviform chases each marked player and hatches on contact. Inside a
// Neurolink the Hatch is survivable once; outside one it wipes the party.
public sealed class AddsHatches(SimWorld world, DamageSolver damage, AddsNeurolinks neurolinks, ICollection<SimEnemy> spawned)
{
    public const float CastSeconds = 2.7f;
    public const float MarkerLead = 0.09f;
    public const float ChaseAfterCast = 1.25f;
    public const float OviformSpeed = 5f;
    public const float Radius = 8f;
    // Follow stops at the hitbox edge.
    private const float ContactSlack = 0.3f;
    private const float ManaHypersensitivitySeconds = 15.96f;

    private sealed class Oviform(SimEnemy enemy, SimCharacter target)
    {
        public SimEnemy Enemy { get; } = enemy;
        public SimCharacter Target { get; } = target;
        public bool Chasing { get; set; }
        public bool Hatched { get; set; }
    }

    private readonly List<Oviform> oviforms = [];

    public void Generate(SimEnemy? twintania, IEnumerable<PartyRole> targets)
    {
        if (twintania is not { } boss) return;
        UcobSim.CastSelf(boss, ActionId.Generate, CastSeconds);
        foreach (var role in targets)
        {
            if (world.Party.Get(role) is not { } target || !target.IsAlive()) continue;
            target.AttachLockonVfx(LockonId.Hatch);
            var enemy = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Oviform,
                NameId: BNpcNameId.Oviform,
                Level: Level,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(boss.Position, boss.Rotation)));
            if (enemy == null) continue;
            spawned.Add(enemy);
            oviforms.Add(new Oviform(enemy, target));
        }
    }

    public void Chase()
    {
        foreach (var oviform in oviforms.Where(o => !o.Chasing && !o.Hatched))
        {
            oviform.Chasing = true;
            oviform.Enemy.Follow(oviform.Target, OviformSpeed);
        }
    }

    public void Tick()
    {
        foreach (var oviform in oviforms.Where(o => o.Chasing && !o.Hatched).ToList())
        {
            if (!oviform.Target.IsAlive())
            {
                oviform.Hatched = true;
                oviform.Enemy.Despawn();
                continue;
            }
            var contact = oviform.Enemy.HitboxRadius + oviform.Target.HitboxRadius + ContactSlack;
            if (Vector2.Distance(UcobSim.Flat(oviform.Enemy.Position), UcobSim.Flat(oviform.Target.Position)) <= contact) Hatch(oviform);
        }
    }

    // The Oviforms still out vanish with her.
    public void Clear()
    {
        foreach (var oviform in oviforms.Where(o => !o.Hatched))
        {
            oviform.Hatched = true;
            oviform.Enemy.Despawn();
        }
        oviforms.Clear();
    }

    private void Hatch(Oviform oviform)
    {
        oviform.Hatched = true;
        var enemy = oviform.Enemy;
        var target = oviform.Target;
        enemy.Follow(null);
        if (!neurolinks.Contains(target))
        {
            UcobSim.PlayEffect(enemy, ActionId.DeepHatch, 1.1f);
            foreach (var member in UcobSim.AliveMembers(world.Party).ToList())
                member.Die(ActionId.DeepHatch, ReferenceEquals(member, target)
                    ? "the Hatch reached you outside a Neurolink (run into one before it arrives)"
                    : "Deep Hatch: the marked player wasn't in a Neurolink");
        }
        else
        {
            UcobSim.PlayEffect(enemy, ActionId.Hatch, 1.1f, target: target.GameObjectId, at: target.Position);
            foreach (var hit in UcobSim.AliveMembers(world.Party).Where(m => Vector2.Distance(UcobSim.Flat(m.Position), UcobSim.Flat(target.Position)) <= Radius).ToList())
            {
                if (hit.HasStatus(StatusId.ManaHypersensitivity))
                    hit.Die(ActionId.Hatch, "took a second Hatch with Mana Hypersensitivity (Hatches go to separate Neurolinks)");
                else if (!neurolinks.Contains(hit))
                    hit.Die(ActionId.Hatch, "caught in a Hatch outside a Neurolink (keep 8y away from the marked player)");
                else
                {
                    damage.ApplyDamage(hit, 0.1f, ActionId.Hatch, "Hatch", lethal: false);
                    hit.AddStatus(StatusId.ManaHypersensitivity, ManaHypersensitivitySeconds);
                }
            }
        }
        world.Events.Add(0.65f, enemy.Despawn);
    }
}
