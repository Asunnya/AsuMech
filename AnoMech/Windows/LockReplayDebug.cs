#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using AnoMech.Core;
using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Implementations.Pointers;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using Condition = Lumina.Excel.Sheets.Condition;

namespace AnoMech.Windows;

// Replays, packet for packet, what the TEA clear replay sent the recorder for the two freezes
// (Temporal Stasis → Temporal Displacement 1119, Divine Judgment → Down for the Count 783), on the
// local player and straight into the client's handlers. The Duty Recorder doesn't record
// PlayerStateFlags, so the server-owned Conditions that may go with them are a separate step.
internal sealed unsafe class LockReplayDebug
{
    private const uint NoTarget = 0xE0000000;
    private const ushort TemporalDisplacement = 1119;
    private const ushort Looper = 1624;
    private const ushort MemoryLoss = 1626;
    // UNVERIFIED: the capture ended in a death 3.8s in; the real duration isn't known.
    private const float MemoryLossSeconds = 10f;
    private const ushort DownForTheCount = 783;
    private const ushort DownForTheCountParam = 3783;
    private const uint DivineJudgment = 18554;

    // The networked conditions whose log message is "while suffering status affliction".
    private static readonly ConditionFlag[] AfflictionFlags =
    [
        ConditionFlag.SufferingStatusAffliction,
        ConditionFlag.SufferingStatusAffliction2,
        ConditionFlag.SufferingStatusAffliction63,
        ConditionFlag.SufferingStatusAffliction72,
        ConditionFlag.SufferingStatusAffliction73,
    ];

    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    private struct EffectResultEntry
    {
        [FieldOffset(0x0)] public byte Slot;
        [FieldOffset(0x2)] public ushort StatusId;
        [FieldOffset(0x4)] public ushort Param;
        [FieldOffset(0x8)] public float Duration;
        [FieldOffset(0xC)] public uint SourceEntityId;
    }

    // One result; the handler reads the result count from byte 0 and result i at i * 0x58 + 4.
    [StructLayout(LayoutKind.Explicit, Size = 0x60)]
    private struct EffectResultPacket
    {
        public const int EntriesOffset = 0x1C;
        public const int Capacity = 4;

        [FieldOffset(0x00)] public byte Count;
        [FieldOffset(0x04)] public uint GlobalSequence;
        [FieldOffset(0x08)] public uint EntityId;
        [FieldOffset(0x0C)] public uint CurrentHP;
        [FieldOffset(0x10)] public uint MaxHP;
        [FieldOffset(0x14)] public ushort CurrentMP;
        // The target's index in the ActionEffect it confirms.
        [FieldOffset(0x16)] public byte TargetIndex;
        [FieldOffset(0x17)] public byte ClassJob;
        [FieldOffset(0x18)] public byte DamageShield;
        [FieldOffset(0x19)] public byte EntryCount;
    }

    private delegate void HandleEffectResultPacketDelegate(uint entityId, EffectResultPacket* packet, byte replay);

    [Signature("48 8B C4 44 88 40 18 89 48 08 53 55 57 48 81 EC 80 00 00 00 83 3D ?? ?? ?? ?? 03 41 0F B6 D8", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    private HandleEffectResultPacketDelegate handleEffectResult = null!;

    private readonly List<(DateTime Due, string Label, Action Send)> queue = [];
    private readonly List<string> results = [];
    private readonly bool[] afflictionChecks = new bool[AfflictionFlags.Length];
    private readonly uint[] networkedConditions;
    private bool casterIsTarget = true;

    public LockReplayDebug()
    {
        Plugin.GameInterop.InitializeFromAttributes(this);
        networkedConditions = Plugin.DataManager.GetExcelSheet<Condition>()
            .Where(row => row.IsNetworked).Select(row => row.RowId).OrderBy(id => id).ToArray();
    }

    public void Tick()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < queue.Count; i++)
        {
            if (queue[i].Due > now) continue;
            var (_, label, send) = queue[i];
            queue.RemoveAt(i--);
            try
            {
                send();
                Report($"{label} sent; {ConditionReadout()}");
            }
            catch (Exception e)
            {
                Report($"{label} threw: {e.Message}");
            }
        }
    }

