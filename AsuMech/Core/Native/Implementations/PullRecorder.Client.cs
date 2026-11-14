using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Trace;
using AsuMech.Core.Native.Implementations.Pointers;
using AsuMech.Core.Native.Interfaces;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;
using InteropGenerator.Runtime;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using SceneVfxObject = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.VfxObject;

namespace AsuMech.Core.Native.Implementations;

// What the client knows beyond the packets: VFX it spawns, who each enemy is looking at, enmity,
// model and cast-bar state, markers, the countdown, and what the local player asks for.
internal sealed unsafe partial class PullRecorder
{
    // The actor VFX constructor, as VFX tools hook it; it matches once in the current client.
    private const string ActorVfxCreateSignature =
        "40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8";

    // BaseParam rows in the order a stats line lists them: Str, Dex, Vit, Int, Mnd, Pie, Attack,
    // DirectHit, Crit, AttackMagic, HealMagic, Det, SkS, SpS, Ten.
    private static readonly int[] StatsBaseParams = [1, 2, 3, 4, 5, 6, 20, 22, 27, 33, 34, 44, 45, 46, 19];

    private delegate nint ActorVfxCreateDelegate(byte* path, nint caster, nint target, float a4, byte a5, ushort a6, byte a7);

    private Hook<ActorVfxCreateDelegate>? actorVfxHook;
    private Hook<SceneVfxObject.Delegates.Create>? staticVfxHook;
    private Hook<PacketDispatcherPointers.HandleActorGaugePacketDelegate>? gaugeHook;
    private Hook<PacketDispatcherPointers.HandleUpdatePartyPacketDelegate>? partyHook;
    private Hook<PacketDispatcherPointers.HandlePlayerStatsPacketDelegate>? statsHook;
    private Hook<PacketDispatcherPointers.HandleUpdateHaterPacketDelegate>? haterHook;
    private Hook<PacketDispatcherPointers.HandleUpdateRecastTimesPacketDelegate>? recastHook;
    private Hook<ActionManager.Delegates.UseAction>? useActionHook;
    private Hook<ActionManager.Delegates.UseActionLocation>? useActionLocationHook;

    private readonly Dictionary<uint, ulong> enemyTargets = [];
    private readonly Dictionary<uint, (uint Model, int Render, float Scale, float Hitbox)> objectStates = [];
    private readonly Dictionary<uint, (uint Action, float Total)> castBars = [];
    private readonly (bool Active, Vector3 Position)[] waymarks = new (bool, Vector3)[8];
    private ulong[] signs = [];
    private bool countdownActive;
    private float countdownLeft;
    private float animationLock;

