using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AnoMech.Core.Native.Interfaces;

// The server's ActionEffect packet: plays the action's release animation and VFX, and queues each
// target's effects until that target's EffectResult confirms them. No targets sends none; some
// actions only animate with one. World space.
public readonly record struct ActionEffectData(
    uint ActionId,
    ActionType ActionType,
    float AnimationLock,
    ushort SpellId,
    byte AnimationVariation,
    byte Flags,
    float Rotation,
    Vector3 Position,
    GameObjectId? AnimationTarget = null,
    IReadOnlyList<ActionEffectTarget>? Targets = null,
    GameObjectId? Ballista = null,
    // The local player's request this answers (ActionManager.LastUsedActionSequence); 0 for none.
    ushort SourceSequence = 0);

// A target's index in ActionEffectData.Targets is what its EffectResult names. At most 8 effects.
public readonly record struct ActionEffectTarget(GameObjectId Id, IReadOnlyList<ActionEffectEntry>? Effects = null);

// One effect entry as the server sends it.
public readonly record struct ActionEffectEntry(byte Type, byte Param0, byte Param1, byte Param2, byte Param3, byte Param4, ushort Value)
{
    private const byte DodgeType = 0x01;
    private const byte DamageType = 0x03;
    private const byte StatusGainType = 0x0E;
    private const byte KnockbackType = 0x1F;
    private const byte ActionMarkerType = 0x1B;
    private const byte UnaspectedElement = 7;
    // Param4 bit: Param3 carries bits 16-23 of the amount.
    private const byte LargeValueFlag = 0x40;
    // Param4 bit: the effect applies to the source.
    private const byte AtSourceFlag = 0x80;

    // AttackType as in the Action sheet: 1-4 physical, 5 magic, 6 darkness; 0 is typeless and sends
    // no element either, which is a push's zero hit.
    public static ActionEffectEntry Damage(uint amount, byte attackType) => new(
        DamageType, 0,
        attackType == 0 ? (byte)0 : (byte)(UnaspectedElement << 4 | (attackType & 0x0F)),
        0, (byte)(amount >> 16), amount > ushort.MaxValue ? LargeValueFlag : (byte)0, (ushort)amount);

    public static ActionEffectEntry Dodge() => new(DodgeType, 0, 0, 0, 0, 0, 0);

    // The gain flytext; the status list puts it on the character. Retail's Param0 is the low byte
    // of the status's strength (10 for Battle Litany's 10%); whether the client reads it is
    // UNVERIFIED.
    public static ActionEffectEntry StatusGain(ushort statusId, byte param0 = 0) => new(StatusGainType, param0, 0, 0, 0, 0, statusId);

    public static ActionEffectEntry Knockback(uint knockbackRow) => new(KnockbackType, 0, 0, 0, 0, 0, (ushort)knockbackRow);

    // Retail ends every target's list of an enemy action with it. On the local player's own action it
    // sets ActionManager.Combo, as a combo starter or, with comboContinuation, the next step.
    public static ActionEffectEntry ActionMarker(uint actionId, bool comboContinuation = false)
        => new(ActionMarkerType, comboContinuation ? (byte)1 : (byte)0, 0, 0, 0, AtSourceFlag, (ushort)actionId);
}