    public void Draw()
    {
        ImGui.TextUnformatted("Lock replay (TEA freezes, retail packets 1:1 on self)");
        ImGui.Separator();
        ImGui.TextUnformatted(ConditionReadout());

        if (ImGui.Button("Temporal Stasis (1119)##lockreplay")) QueueTemporalStasis();
        ImGui.SameLine();
        if (ImGui.Button("Divine Judgment (783)##lockreplay")) QueueDivineJudgment();
        ImGui.SameLine();
        ImGui.Checkbox("Caster = target##lockreplay", ref casterIsTarget);
        if (ImGui.Button("Memory Loss (1626)##lockreplay")) QueueMemoryLoss();
        ImGui.SameLine();
        if (ImGui.Button("Memory Loss, no condition##lockreplay")) QueueMemoryLoss(withCondition: false);
        ImGui.SameLine();
        if (ImGui.Button("Clear Memory Loss##lockreplay")) QueueClearMemoryLoss();
        if (ImGui.Button("Approach target (0x0082, 3y)##lockreplay")) Run("approach", SendApproachTarget);
        ImGui.SameLine();
        if (ImGui.Button("End forced move (0x025D)##lockreplay")) Run("end", () => PacketDispatcherPointers.HandleForcedFollowEndPacket(0));
        if (ImGui.Button("Remove now##lockreplay")) RemoveNow();
        ImGui.SameLine();
        if (ImGui.Button("Clear##lockreplay")) results.Clear();

        ImGui.TextUnformatted("PlayerStateFlags (not in replays; unset networked flags keep their current value)");
        for (var i = 0; i < AfflictionFlags.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            ImGui.Checkbox($"{(int)AfflictionFlags[i]}##lockcond{i}", ref afflictionChecks[i]);
        }
        if (ImGui.Button("Send PlayerStateFlags##lockreplay")) Run("PlayerStateFlags", SendPlayerStateFlags);

        foreach (var line in results)
            ImGui.TextWrapped(line);
    }

    // List + GainEffect at 0, LoseEffect + list 8.93s later. No EffectResult, no ActionEffect.
    private void QueueTemporalStasis()
    {
        if (LocalPlayer() == null) return;
        Queue(0f, "1119 list", () => SendStatusList(add: (TemporalDisplacement, 0, 9.0f, NoTarget)));
        Queue(0.02f, "1119 AC 0x14", () => SendActorControl(0x14, TemporalDisplacement));
        Queue(8.93f, "1119 list w/o", () => SendStatusList(remove: TemporalDisplacement));
        Queue(8.95f, "1119 AC 0x15", () => SendActorControl(0x15, TemporalDisplacement, 0, NoTarget));
    }

    // ActionEffect at 0; EffectResult + list 0.433s later; LoseEffect + list when the 57s run out.
    private void QueueDivineJudgment()
    {
        if (LocalPlayer() == null) return;
        var caster = Caster();
        if (caster == null) return;
        var casterId = ((GameObject*)caster)->EntityId;
        uint sequence = 0;
        Queue(0f, "783 ActionEffect", () => sequence = SendDivineJudgmentEffect(caster));
        Queue(0.433f, "783 EffectResult", () => SendEffectResult(sequence, DownForTheCount, DownForTheCountParam, 57.0f, casterId));
        Queue(0.434f, "783 list", () => SendStatusList(add: (DownForTheCount, DownForTheCountParam, 56.96f, casterId)));
        Queue(57.43f, "783 AC 0x15", () => SendActorControl(0x15, DownForTheCount, DownForTheCountParam, casterId));
        Queue(57.44f, "783 list w/o", () => SendStatusList(remove: DownForTheCount));
    }

    // A live capture's frame, in its order: PlayerStateFlags with condition 29, GainEffect 1626,
    // LoseEffect 1624 (Looper, which Memory Loss replaces). No status list came with it; one is
    // sent so the status is on the player either way. The client then walks the player by itself.
    private void QueueMemoryLoss(bool withCondition = true)
    {
        if (LocalPlayer() == null) return;
        if (withCondition) Queue(0f, "1626 cond 29", () => SendCondition(ConditionFlag.SufferingStatusAffliction2, true));
        Queue(0f, "1626 list", () => SendStatusList(add: (MemoryLoss, 0, MemoryLossSeconds, NoTarget)));
        Queue(0f, "1626 AC 0x14", () => SendActorControl(0x14, MemoryLoss));
        Queue(0f, "1624 AC 0x15", () => SendActorControl(0x15, Looper, 0, 0, NoTarget));
    }

    private void QueueClearMemoryLoss()
    {
        Queue(0f, "1626 AC 0x15", () => SendActorControl(0x15, MemoryLoss, 0, 0, NoTarget));
        Queue(0f, "1626 list w/o", () => SendStatusList(remove: MemoryLoss));
        Queue(0f, "1626 cond 29 off", () => SendCondition(ConditionFlag.SufferingStatusAffliction2, false));
    }