    private void HookClient()
    {
        actorVfxHook = TryHook<ActorVfxCreateDelegate>("ActorVfx", () => Plugin.SigScanner.ScanText(ActorVfxCreateSignature), OnActorVfx);
        staticVfxHook = TryHook<SceneVfxObject.Delegates.Create>("StaticVfx", () => SceneVfxObject.Addresses.Create.Value, OnStaticVfx);
        gaugeHook = TryHook<PacketDispatcherPointers.HandleActorGaugePacketDelegate>("Gauge",
            () => System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleActorGaugePacket), OnGauge);
        partyHook = TryHook<PacketDispatcherPointers.HandleUpdatePartyPacketDelegate>("UpdateParty",
            () => System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleUpdatePartyPacket), OnParty);
        statsHook = TryHook<PacketDispatcherPointers.HandlePlayerStatsPacketDelegate>("PlayerStats",
            () => System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandlePlayerStatsPacket), OnStats);
        haterHook = TryHook<PacketDispatcherPointers.HandleUpdateHaterPacketDelegate>("UpdateHater",
            () => System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleUpdateHaterPacket), OnHater);
        recastHook = TryHook<PacketDispatcherPointers.HandleUpdateRecastTimesPacketDelegate>("UpdateRecastTimes",
            () => System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleUpdateRecastTimesPacket), OnRecast);
        useActionHook = TryHook<ActionManager.Delegates.UseAction>("UseAction", () => ActionManager.Addresses.UseAction.Value, OnUseAction);
        useActionLocationHook = TryHook<ActionManager.Delegates.UseActionLocation>("UseActionLocation",
            () => ActionManager.Addresses.UseActionLocation.Value, OnUseActionLocation);
    }

    private void ResetClient()
    {
        enemyTargets.Clear();
        objectStates.Clear();
        castBars.Clear();
        Array.Clear(waymarks);
        signs = [];
        countdownActive = false;
        countdownLeft = 0f;
        animationLock = 0f;
    }

    // ── Per frame ─────────────────────────────────────────────────────────────

    private void TickClient()
    {
        TickMarkers();
        TickCountdown();
        var am = ActionManager.Instance();
        if (am != null)
        {
            var lockNow = am->AnimationLock;
            if (lockNow > animationLock + 0.01f && lockNow > 0f)
                timeline!.Add("anim_lock", LocalPlayerId, NameOf(LocalPlayerId), detail: $"lock={SimTimeline.F(lockNow, "F3")}");
            animationLock = lockNow;
        }
    }

    private void TickObjectClient(IGameObject obj)
    {
        var id = obj.EntityId;
        if (obj is not IBattleChara chara) return;
        if (id >> 28 == 4)
        {
            var target = obj.TargetObjectId;
            if (!enemyTargets.TryGetValue(id, out var was) || was != target)
            {
                var targetId = (uint)target is var t and not NoTarget ? t : 0;
                timeline!.Add("aggro_target", id, NameOf(id), target: targetId, targetName: NameOf(targetId));
            }
            enemyTargets[id] = target;
        }
        var native = (GameObjectStruct*)obj.Address;
        var model = obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Pc ? 0u : (uint)((CharacterStruct*)obj.Address)->ModelContainer.ModelCharaId;
        (uint Model, int Render, float Scale, float Hitbox) state = (model, (int)native->RenderFlags, native->Scale, obj.HitboxRadius);
        if (!objectStates.TryGetValue(id, out var before) || before != state)
        {
            if (id >> 28 != 1 || objectStates.ContainsKey(id))
                timeline!.Add("state", id, NameOf(id), model.ToString(), "ModelChara",
                    detail: $"render=0x{state.Render:X} scale={SimTimeline.F(state.Scale, "F3")} hitbox={SimTimeline.F(state.Hitbox)}");
            objectStates[id] = state;
        }
        if (chara.IsCasting)
        {
            if (!castBars.ContainsKey(id))
            {
                castBars[id] = (chara.CastActionId, chara.TotalCastTime);
                var castTarget = (uint)chara.CastTargetObjectId is var ct and not NoTarget ? ct : 0;
                timeline!.Add("cast_bar", id, NameOf(id), chara.CastActionId.ToString(), ActionName(chara.CastActionId), castTarget, NameOf(castTarget),
                    detail: $"start total={SimTimeline.F(chara.TotalCastTime)} interruptible={(chara.IsCastInterruptible ? 1 : 0)}");
            }
        }
        else if (castBars.Remove(id, out var bar))
        {
            timeline!.Add("cast_bar", id, NameOf(id), bar.Action.ToString(), ActionName(bar.Action),
                detail: $"end at={SimTimeline.F(chara.CurrentCastTime)} total={SimTimeline.F(bar.Total)}");
        }
    }

    private void ForgetClient(uint id)
    {
        enemyTargets.Remove(id);
        objectStates.Remove(id);
        castBars.Remove(id);
    }

    private void TickMarkers()
    {
        var marking = MarkingController.Instance();
        if (marking == null) return;
        var fields = marking->FieldMarkers;
        for (var i = 0; i < fields.Length && i < waymarks.Length; i++)
        {
            var now = (fields[i].Active, fields[i].Position);
            if (now == waymarks[i]) continue;
            if (waymarks[i].Active) LogWaymark(false, i, waymarks[i].Position);
            if (now.Active) LogWaymark(true, i, now.Position);
            waymarks[i] = now;
        }
        var markers = marking->Markers;
        if (signs.Length != markers.Length) signs = new ulong[markers.Length];
        for (var i = 0; i < markers.Length; i++)
        {
            var now = (ulong)markers[i];
            if (now == signs[i]) continue;
            if (signs[i] != 0 && (uint)signs[i] != NoTarget) LogSign(false, i, (uint)signs[i]);
            if (now != 0 && (uint)now != NoTarget) LogSign(true, i, (uint)now);
            signs[i] = now;
        }
    }

    private void LogWaymark(bool added, int index, Vector3 position)
    {
        timeline!.Add("waymark", id: index.ToString(), name: added ? "add" : "remove", position: position);
        netLog?.Waymark(added, (uint)index, 0, "", position);
    }

    private void LogSign(bool added, int index, uint target)
    {
        timeline!.Add("sign", id: index.ToString(), name: added ? "add" : "remove", target: target, targetName: NameOf(target));
        netLog?.Sign(added, (uint)index, 0, "", target, LogName(target));
    }

    private void TickCountdown()
    {
        var module = AgentModule.Instance();
        if (module == null) return;
        var agent = (AgentCountDownSettingDialog*)module->GetAgentByInternalId(AgentId.CountDownSettingDialog);
        if (agent == null) return;
        var active = agent->Active;
        if (active && !countdownActive)
        {
            var starter = agent->InitiatorId;
            var seconds = (int)MathF.Ceiling(agent->TimeRemaining);
            timeline!.Add("countdown", starter, NameOf(starter), "start", detail: $"seconds={seconds}");
            netLog?.Countdown(starter, WorldOf(starter), seconds, LogName(starter));
        }
        else if (!active && countdownActive && countdownLeft > 0.5f)
        {
            timeline!.Add("countdown", id: "cancel", detail: $"left={SimTimeline.F(countdownLeft, "F1")}");
            netLog?.CountdownCancel(0, 0, "");
        }
        countdownActive = active;
        countdownLeft = agent->TimeRemaining;
    }

    private static uint WorldOf(uint id) => Plugin.ObjectTable.SearchByEntityId(id) is IPlayerCharacter p ? p.HomeWorld.RowId : 0;

    // ── Hooks ─────────────────────────────────────────────────────────────────

    private nint OnActorVfx(byte* path, nint caster, nint target, float a4, byte a5, ushort a6, byte a7)
    {
        var result = actorVfxHook!.Original(path, caster, target, a4, a5, a6, a7);
        if (timeline == null) return result;
        try
        {
            var source = caster != 0 ? ((GameObjectStruct*)caster)->EntityId : 0;
            var victim = target != 0 ? ((GameObjectStruct*)target)->EntityId : 0;
            timeline.Add("vfx_actor", source, NameOf(source), new string((sbyte*)path), "", victim, NameOf(victim));
        }
        catch (Exception e)
        {
            DiagnosticLog.Warn($"[PullRecorder] actor vfx row failed: {e.Message}");
        }
        return result;
    }

    private SceneVfxObject* OnStaticVfx(CStringPointer path, CStringPointer pool)
    {
        var result = staticVfxHook!.Original(path, pool);
        if (timeline == null) return result;
        timeline.Add("vfx", id: path.ToString(), name: pool.ToString());
        return result;
    }

    private void OnGauge(JobGaugeManager* manager, ActorGaugePacket* packet)
    {
        gaugeHook!.Original(manager, packet);
        if (!Begin()) return;
        var words = (uint*)packet;
        timeline!.Add("gauge", LocalPlayerId, NameOf(LocalPlayerId), packet->ClassJobId.ToString(),
            detail: Convert.ToHexString(new ReadOnlySpan<byte>(packet, 16)));
        netLog?.Gauge(LocalPlayerId, words[0], words[1], words[2], words[3]);
    }

    private void OnParty(uint unused, UpdatePartyPacket* packet, byte replayGroup)
    {
        partyHook!.Original(unused, packet, replayGroup);
        if (!Begin()) return;
        var members = new List<uint>();
        for (var i = 0; i < UpdatePartyPacket.Capacity && i < packet->MemberCount; i++)
        {
            var member = (UpdatePartyPacketMember*)((byte*)packet + i * sizeof(UpdatePartyPacketMember));
            if (member->EntityId != 0) members.Add(member->EntityId);
        }
        timeline!.Add("party", id: members.Count.ToString(), detail: string.Join(" ", members.Select(m => $"{m:X8}={NameOf(m)}")));
        netLog?.PartyList(members);
    }

    private void OnStats(uint unused, PlayerStatsPacket* packet)
    {
        statsHook!.Original(unused, packet);
        if (!Begin()) return;
        var player = PlayerState.Instance();
        var stats = new int[StatsBaseParams.Length];
        for (var i = 0; i < stats.Length; i++) stats[i] = player->Attributes[StatsBaseParams[i]];
        var job = Plugin.ObjectTable.LocalPlayer?.ClassJob.RowId ?? 0;
        timeline!.Add("stats", LocalPlayerId, NameOf(LocalPlayerId), job.ToString(), "ClassJob", detail: string.Join(" ", stats));
        netLog?.PlayerStats(job, stats, 0);
    }

    private void OnHater(ulong unused, HaterPacket* packet)
    {
        haterHook!.Original(unused, packet);
        if (!Begin()) return;
        var entries = (HaterPacketEntry*)((byte*)packet + HaterPacket.EntriesOffset);
        var list = new List<string>();
        for (var i = 0; i < packet->Count && i < HaterPacket.Capacity; i++)
            list.Add($"{entries[i].EntityId:X8}:{entries[i].Enmity}");
        timeline!.Add("enmity", LocalPlayerId, NameOf(LocalPlayerId), packet->Count.ToString(), detail: string.Join(" ", list));
    }

    private void OnRecast(uint entityId, UpdateRecastTimesPacket* packet)
    {
        recastHook!.Original(entityId, packet);
        if (!Begin()) return;
        var groups = new List<string>();
        for (var i = 0; i < UpdateRecastTimesPacket.Capacity; i++)
            if (packet->Total[i] > 0f) groups.Add($"{i}:{SimTimeline.F(packet->Elapsed[i])}/{SimTimeline.F(packet->Total[i])}");
        timeline!.Add("recast", entityId, NameOf(entityId), detail: string.Join(" ", groups));
    }

    private bool OnUseAction(ActionManager* self, ActionType actionType, uint actionId, ulong targetId, uint extraParam,
        ActionManager.UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted)
    {
        var result = useActionHook!.Original(self, actionType, actionId, targetId, extraParam, mode, comboRouteId, outOptAreaTargeted);
        if (timeline == null) return result;
        var target = (uint)targetId is var t and not NoTarget ? t : 0;
        timeline.Add("use_action", LocalPlayerId, NameOf(LocalPlayerId), actionId.ToString(), ActionName(actionId), target, NameOf(target),
            detail: $"type={actionType} mode={mode} ok={(result ? 1 : 0)}");
        return result;
    }

    private bool OnUseActionLocation(ActionManager* self, ActionType actionType, uint actionId, ulong targetId, Vector3* location, uint extraParam, byte a7)
    {
        var result = useActionLocationHook!.Original(self, actionType, actionId, targetId, location, extraParam, a7);
        if (timeline == null) return result;
        timeline.Add("use_action", LocalPlayerId, NameOf(LocalPlayerId), actionId.ToString(), ActionName(actionId),
            position: location != null ? *location : null, detail: $"type={actionType} at_location=1 ok={(result ? 1 : 0)}");
        return result;
    }
}
