using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using AsuMech.Core.Game.Trace;
using AsuMech.Core.Native.Implementations.Pointers;
using AsuMech.Core.Native.Interfaces;
using Dalamud.Game.ClientState.Objects.Types;
using System.Threading.Tasks;
using AsuMech.Network;
using Dalamud.Game.DutyState;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using ActorCastPacket = AsuMech.Core.Native.Implementations.Pointers.ActorCastPacket;
using ActorSetPosPacket = AsuMech.Core.Native.Implementations.Pointers.ActorSetPosPacket;
using MapEffectPacket = AsuMech.Core.Native.Implementations.Pointers.MapEffectPacket;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Network;
using DalamudObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using LuminaTerritoryType = Lumina.Excel.Sheets.TerritoryType;

namespace AsuMech.Core.Native.Implementations;

// Records a real savage or ultimate pull as the SimTimeline CSV the headless runs write, so the
// two can be diffed row for row. Taps the client's own packet handlers (the ones the sim feeds
// when it plays a scenario), not opcodes: a handler's signature outlives opcode shuffles, and a
// packet no handler claims comes out as a `raw` row with its payload. Statuses and despawns are
// read off the object table every frame, spawns and poses off the table at 10 Hz, because the
// status-list and despawn packets come in more shapes than the sim emulates.
//
// Zoning into a savage, ultimate or extreme asks in chat (or starts outright for a territory the
// player picked "Always here" for); `/asu rec` arms it anywhere. Never records while a scenario
// is loaded. The hooks are enabled for the recording's duration only.
internal sealed unsafe class PullRecorder : IDisposable
{
    private const uint NoTarget = 0xE0000000;
    private const long PoseIntervalMs = 100;
    private const int RawBytes = 0x40;
    private const int RawRowsPerOpcode = 50;
    private const int IpcHeaderSize = 0x10;
    // Retail's snapshot is 0x88 bytes: 65 states. UNVERIFIED that no duty sends a longer one.
    private const int SnapshotStates = 65;

    private readonly List<(Action Enable, Action Disable, IDisposable Owner)> hooks = [];
    private readonly Stopwatch clock = new();
    private StreamWriter? writer;
    private SimTimeline? timeline;
    private uint? gateTerritory;
    private bool gateRecordable;
    private bool armed;
    private bool prompted;
    private readonly DalamudLinkPayload yesLink, noLink, alwaysLink;
    private const uint YesLinkId = 7101, NoLinkId = 7102, AlwaysLinkId = 7103;

    // Recording state, cleared on Start.
    private readonly Dictionary<uint, string> names = [];
    private readonly HashSet<uint> announced = [];
    private readonly Dictionary<uint, (uint Caster, uint ActionId)> effects = [];
    private readonly Dictionary<uint, HashSet<(ushort StatusId, uint Source)>> statuses = [];
    private readonly Dictionary<uint, (Vector3 Position, float Rotation)> poses = [];
    private readonly Dictionary<ushort, int> rawCounts = [];
    private HashSet<uint> present = [];
    private long lastPoseMs;
    private bool handled;

    public string? Path { get; private set; }
    public bool Recording => timeline != null;
    public int Rows => timeline?.Count ?? 0;
    public TimeSpan Elapsed => clock.Elapsed;
    public IReadOnlyList<string> MissingHooks => missingHooks;
    private readonly List<string> missingHooks = [];

    private Hook<PacketDispatcher.Delegates.OnReceivePacket>? receiveHook;
    private Hook<PacketDispatcher.Delegates.HandleActorControlPacket>? actorControlHook;
    private Hook<ActionEffectHandler.Delegates.Receive>? actionEffectHook;
    private Hook<PacketDispatcherPointers.HandleActorCastPacketDelegate>? castHook;
    private Hook<PacketDispatcherPointers.HandleEffectResultBasicPacketDelegate>? effectResultHook;
    private Hook<PacketDispatcherPointers.HandleMapEffectPacketDelegate>? mapEffectHook;
    private Hook<PacketDispatcherPointers.HandleMapEffectSnapshotPacketDelegate>? mapEffectSnapshotHook;
    private Hook<PacketDispatcherPointers.HandleDirectorVarsPacketDelegate>? directorVarsHook;
    private Hook<PacketDispatcher.Delegates.HandleEventPlayPacket>? eventPlayHook;
    private Hook<PacketDispatcher.Delegates.HandleSpawnNpcPacket>? spawnNpcHook;
    private Hook<PacketDispatcher.Delegates.HandleSpawnObjectPacket>? spawnObjectHook;
    private Hook<PacketDispatcher.Delegates.HandleSpawnPlayerPacket>? spawnPlayerHook;
    private Hook<PacketDispatcherPointers.HandleActorMovePacketDelegate>? moveHook;
    private Hook<PacketDispatcherPointers.HandleActorSetPosPacketDelegate>? setPosHook;
    private Hook<PacketDispatcherPointers.HandleNpcKnockbackPacketDelegate>? knockbackHook;
    private Hook<PacketDispatcherPointers.HandleWeatherChangePacketDelegate>? weatherHook;