    // The forced-move controller's approach mode toward the current target.
    private void SendApproachTarget()
    {
        if (Plugin.TargetManager.Target is not { } target)
        {
            Report("approach: target someone first");
            return;
        }
        var packet = new ForcedApproachPacket { Range = 3, TargetEntityId = target.EntityId };
        PacketDispatcherPointers.HandleForcedApproachPacket(&packet);
    }

    // Every networked condition as it stands, with `flag` forced.
    private void SendCondition(ConditionFlag flag, bool on)
    {
        var bits = stackalloc byte[PacketDispatcherPointers.PlayerStateFlagsLength];
        for (var i = 0; i < PacketDispatcherPointers.PlayerStateFlagsLength; i++) bits[i] = 0;
        for (var bit = 0; bit < networkedConditions.Length && bit < PacketDispatcherPointers.PlayerStateFlagsLength * 8; bit++)
        {
            var current = (ConditionFlag)networkedConditions[bit];
            if (current == flag ? on : Plugin.Condition[current]) bits[bit >> 3] |= (byte)(1 << (bit & 7));
        }
        PacketDispatcherPointers.HandlePlayerStateFlagsPacket(Conditions.Instance(), bits);
    }

    private void RemoveNow()
    {
        queue.Clear();
        Run("remove", () =>
        {
            SendActorControl(0x15, TemporalDisplacement, 0, NoTarget);
            SendActorControl(0x15, DownForTheCount, DownForTheCountParam, NoTarget);
            SendStatusList(remove: TemporalDisplacement);
            SendStatusList(remove: DownForTheCount);
        });
    }

    private uint SendDivineJudgmentEffect(BattleChara* caster)
    {
        var player = LocalPlayer();
        var sequence = ActionEffects.NextGlobalSequence();
        var casterObject = (GameObject*)caster;
        var header = new ActionEffectHandler.Header
        {
            AnimationTargetId = casterObject->GetGameObjectId(),
            ActionId = DivineJudgment,
            GlobalSequence = sequence,
            AnimationLock = 3.1f,
            BallistaEntityId = NoTarget,
            RotationInt = 0x7FFF,
            SpellId = (ushort)DivineJudgment,
            ActionType = (byte)ActionType.Action,
            NumTargets = 1,
        };
        var targetEffects = stackalloc ActionEffectHandler.TargetEffects[1];
        targetEffects[0] = default;
        targetEffects[0].Effects[0] = new ActionEffectHandler.Effect { Type = 0x03, Param1 = 0x75, Value = 13044 };
        targetEffects[0].Effects[1] = new ActionEffectHandler.Effect { Type = 0x0E, Param2 = 0xC7, Value = DownForTheCount };
        targetEffects[0].Effects[2] = new ActionEffectHandler.Effect { Type = 0x1B, Param4 = 0x80, Value = (ushort)DivineJudgment };
        var targetIds = stackalloc GameObjectId[1];
        targetIds[0] = ((GameObject*)player)->GetGameObjectId();
        Vector3 position = casterObject->Position;
        ActionEffectHandler.Receive(casterObject->EntityId, (Character*)caster, &position, &header, targetEffects, targetIds);
        return sequence;
    }

    private void SendEffectResult(uint sequence, ushort statusId, ushort param, float duration, uint sourceId)
    {
        var player = LocalPlayer();
        if (player == null) return;
        var chara = (Character*)player;
        var packet = new EffectResultPacket
        {
            Count = 1,
            GlobalSequence = sequence,
            EntityId = ((GameObject*)player)->EntityId,
            CurrentHP = chara->Health,
            MaxHP = chara->MaxHealth,
            CurrentMP = (ushort)chara->Mana,
            TargetIndex = 0,
            ClassJob = chara->ClassJob,
            EntryCount = 1,
        };
        var entry = (EffectResultEntry*)((byte*)&packet + EffectResultPacket.EntriesOffset);
        entry->Slot = (byte)FreeSlot(player, statusId);
        entry->StatusId = statusId;
        entry->Param = param;
        entry->Duration = duration;
        entry->SourceEntityId = sourceId;
        handleEffectResult(packet.EntityId, &packet, 0);
    }

