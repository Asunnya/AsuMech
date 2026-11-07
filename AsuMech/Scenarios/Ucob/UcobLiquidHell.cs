using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// Twintania's Liquid Hell: each drop lands under a player and leaves a puddle whose Burns kill.
public sealed class UcobLiquidHell(SimWorld world, DamageSolver damage)
{
    public const float Interval = 1.16f;
    public const float PuddleRadius = 6f;
    public const float PuddleAppears = 0.9f;
    // It burns from 1.8s after the drop, a while after it shows.
    public const float PuddleHurts = 1.8f;
    private const float PuddleFades = 11.7f;
    private const float PuddleGone = 12.7f;
    private const float BurnsSeconds = 15f;
    private const float BurnsKill = 3f; // UNVERIFIED

    private sealed record Puddle(Vector2 At, float HurtsAt, float FadesAt);

    private readonly List<Puddle> puddles = [];
    private readonly List<SimEventObject> eobjs = [];

    // Lands on `chosen` when given and alive, otherwise on whoever is furthest from her.
    public void Drop(SimEnemy? twintania, SimCharacter? chosen = null)
    {
        if (twintania is not { } boss) return;
        var target = chosen != null && chosen.IsAlive()
            ? chosen
            : UcobSim.AliveMembers(world.Party).OrderByDescending(m => Vector3.DistanceSquared(m.Position, boss.Position)).FirstOrDefault();
        if (target == null) return;
        var at = UcobSim.Flat(target.Position);
        UcobSim.PlayEffect(boss, ActionId.LiquidHell, 1.1f, target: target.GameObjectId, at: target.Position);
        damage.ApplyDamage(target, 0.05f, ActionId.LiquidHell, "Liquid Hell", lethal: false);
        world.Events.Add(PuddleAppears, () =>
        {
            var puddle = new Puddle(at, world.Events.Elapsed + PuddleHurts - PuddleAppears, world.Events.Elapsed + PuddleFades);
            puddles.Add(puddle);
            var eobj = UcobSim.SpawnEObj(world, EObjId.LiquidHell, at);
            if (eobj != null) eobjs.Add(eobj);
            world.Events.Add(PuddleFades, () =>
            {
                puddles.Remove(puddle);
                eobj?.FadeOut();
            });
            world.Events.Add(PuddleGone, () => eobj?.SetState(0));
        });
    }

    public void Tick(float now)
    {
        foreach (var member in UcobSim.AliveMembers(world.Party).Where(m => !m.HasStatus(StatusId.Burns)).ToList())
        {
            if (!puddles.Any(p => now >= p.HurtsAt && now < p.FadesAt && Vector2.Distance(UcobSim.Flat(member.Position), p.At) <= PuddleRadius)) continue;
            member.AddStatus(StatusId.Burns, BurnsSeconds);
            world.Events.Add(BurnsKill, () =>
            {
                if (member.IsAlive() && member.HasStatus(StatusId.Burns))
                    member.Die(ActionId.LiquidHell, "stood in a Liquid Hell puddle (its Burns kill; keep the puddles away from the party)");
            });
        }
    }

    public void Clear()
    {
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        puddles.Clear();
    }
}
