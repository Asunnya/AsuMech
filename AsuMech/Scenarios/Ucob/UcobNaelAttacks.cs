using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// A quote names, in order, the moves Nael follows it with.
public sealed record NaelTripleQuote(string Text, params NaelMove[] Moves);

public static class UcobNaelAttacks
{
    public const float LunarDynamoInner = 6f; // UNVERIFIED
    public const float IronChariotRadius = 8.55f;
    public const float ThermionicBeamRadius = 4f;
    public const int ThermionicBeamMinimumStack = 4; // UNVERIFIED
    public const float RavenDiveRadius = 3f;
    public const int ClawHits = 5;
    public const float ClawInterval = 0.805f;
    public const float RavensbeakCastSeconds = 3.7f;
    private const float PiercingResistanceDownSeconds = 45f;

    // `target` is the player a Thermionic Beam or a Raven Dive lands on.
    public static void Move(SimParty party, DamageSolver damage, SimEnemy? nael, NaelMove move, SimCharacter? target)
    {
        if (nael is not { } boss) return;
        switch (move)
        {
            case NaelMove.LunarDynamo:
                UcobSim.PlayEffect(boss, ActionId.LunarDynamo, 1.1f);
                foreach (var hit in UcobSim.AliveMembers(party).Where(m => Distance(m, boss.Position) > LunarDynamoInner).ToList())
                    hit.Die(ActionId.LunarDynamo, "outside Nael's melee range for Lunar Dynamo (\"hallowed moon\": get in)");
                break;
            case NaelMove.IronChariot:
                UcobSim.PlayEffect(boss, ActionId.IronChariot, 1.1f);
                foreach (var hit in UcobSim.AliveMembers(party).Where(m => Distance(m, boss.Position) <= IronChariotRadius).ToList())
                    hit.Die(ActionId.IronChariot, "inside Iron Chariot (\"iron\": get out)");
                break;
            case NaelMove.ThermionicBeam:
                ThermionicBeam(party, damage, boss, target);
                break;
            case NaelMove.RavenDive:
                RavenDive(party, damage, boss, target);
                break;
        }
    }

    private static void ThermionicBeam(SimParty party, DamageSolver damage, SimEnemy nael, SimCharacter? target)
    {
        if (target == null || !target.IsAlive()) return;
        UcobSim.PlayEffect(nael, ActionId.ThermionicBeam, 1.1f, target: target.GameObjectId, at: target.Position);
        var hits = UcobSim.AliveMembers(party).Where(m => Distance(m, target.Position) <= ThermionicBeamRadius).ToList();
        foreach (var hit in hits)
        {
            if (hits.Count < ThermionicBeamMinimumStack) hit.Die(ActionId.ThermionicBeam, "Thermionic Beam wasn't shared (\"take fire\" / \"fiery edge\" / \"blazing path\": stack)");
            else damage.ApplyDamage(hit, 0.5f, ActionId.ThermionicBeam, "Thermionic Beam", lethal: false);
        }
    }

    // UNVERIFIED: whoever else it lands on is knocked back like Heavensfall.
    private static void RavenDive(SimParty party, DamageSolver damage, SimEnemy nael, SimCharacter? target)
    {
        if (target == null || !target.IsAlive()) return;
        nael.SetPosition(new Placement(target.Position, nael.Rotation));
        UcobSim.PlayEffect(nael, ActionId.RavenDive, 1.1f, target: target.GameObjectId, at: target.Position);
        damage.ApplyDamage(target, 0.2f, ActionId.RavenDive, "Raven Dive", lethal: false);
        foreach (var hit in UcobSim.AliveMembers(party).Where(m => !ReferenceEquals(m, target) && Distance(m, target.Position) <= RavenDiveRadius).ToList())
            hit.Knockback(target.Position, HeavensfallKnockback);
    }

    public static void BahamutsClaw(DamageSolver damage, SimEnemy? nael, SimCharacter? tank)
    {
        if (tank == null || !tank.IsAlive()) return;
        UcobSim.PlayEffect(nael, ActionId.BahamutsClaw, 0.6f, target: tank.GameObjectId);
        damage.ApplyDamage(tank, 0.08f, ActionId.BahamutsClaw, "Bahamut's Claw", lethal: false);
    }

    public static void CastRavensbeak(SimEnemy? nael, SimCharacter? tank)
    {
        if (tank != null) UcobSim.CastAt(nael, ActionId.Ravensbeak, RavensbeakCastSeconds, tank);
    }

    public static void Ravensbeak(DamageSolver damage, SimEnemy? nael, SimCharacter? tank)
    {
        if (tank == null || !tank.IsAlive()) return;
        UcobSim.PlayEffect(nael, ActionId.Ravensbeak, 1.1f, target: tank.GameObjectId);
        if (tank.HasStatus(StatusId.PiercingResistanceDownII))
        {
            tank.Die(ActionId.Ravensbeak, "took Ravensbeak with Piercing Resistance Down II (tanks swap after each one)");
            return;
        }
        damage.ApplyDamage(tank, 0.5f, ActionId.Ravensbeak, "Ravensbeak", lethal: false);
        tank.AddStatus(StatusId.PiercingResistanceDownII, PiercingResistanceDownSeconds);
    }

    private static float Distance(SimCharacter member, Vector3 from) => Vector2.Distance(UcobSim.Flat(member.Position), UcobSim.Flat(from));
}
