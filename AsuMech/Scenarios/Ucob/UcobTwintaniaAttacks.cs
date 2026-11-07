using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

public static class UcobTwintaniaAttacks
{
    public const float PlummetRange = 8f;
    public const float PlummetHalfAngle = MathF.PI / 4f; // UNVERIFIED
    public const float DeathSentenceCastSeconds = 3.7f;
    private const float SlashingResistanceDownSeconds = 34f;

    // A frontal cleave toward her tank: tanks take it, anyone else in front dies.
    public static void Plummet(SimParty party, DamageSolver damage, SimEnemy? twintania, SimCharacter? tank)
    {
        if (twintania is not { } boss || tank == null || !tank.IsAlive()) return;
        var facing = UcobSim.RotationTowards(boss.Position, tank.Position);
        boss.SetRotation(facing);
        UcobSim.PlayEffect(boss, ActionId.Plummet, 1.1f, facing, tank.GameObjectId);
        var forward = new Vector2(MathF.Sin(facing), MathF.Cos(facing));
        var range = PlummetRange + boss.HitboxRadius;
        foreach (var hit in UcobSim.AliveMembers(party).ToList())
        {
            var to = UcobSim.Flat(hit.Position) - UcobSim.Flat(boss.Position);
            if (to.Length() > range || (to.Length() > 0.01f && Vector2.Dot(Vector2.Normalize(to), forward) < MathF.Cos(PlummetHalfAngle))) continue;
            if (UcobSim.IsTank(hit)) damage.ApplyDamage(hit, 0.25f, ActionId.Plummet, "Plummet", lethal: false);
            else hit.Die(ActionId.Plummet, "stood in front of Twintania for Plummet (stay behind her)");
        }
    }

    public static void CastDeathSentence(SimEnemy? twintania, SimCharacter? tank)
    {
        if (tank != null) UcobSim.CastAt(twintania, ActionId.DeathSentence, DeathSentenceCastSeconds, tank);
    }

    public static void DeathSentence(DamageSolver damage, SimEnemy? twintania, SimCharacter? tank)
    {
        if (tank == null || !tank.IsAlive()) return;
        UcobSim.PlayEffect(twintania, ActionId.DeathSentence, 1.1f, target: tank.GameObjectId);
        if (tank.HasStatus(StatusId.SlashingResistanceDownII))
        {
            tank.Die(ActionId.DeathSentence, "took Death Sentence with Slashing Resistance Down II (tanks swap after each one)");
            return;
        }
        damage.ApplyDamage(tank, 0.6f, ActionId.DeathSentence, "Death Sentence", lethal: false);
        tank.AddStatus(StatusId.SlashingResistanceDownII, SlashingResistanceDownSeconds);
    }
}
