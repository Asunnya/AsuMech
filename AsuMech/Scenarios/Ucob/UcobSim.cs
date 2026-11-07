using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AsuMech.Scenarios.Ucob;

// The arena centre is the zone origin, so scenario-local spots are world spots on the floor.
public static class UcobSim
{
    public static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    public static Vector2 Flat(Vector3 at) => new(at.X, at.Z);

    public static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    public static bool IsTank(SimCharacter member) => member is ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank };

    public static IEnumerable<SimCharacter> AliveMembers(SimParty party)
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    public static void CastSelf(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, targetId: caster.GameObjectId);

    public static void CastAt(SimEnemy? caster, uint actionId, float castSeconds, SimCharacter target) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, targetId: target.GameObjectId);

    // An effect without a position plays at the arena centre, so default it to the caster.
    public static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);

    public static SimEventObject? SpawnEObj(SimWorld world, uint eobjId, Vector2 at) =>
        world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = eobjId,
            Placement = new Placement(ToWorld(at), 0f),
            TimelineState = 1,
        });

    // Linear between the samples; past the last one, the last fraction holds.
    public static float Interpolate((float At, float Fraction)[] samples, float t)
    {
        if (t <= samples[0].At) return samples[0].Fraction;
        for (var i = 1; i < samples.Length; i++)
        {
            if (t > samples[i].At) continue;
            var (a, b) = (samples[i - 1], samples[i]);
            return a.Fraction + (b.Fraction - a.Fraction) * (t - a.At) / (b.At - a.At);
        }
        return samples[^1].Fraction;
    }
}
