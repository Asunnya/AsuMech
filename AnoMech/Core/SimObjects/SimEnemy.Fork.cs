using System.Numerics;
using AnoMech.Core.EnemyActions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.SimObjects;

// The fork's own scenarios (DSR, M9S and its UWU phases) still script casts and effects by hand;
// upstream scenarios describe theirs as EnemyActions instead.
public sealed partial class SimEnemy
{
    public void NativeCast(uint actionId, ActionType actionType, float omenDelay, float castTime, bool interruptible, float? rotation = null, Vector3? position = null, GameObjectId? targetId = null, GameObjectId? ballistaId = null)
        => cast.NativeCast(actionId, actionType, omenDelay, castTime, interruptible, rotation, position, targetId, ballistaId);

    public void NativeActionEffect(uint actionId, float animationLock, ushort spellId, byte animationVariaton, ActionType actionType, byte flags, float? rotation = null, Vector3? position = null, GameObjectId? animationTargetId = null, GameObjectId? actionTargetId = null, GameObjectId? ballistaId = null)
        => cast.NativeActionEffect(actionId, animationLock, spellId, animationVariaton, actionType, flags, rotation, position, animationTargetId, actionTargetId, ballistaId);

    // A bar of `castSeconds` (the sheet's when null) whose effect lands as it ends, with no mechanics.
    public EnemyActionCast LegacyCast(uint actionId, Vector3? targetLocation = null, float? castSeconds = null, SimCharacter? target = null,
        float omenDelay = 0f, float omenRotate = 0f, byte animationVariation = 0, float animationLock = 0.6f)
    {
        var action = new EnemyAction(actionId)
        {
            Cast = new() { AnimationLock = animationLock, OmenDelay = omenDelay, CastSeconds = castSeconds },
            Area = new() { Rotation = omenRotate },
        };
        return actions.Start(action, targetLocation is { } at ? (CastTarget)at : target, animationVariation);
    }
}