    public PullRecorder()
    {
        receiveHook = TryHook<PacketDispatcher.Delegates.OnReceivePacket>("OnReceivePacket",
            () => (nint)PacketDispatcher.StaticVirtualTablePointer->OnReceivePacket, OnReceivePacket);
        actorControlHook = TryHook<PacketDispatcher.Delegates.HandleActorControlPacket>("ActorControl",
            () => PacketDispatcher.Addresses.HandleActorControlPacket.Value, OnActorControl);
        actionEffectHook = TryHook<ActionEffectHandler.Delegates.Receive>("ActionEffect",
            () => ActionEffectHandler.Addresses.Receive.Value, OnActionEffect);
        castHook = TryHook<PacketDispatcherPointers.HandleActorCastPacketDelegate>("ActorCast",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleActorCastPacket), OnActorCast);
        effectResultHook = TryHook<PacketDispatcherPointers.HandleEffectResultBasicPacketDelegate>("EffectResultBasic",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleEffectResultBasicPacket), OnEffectResult);
        mapEffectHook = TryHook<PacketDispatcherPointers.HandleMapEffectPacketDelegate>("MapEffect",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleMapEffectPacket), OnMapEffect);
        mapEffectSnapshotHook = TryHook<PacketDispatcherPointers.HandleMapEffectSnapshotPacketDelegate>("MapEffectSnapshot",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleMapEffectSnapshotPacket), OnMapEffectSnapshot);
        directorVarsHook = TryHook<PacketDispatcherPointers.HandleDirectorVarsPacketDelegate>("DirectorVars",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleDirectorVarsPacket), OnDirectorVars);
        eventPlayHook = TryHook<PacketDispatcher.Delegates.HandleEventPlayPacket>("EventPlay",
            () => PacketDispatcher.Addresses.HandleEventPlayPacket.Value, OnEventPlay);
        spawnNpcHook = TryHook<PacketDispatcher.Delegates.HandleSpawnNpcPacket>("SpawnNpc",
            () => PacketDispatcher.Addresses.HandleSpawnNpcPacket.Value, OnSpawnNpc);
        spawnObjectHook = TryHook<PacketDispatcher.Delegates.HandleSpawnObjectPacket>("SpawnObject",
            () => PacketDispatcher.Addresses.HandleSpawnObjectPacket.Value, OnSpawnObject);
        spawnPlayerHook = TryHook<PacketDispatcher.Delegates.HandleSpawnPlayerPacket>("SpawnPlayer",
            () => PacketDispatcher.Addresses.HandleSpawnPlayerPacket.Value, OnSpawnPlayer);
        moveHook = TryHook<PacketDispatcherPointers.HandleActorMovePacketDelegate>("ActorMove",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleActorMovePacket), OnActorMove);
        setPosHook = TryHook<PacketDispatcherPointers.HandleActorSetPosPacketDelegate>("ActorSetPos",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleActorSetPosPacket), OnActorSetPos);
        knockbackHook = TryHook<PacketDispatcherPointers.HandleNpcKnockbackPacketDelegate>("NpcKnockback",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleNpcKnockbackPacket), OnNpcKnockback);
        weatherHook = TryHook<PacketDispatcherPointers.HandleWeatherChangePacketDelegate>("WeatherChange",
            () => Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleWeatherChangePacket), OnWeather);

        Plugin.DutyState.DutyStarted += OnDutyStarted;
        Plugin.DutyState.DutyWiped += OnDutyWiped;
        Plugin.DutyState.DutyCompleted += OnDutyCompleted;
        yesLink = Plugin.ChatGui.AddChatLinkHandler(YesLinkId, (_, _) => Arm());
        noLink = Plugin.ChatGui.AddChatLinkHandler(NoLinkId, (_, _) => Chat("Not recording this one."));
        alwaysLink = Plugin.ChatGui.AddChatLinkHandler(AlwaysLinkId, (_, _) => AlwaysHere());
    }

    // A hook that can't be placed costs that one row kind, not the recorder.
    private Hook<T>? TryHook<T>(string what, Func<nint> address, T detour) where T : Delegate
    {
        try
        {
            var hook = Plugin.GameInterop.HookFromAddress(address(), detour);
            hooks.Add((hook.Enable, hook.Disable, hook));
            return hook;
        }
        catch (Exception e)
        {
            missingHooks.Add(what);
            DiagnosticLog.Warn($"[PullRecorder] {what} hook not placed: {e.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        Plugin.DutyState.DutyStarted -= OnDutyStarted;
        Plugin.DutyState.DutyWiped -= OnDutyWiped;
        Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        Plugin.ChatGui.RemoveChatLinkHandler(YesLinkId);
        Plugin.ChatGui.RemoveChatLinkHandler(NoLinkId);
        Plugin.ChatGui.RemoveChatLinkHandler(AlwaysLinkId);
        Stop("plugin unloaded", upload: false);
        foreach (var hook in hooks) hook.Owner.Dispose();
        hooks.Clear();
    }

    // ── Gate ─────────────────────────────────────────────────────────────────

    public bool Armed => armed;

    // Records the current territory until the player leaves it or disarms.
    public void Arm()
    {
        if (Natives.Zone is { IsActive: true }) { Chat("Can't record while a scenario is loaded."); return; }
        if (armed) { Chat(timeline != null ? "Already recording." : "Recording starts as soon as you're in."); return; }
        armed = true;
        prompted = true;
    }

    public void Disarm()
    {
        armed = false;
        if (timeline != null) Stop("stopped");
        else Chat("Not recording.");
    }

    private void AlwaysHere()
    {
        if (gateTerritory is { } territory && !Plugin.Config.RecordAlwaysTerritories.Contains(territory))
        {
            Plugin.Config.RecordAlwaysTerritories.Add(territory);
            Plugin.Config.Save();
        }
        Arm();
    }

    public void Tick()
    {
        var territory = Plugin.ClientState.TerritoryType;
        if (gateTerritory != territory)
        {
            if (timeline != null) Stop("left the duty");
            gateTerritory = territory;
            gateRecordable = Natives.Data.IsRecordableDuty(territory) || Plugin.Game.Catalog.Scenarios.Any(s => s.Phase.Zone.TerritoryId == territory);
            armed = gateRecordable && Plugin.Config.RecordAlwaysTerritories.Contains(territory);
            prompted = armed;
        }
        var inStay = Natives.Zone is { IsActive: true };
        var player = Plugin.ObjectTable.LocalPlayer;
        if (gateRecordable && !inStay && player != null && !prompted && Plugin.Config.AskToRecordPulls)
        {
            prompted = true;
            Prompt();
        }
        var want = armed && player != null && !inStay;
        if (want && timeline == null) Start(territory);
        else if (!want && timeline != null) Stop(inStay ? "scenario loaded" : "player gone");
        if (timeline == null) return;
        try
        {
            TickObjects();
        }
        catch (Exception e)
        {
            DiagnosticLog.Warn($"[PullRecorder] object scan threw: {e}");
        }
    }

    // The file being written, if any; a clear must leave it alone.
    public string? Writing => writer != null ? Path : null;

    public string Directory => Plugin.Config.PullRecordingsDir is { Length: > 0 } dir
        ? dir
        : System.IO.Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "replays");

    private void Start(uint territory)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            Path = System.IO.Path.Combine(Directory, $"{territory}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            writer = new StreamWriter(Path, false, new UTF8Encoding(false), 1 << 16) { NewLine = "\n" };
        }
        catch (Exception e)
        {
            DiagnosticLog.Warn($"[PullRecorder] can't open {Path}: {e.Message}");
            writer = null;
            Path = null;
            return;
        }
        names.Clear();
        announced.Clear();
        effects.Clear();
        statuses.Clear();
        poses.Clear();
        rawCounts.Clear();
        present = [];
        lastPoseMs = -PoseIntervalMs;
        clock.Restart();
        timeline = new SimTimeline(writer) { Clock = () => (float)clock.Elapsed.TotalSeconds };
        var place = Plugin.DataManager.GetExcelSheet<LuminaTerritoryType>()?.GetRowOrDefault(territory)?.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
        var player = Plugin.ObjectTable.LocalPlayer;
        timeline.Add("recording", player?.EntityId ?? 0, player is null ? "" : PlayerLabel(player.EntityId), territory.ToString(), place,
            detail: $"started={DateTime.Now:O} game={new string(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GameVersionString)} missing_hooks={string.Join("/", missingHooks)}");
        foreach (var hook in hooks) hook.Enable();
        DiagnosticLog.Info($"[PullRecorder] recording {place} ({territory}) to {Path}");
    }

    private void Stop(string why, bool upload = true)
    {
        if (timeline == null) return;
        foreach (var hook in hooks) hook.Disable();
        foreach (var (opcode, count) in rawCounts.OrderByDescending(kv => kv.Value))
            timeline.Add("raw_count", id: $"0x{opcode:X4}", detail: $"count={count}");
        timeline.Add("recording_end", detail: why);
        timeline.Flush();
        writer?.Dispose();
        writer = null;
        var rows = timeline.Count;
        timeline = null;
        clock.Stop();
        DiagnosticLog.Info($"[PullRecorder] stopped ({why}): {rows} rows in {Path}");
        Chat($"Recording saved: {rows} rows, {Path}");
        if (upload && Plugin.Config.UploadRecordings && Path is { } path) _ = UploadAsync(path);
    }

    // Off the framework thread; only the chat line comes back to it.
    private static Task UploadAsync(string path)
    {
        var config = Plugin.Config;
        var (relay, token) = config.RecordingsDestination();
        return Multiplayer.RecordingUploader.UploadAsync(path, relay, token, RelayWire.RelayCredential(config.EnsurePeerSecret(), relay))
            .ContinueWith(t => Plugin.Framework.RunOnFrameworkThread(() =>
            {
                if (!t.IsCompletedSuccessfully) { Chat($"Upload failed: {t.Exception?.GetBaseException().Message}"); return; }
                if (!t.Result.Ok) { Chat($"Upload failed: {t.Result.Message}"); return; }
                Chat($"Upload: {t.Result.Message}{(Plugin.Config.KeepRecordingsAfterUpload ? "" : DeleteSent(path))}");
            }));
    }

    private static string DeleteSent(string path)
    {
        try
        {
            File.Delete(path);
            return "; removed from your disk";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Warn($"[PullRecorder] deleting the sent {path} failed: {e.Message}");
            return "";
        }
    }

    // ── Chat ──────────────────────────────────────────────────────────────────

    private void Prompt()
    {
        var message = new SeStringBuilder()
            .AddText("[AsuMech] Record this pull? ")
            .Add(yesLink).AddUiForeground("[Yes]", 45).Add(RawPayload.LinkTerminator).AddText(" ")
            .Add(noLink).AddUiForeground("[No]", 45).Add(RawPayload.LinkTerminator).AddText(" ")
            .Add(alwaysLink).AddUiForeground("[Always here]", 45).Add(RawPayload.LinkTerminator)
            .AddText("  (/asu rec, /asu rec stop)")
            .Build();
        Plugin.ChatGui.Print(new XivChatEntry { Type = XivChatType.SystemMessage, Message = message });
    }

    private static void Chat(string text)
        => Plugin.ChatGui.Print(new XivChatEntry { Type = XivChatType.SystemMessage, Message = $"[AsuMech] {text}" });

    // ── Object table: statuses and despawns every frame, poses at 10 Hz ──────

    private void TickObjects()
    {
        var now = clock.ElapsedMilliseconds;
        var samplePoses = now - lastPoseMs >= PoseIntervalMs;
        if (samplePoses) lastPoseMs = now;
        var seen = new HashSet<uint>();
        foreach (var obj in Plugin.ObjectTable)
        {
            if (obj == null || obj.Address == nint.Zero || obj.EntityId is 0 or NoTarget) continue;
            if (obj.ObjectKind is not (DalamudObjectKind.Pc or DalamudObjectKind.BattleNpc or DalamudObjectKind.EventObj)) continue;
            var id = obj.EntityId;
            seen.Add(id);
            if (obj is IBattleChara) DiffStatuses(id, (BattleChara*)obj.Address);
            if (!samplePoses) continue;
            if (id >> 28 == 1)
            {
                if (obj is ICharacter character && announced.Add(id))
                    timeline!.Add("member", id, PlayerLabel(id), character.ClassJob.RowId.ToString(), "ClassJob");
            }
            else if (!names.ContainsKey(id)) names[id] = obj.Name.TextValue;
            SamplePose(obj);
        }
        foreach (var gone in present.Where(id => !seen.Contains(id)))
        {
            timeline!.Add("despawn", gone, NameOf(gone));
            statuses.Remove(gone);
            poses.Remove(gone);
        }
        present = seen;
        if (samplePoses) timeline!.Flush();
    }

    private void DiffStatuses(uint id, BattleChara* chara)
    {
        var now = new HashSet<(ushort StatusId, uint Source)>();
        if (!statuses.TryGetValue(id, out var before))
        {
            before = [];
            statuses[id] = before;
        }
        foreach (ref var status in chara->StatusManager.Status)
        {
            if (status.StatusId == 0) continue;
            var source = status.SourceObject.ObjectId is var s and not NoTarget ? s : 0;
            var key = (status.StatusId, source);
            if (!now.Add(key)) continue;
            if (before.Contains(key)) continue;
            timeline!.Add("status_gain", id, NameOf(id), status.StatusId.ToString(), Natives.Data.Status(status.StatusId)?.Name ?? "",
                source, NameOf(source), detail: $"dur={SimTimeline.F(status.RemainingTime, "F1")} param={status.Param}");
        }
        foreach (var key in before.Where(k => !now.Contains(k)))
            timeline!.Add("status_lose", id, NameOf(id), key.StatusId.ToString(), Natives.Data.Status(key.StatusId)?.Name ?? "");
        statuses[id] = now;
    }

    private void SamplePose(IGameObject obj)
    {
        var position = obj.Position;
        var rotation = obj.Rotation;
        if (poses.TryGetValue(obj.EntityId, out var last)
            && Vector3.DistanceSquared(position, last.Position) < 0.0001f && MathF.Abs(rotation - last.Rotation) < 0.001f)
            return;
        poses[obj.EntityId] = (position, rotation);
        timeline!.Add("pos", obj.EntityId, NameOf(obj.EntityId), position: position, rotation: rotation,
            detail: obj.ObjectKind == DalamudObjectKind.EventObj ? "eobj" : "");
    }

    // ── Packet handlers ───────────────────────────────────────────────────────

    private void OnReceivePacket(PacketDispatcher* self, uint targetId, nint packet)
    {
        handled = false;
        receiveHook!.Original(self, targetId, packet);
        if (timeline == null || handled || packet == nint.Zero) return;
        try
        {
            var opcode = *(ushort*)(packet + 2);
            var count = rawCounts.GetValueOrDefault(opcode) + 1;
            rawCounts[opcode] = count;
            if (count > RawRowsPerOpcode) return;
            var payload = new ReadOnlySpan<byte>((byte*)(packet + IpcHeaderSize), RawBytes);
            timeline.Add("raw", targetId, NameOf(targetId), $"0x{opcode:X4}", detail: Convert.ToHexString(payload));
        }
        catch (Exception e)
        {
            DiagnosticLog.Warn($"[PullRecorder] raw row failed: {e.Message}");
        }
    }

    private void OnActorControl(uint entityId, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, uint arg7, uint arg8,
        GameObjectId targetId, bool isRecorded)
    {
        actorControlHook!.Original(entityId, category, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, targetId, isRecorded);
        if (!Begin()) return;
        var target = Id(targetId);
        var kind = target != 0 ? "ac_target" : entityId == LocalPlayerId ? "ac_self" : "ac";
        var p = target != 0 ? SimTimeline.Params(arg1, arg2, arg3, arg4) : SimTimeline.Params(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        timeline!.Add(kind, entityId, NameOf(entityId), $"0x{category:X}", target: target, targetName: NameOf(target), p: p);
    }

    private void OnActionEffect(uint casterEntityId, Character* caster, Vector3* targetPos, ActionEffectHandler.Header* header,
        ActionEffectHandler.TargetEffects* targetEffects, GameObjectId* targetIds)
    {
        actionEffectHook!.Original(casterEntityId, caster, targetPos, header, targetEffects, targetIds);
        if (!Begin()) return;
        var count = header->NumTargets;
        var hits = new List<string>(count);
        var targets = new List<string>(count);
        var damageTypes = new List<string>();
        var knockbacks = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var id = Id(targetIds[i]);
            if (id == 0) continue;
            targets.Add(id.ToString("X8"));
            hits.Add(NameOf(id) is { Length: > 0 } n ? n : id.ToString("X8"));
            foreach (var effect in targetEffects[i].Effects)
            {
                if (effect.Type is 0x03 or 0x05 or 0x06) damageTypes.Add($"0x{effect.Param1:X}");
                else if (effect.Type == 0x1F) knockbacks.Add(effect.Value.ToString());
            }
        }
        effects[header->GlobalSequence] = (casterEntityId, header->ActionId);
        var animationTarget = Id(header->AnimationTargetId);
        timeline!.Add("ability", casterEntityId, NameOf(casterEntityId), header->ActionId.ToString(), ActionName(header->ActionId),
            animationTarget, NameOf(animationTarget), caster != null ? (Vector3?)caster->Position : null, MathUtil.DequantizeRotation(header->RotationInt),
            $"hits={hits.Count}" + (hits.Count > 0 ? $" [{string.Join(" ", hits)}]" : ""),
            extra: new Dictionary<string, string>
            {
                ["anim_lock"] = SimTimeline.F(header->AnimationLock),
                ["anim_var"] = header->AnimationVariation.ToString(),
                ["damage_type"] = string.Join("/", damageTypes.Distinct()),
                ["knockback"] = string.Join("/", knockbacks.Distinct()),
                ["targets"] = string.Join(" ", targets),
            });
    }

    private void OnActorCast(uint entityId, ActorCastPacket* packet)
    {
        castHook!.Original(entityId, packet);
        if (!Begin()) return;
        var position = new Vector3(MathUtil.DequantizePosition(packet->PositionX), MathUtil.DequantizePosition(packet->PositionY),
            MathUtil.DequantizePosition(packet->PositionZ));
        var rotation = MathUtil.DequantizeRotation(packet->RotationInt);
        var actorRotation = Plugin.ObjectTable.SearchByEntityId(entityId)?.Rotation ?? rotation;
        var target = packet->TargetEntityId is var t and not NoTarget ? t : 0;
        var ballista = packet->BallistaEntityId is var b and not NoTarget ? b : 0;
        timeline!.Add("cast", entityId, NameOf(entityId), packet->ActionId_2.ToString(), ActionName(packet->ActionId_2), target, NameOf(target),
            position, rotation,
            $"cast={SimTimeline.F(packet->CastTime)}s interruptible={(packet->Interruptible ? 1 : 0)} rel_rot={SimTimeline.F(float.Ieee754Remainder(rotation - actorRotation, MathF.Tau))}",
            extra: new Dictionary<string, string>
            {
                ["omen_delay"] = SimTimeline.F(packet->OmenDelay / 10f, "F1"),
                ["ballista"] = ballista != 0 ? ballista.ToString("X8") : "",
            });
    }

    private void OnEffectResult(uint entityId, EffectResultBasicPacket* packet, byte replay)
    {
        effectResultHook!.Original(entityId, packet, replay);
        if (!Begin()) return;
        if (entityId >> 28 != 1 || !effects.TryGetValue(packet->GlobalSequence, out var effect)) return;
        timeline!.Add("impact", effect.Caster, NameOf(effect.Caster), effect.ActionId.ToString(), ActionName(effect.ActionId), entityId, NameOf(entityId),
            detail: $"hp={packet->CurrentHP} index={packet->TargetIndex}");
    }

    private void OnMapEffect(EventFramework* framework, MapEffectPacket* packet)
    {
        mapEffectHook!.Original(framework, packet);
        if (!Begin()) return;
        var flags = (uint)packet->State | ((uint)packet->Timeline << 16);
        timeline!.Add("map_effect", id: packet->Index.ToString(), name: "MapEffect",
            detail: $"state={packet->State} timeline={packet->Timeline} flags=0x{flags:X} director=0x{packet->DirectorId:X}");
    }

    private void OnMapEffectSnapshot(EventFramework* framework, MapEffectSnapshotPacket* packet)
    {
        mapEffectSnapshotHook!.Original(framework, packet);
        if (!Begin()) return;
        var states = new List<ushort>(SnapshotStates);
        for (var i = 0; i < SnapshotStates; i++) states.Add(packet->States[i]);
        while (states.Count > 0 && states[^1] == 0) states.RemoveAt(states.Count - 1);
        timeline!.Add("map_effect_snapshot", id: packet->Header.ToString(), name: "MapEffectSnapshot",
            detail: $"states={string.Join(",", states)} director=0x{packet->DirectorId:X}");
    }

    private void OnDirectorVars(uint directorId, byte sequence, byte unknown, byte* data, ulong length)
    {
        directorVarsHook!.Original(directorId, sequence, unknown, data, length);
        if (!Begin()) return;
        var bytes = new ReadOnlySpan<byte>(data, (int)Math.Min(length, (ulong)PacketDispatcherPointers.DirectorVarsDataLength));
        timeline!.Add("director_data", id: sequence.ToString(), detail: $"unknown={unknown} data={Convert.ToHexString(bytes)} director=0x{directorId:X}");
    }

    private void OnEventPlay(GameObjectId objectId, EventId eventId, short scene, ulong sceneFlags, uint* sceneData, byte sceneDataCount)
    {
        eventPlayHook!.Original(objectId, eventId, scene, sceneFlags, sceneData, sceneDataCount);
        if (!Begin()) return;
        var data = new uint[sceneDataCount];
        for (var i = 0; i < sceneDataCount; i++) data[i] = sceneData[i];
        var actor = Id(objectId);
        timeline!.Add("event_play", actor, NameOf(actor), $"0x{eventId.Id:X}", detail: $"scene={scene} flags=0x{sceneFlags:X}", p: SimTimeline.Params(data));
    }

    private void OnSpawnNpc(uint targetId, SpawnNpcPacket* packet)
    {
        spawnNpcHook!.Original(targetId, packet);
        if (!Begin()) return;
        ref var c = ref packet->Common;
        var name = Natives.Data.BNpcName(c.NameId) ?? c.NameString;
        names[targetId] = name;
        var owner = c.OwnerId is var o and not NoTarget ? o : 0;
        timeline!.Add("spawn", targetId, name, c.BaseId.ToString(), "BNpcBase", owner, NameOf(owner), c.Position, MathUtil.DequantizeRotation(c.Rotation),
            $"bnpcname={c.NameId} display_flags=0x{c.DisplayFlags:X} layout=0x{c.LayoutId:X} model={c.ModelChara} level={c.Level} battalion={c.Battalion} "
            + $"hidden={(c.IsHidden ? 1 : 0)} hp={c.HealthPoints}/{c.MaxHealthPoints} sub_kind={c.SubKind} event_state={c.EventState} "
            + $"mode=0x{(byte)c.CharacterMode:X} mode_param={c.ModeParam} mode_attr=0x{c.ModelAttributeFlags:X} model_state=0x{c.ModelState:X} model_scale={c.ModelScaleId} "
            + $"gimmick=0x{packet->GimmickId:X} event_npc=0x{packet->EventNpcInstanceId:X} tether={c.TetherId}->0x{c.TetherTargetId:X}");
    }

    private void OnSpawnObject(uint targetId, SpawnObjectPacket* packet)
    {
        spawnObjectHook!.Original(targetId, packet);
        if (!Begin()) return;
        var entityId = packet->EntityId != 0 ? packet->EntityId : targetId;
        var name = $"EObj{packet->BaseId}";
        names[entityId] = name;
        var position = new Vector3(packet->PositionX, packet->PositionY, packet->PositionZ);
        timeline!.Add("eobj_spawn", entityId, name, packet->BaseId.ToString(), "EObj", position: position, rotation: MathUtil.DequantizeRotation(packet->Rotation),
            detail: $"layout=0x{packet->LayoutId:X} event=0x{packet->EventId.Id:X} owner=0x{packet->OwnerId:X} gimmick=0x{packet->GimmickId:X} "
                    + $"radius={SimTimeline.F(packet->Radius)} event_state={packet->EventState} targetable={packet->TargetableStatus} "
                    + $"visibility={packet->Visibility} index={packet->ObjectIndex} kind={packet->ObjectKind} arg1=0x{packet->Arg1:X} arg2=0x{packet->Arg2:X}");
    }

    private void OnSpawnPlayer(uint targetId, SpawnPlayerPacket* packet)
    {
        spawnPlayerHook!.Original(targetId, packet);
        if (!Begin()) return;
        ref var c = ref packet->Common;
        var name = PlayerLabel(targetId);
        timeline!.Add("spawn_player", targetId, name, c.ClassJobId.ToString(), "ClassJob", position: c.Position, rotation: MathUtil.DequantizeRotation(c.Rotation),
            detail: $"level={c.Level} hp={c.HealthPoints}/{c.MaxHealthPoints} world={packet->HomeWorldId}");
    }

    private void OnActorMove(uint entityId, ActorMovePacket* packet)
    {
        moveHook!.Original(entityId, packet);
        if (!Begin()) return;
        var position = new Vector3(MathUtil.DequantizePosition(packet->PositionX), MathUtil.DequantizePosition(packet->PositionY),
            MathUtil.DequantizePosition(packet->PositionZ));
        timeline!.Add("move", entityId, NameOf(entityId), position: position, rotation: MathUtil.DequantizeRotation(packet->RotationInt),
            detail: $"flags=0x{packet->MoveFlags:X} state={packet->State} speed={packet->Speed}");
    }

    private void OnActorSetPos(uint entityId, ActorSetPosPacket* packet)
    {
        setPosHook!.Original(entityId, packet);
        if (!Begin()) return;
        timeline!.Add("set_pos", entityId, NameOf(entityId), position: new Vector3(packet->X, packet->Y, packet->Z),
            rotation: MathUtil.DequantizeRotation(packet->RotationInt), detail: $"type={packet->Type} flags=0x{packet->Flags:X}");
    }

    private void OnNpcKnockback(uint entityId, NpcKnockbackPacket* packet)
    {
        knockbackHook!.Original(entityId, packet);
        if (!Begin()) return;
        var position = new Vector3(MathUtil.DequantizePosition(packet->PositionX), MathUtil.DequantizePosition(packet->PositionY),
            MathUtil.DequantizePosition(packet->PositionZ));
        timeline!.Add("npc_knockback", entityId, NameOf(entityId), position: position, rotation: MathUtil.DequantizeRotation(packet->RotationInt),
            detail: $"dur={SimTimeline.F(packet->DurationSeconds)}");
    }

    private void OnWeather(WeatherManager* manager, WeatherChangePacket* packet)
    {
        weatherHook!.Original(manager, packet);
        if (!Begin()) return;
        timeline!.Add("weather", id: packet->WeatherId.ToString(), name: "Weather",
            detail: $"transition={SimTimeline.F(packet->TransitionTime, "F1")} forced={packet->Forced & 1}");
    }

    private void OnDutyStarted(IDutyStateEventArgs args) => timeline?.Add("duty", id: "start");
    private void OnDutyWiped(IDutyStateEventArgs args) => timeline?.Add("duty", id: "wipe");
    private void OnDutyCompleted(IDutyStateEventArgs args) => timeline?.Add("duty", id: "clear");

    // ── Helpers ───────────────────────────────────────────────────────────────

    // A handler ran for the packet being dispatched, so it needs no raw row.
    private bool Begin()
    {
        handled = true;
        return timeline != null;
    }

    private static uint LocalPlayerId => Plugin.ObjectTable.LocalPlayer?.EntityId ?? 0;

    private static uint Id(GameObjectId id) => id.ObjectId is var objectId and not (0 or NoTarget) ? objectId : 0;

    // Players are numbered in the order the recording meets them; their names never reach a file
    // that may be sent to someone else.
    private string PlayerLabel(uint entityId)
    {
        if (!names.TryGetValue(entityId, out var label))
            names[entityId] = label = $"player {names.Keys.Count(id => id >> 28 == 1) + 1}";
        return label;
    }

    private string NameOf(uint entityId)
    {
        if (entityId is 0 or NoTarget) return "";
        if (entityId >> 28 == 1) return PlayerLabel(entityId);
        if (names.TryGetValue(entityId, out var name)) return name;
        name = Plugin.ObjectTable.SearchByEntityId(entityId)?.Name.TextValue ?? "";
        if (name.Length > 0) names[entityId] = name;
        return name;
    }

    private static string ActionName(uint actionId) => Natives.Data.Action(actionId)?.Name ?? "";
}