    // The player's current StatusManager, with one status added in the lowest free slot or removed.
    private static void SendStatusList((ushort Id, ushort Param, float Time, uint Source)? add = null, ushort remove = 0)
    {
        var player = LocalPlayer();
        if (player == null) return;
        var chara = (Character*)player;
        var packet = new StatusEffectListDoublePacket
        {
            ClassJob = chara->ClassJob,
            Level = chara->Level,
            Level_2 = chara->Level,
            CurrentHP = chara->Health,
            MaxHP = chara->MaxHealth,
            CurrentMP = (ushort)chara->Mana,
            MaxMP = (ushort)chara->MaxMana,
        };
        var addSlot = add is { } a ? FreeSlot(player, a.Id) : -1;
        var statuses = player->StatusManager.Status;
        for (var slot = 0; slot < statuses.Length && slot < 2 * StatusEffectListDoublePacket.Capacity; slot++)
        {
            var entry = slot < StatusEffectListDoublePacket.Capacity
                ? (StatusEffectListStatus*)((byte*)&packet + StatusEffectListDoublePacket.StatusesOffset) + slot
                : (StatusEffectListStatus*)((byte*)&packet + StatusEffectListDoublePacket.ExtraStatusesOffset) + (slot - StatusEffectListDoublePacket.Capacity);
            if (slot == addSlot && add is { } s)
            {
                *entry = new StatusEffectListStatus { StatusId = s.Id, Param = s.Param, RemainingTime = s.Time, SourceEntityId = s.Source };
                continue;
            }
            var current = statuses[slot];
            if (current.StatusId == 0 || current.StatusId == remove) continue;
            *entry = new StatusEffectListStatus
            {
                StatusId = current.StatusId,
                Param = current.Param,
                RemainingTime = current.RemainingTime,
                SourceEntityId = current.SourceObject.ObjectId,
            };
        }
        PacketDispatcherPointers.HandleStatusEffectListDoublePacket(((GameObject*)player)->EntityId, &packet, 0);
    }

    private static void SendActorControl(uint category, uint arg1 = 0, uint arg2 = 0, uint arg3 = 0, uint arg4 = 0)
    {
        var player = LocalPlayer();
        if (player == null) return;
        PacketDispatcher.HandleActorControlPacket(((GameObject*)player)->EntityId, category, arg1, arg2, arg3, arg4, 0, 0, 0, 0, NoTarget, false);
    }

    private void SendPlayerStateFlags()
    {
        var conditions = Conditions.Instance();
        var bits = stackalloc byte[PacketDispatcherPointers.PlayerStateFlagsLength];
        for (var i = 0; i < PacketDispatcherPointers.PlayerStateFlagsLength; i++) bits[i] = 0;
        for (var bit = 0; bit < networkedConditions.Length && bit < PacketDispatcherPointers.PlayerStateFlagsLength * 8; bit++)
        {
            var flag = (ConditionFlag)networkedConditions[bit];
            var index = Array.IndexOf(AfflictionFlags, flag);
            var on = index >= 0 ? afflictionChecks[index] : Plugin.Condition[flag];
            if (on) bits[bit >> 3] |= (byte)(1 << (bit & 7));
        }
        PacketDispatcherPointers.HandlePlayerStateFlagsPacket(conditions, bits);
    }

    // The slot already holding the id, else the lowest empty one.
    private static int FreeSlot(BattleChara* player, ushort statusId)
    {
        var statuses = player->StatusManager.Status;
        for (var slot = 0; slot < statuses.Length; slot++)
            if (statuses[slot].StatusId == statusId) return slot;
        for (var slot = 0; slot < statuses.Length; slot++)
            if (statuses[slot].StatusId == 0) return slot;
        return statuses.Length - 1;
    }

    private BattleChara* Caster()
    {
        if (!casterIsTarget) return LocalPlayer();
        var target = Plugin.TargetManager.Target;
        var go = target == null ? null : (GameObject*)target.Address;
        if (go != null && go->IsCharacter()) return (BattleChara*)go;
        Report("no character targeted: target the caster or untick 'Caster = target'");
        return null;
    }

    private static BattleChara* LocalPlayer()
    {
        var player = Plugin.ObjectTable.LocalPlayer;
        return player == null ? null : (BattleChara*)player.Address;
    }

    private void Queue(float delaySeconds, string label, Action send)
        => queue.Add((DateTime.UtcNow.AddSeconds(delaySeconds), label, send));

    private void Run(string label, Action send) => Queue(0f, label, send);

    private static string ConditionReadout()
        => "cond " + string.Join(" ", AfflictionFlags.Select(f => $"{(int)f}={(Plugin.Condition[f] ? 1 : 0)}"))
           + $" Occupied={(Plugin.Condition[ConditionFlag.Occupied] ? 1 : 0)}";

    private void Report(string line)
    {
        Plugin.Log.Info($"[LockReplay] {line}");
        results.Insert(0, line);
        if (results.Count > 30) results.RemoveAt(results.Count - 1);
    }
}
#endif
