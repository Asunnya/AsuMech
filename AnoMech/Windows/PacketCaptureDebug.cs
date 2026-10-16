#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using AnoMech.Core.Native.Implementations.Pointers;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace AnoMech.Windows;

// Logs every zone packet the live dispatcher receives, before the Duty Recorder picks the ones it
// writes, so packets that never reach a replay can be read from real content. One line per packet:
// ms since start, opcode, target entity, payload size, payload hex.
internal sealed unsafe class PacketCaptureDebug : IDisposable
{
    // The IPC header in front of the payload; the opcode is its u16 at +2.
    private const int IpcHeaderSize = 0x10;

    // The packet's length isn't passed in (the u32 before the IPC header reads 0x20 for every
    // packet), so a fixed span is dumped; shorter packets run on into the next one.
    private const int DumpBytes = 0x100;

    private delegate void OnReceivePacketDelegate(PacketDispatcher* self, uint targetId, byte* packet);

    private readonly Hook<OnReceivePacketDelegate> hook;
    private readonly Stopwatch clock = new();
    private StreamWriter? writer;
    private string? path;
    private long packets;
    private HashSet<uint> statuses = [];

    // The local player's transform, at most every PositionInterval and only when it changed.
    private const long PositionIntervalMs = 100;
    private long lastPositionMs = -PositionIntervalMs;
    private Vector3 lastPosition;
    private float lastRotation;

    private void WritePosition(long ms, Vector3 position, float rotation)
    {
        if (ms - lastPositionMs < PositionIntervalMs) return;
        if (Vector3.DistanceSquared(position, lastPosition) < 0.0001f && MathF.Abs(rotation - lastRotation) < 0.001f) return;
        lastPositionMs = ms;
        lastPosition = position;
        lastRotation = rotation;
        writer!.WriteLine($"{ms} POS {position.X:F2} {position.Z:F2} {rotation:F3}");
    }
    private readonly HashSet<ConditionFlag> conditions = [];

    public PacketCaptureDebug()
    {
        hook = Plugin.GameInterop.HookFromAddress<OnReceivePacketDelegate>(
            (nint)PacketDispatcher.StaticVirtualTablePointer->OnReceivePacket, OnReceivePacket);
        InstallCompletionLog();
    }

    public void Dispose()
    {
        Stop();
        hook.Dispose();
        completeHook?.Dispose();
    }

    public void Draw()
    {
        ImGui.TextUnformatted("Packet capture (every received zone packet, incl. ones replays skip)");
        ImGui.Separator();
        if (writer == null)
        {
            if (ImGui.Button("Start capture##packetcapture")) Start();
        }
        else
        {
            if (ImGui.Button("Stop capture##packetcapture")) Stop();
            ImGui.SameLine();
            ImGui.TextUnformatted($"{packets} packets, {clock.Elapsed:mm\\:ss}");
            if (ImGui.Button("Mark##packetcapture")) writer.WriteLine($"{clock.ElapsedMilliseconds} MARK");
        }
        if (path != null) ImGui.TextWrapped(path);
        DrawForcedFollow();
    }

    private void DrawForcedFollow() => ImGui.TextUnformatted($"Forced follow: {DescribeController(Controller)}");

    // The client's forced-follow controller, which the forced-follow handler writes: target entity
    // at +0x08, the active mode at +0x38 (null when idle; +0x40 approach, +0x50 position,
    // +0x68 follow), the packet's flag at +0x70. +0x00 range and +0x04 entity belong to the
    // approach mode; a completion resets them to -1 / E0000000.
    private static byte* Controller
    {
        get
        {
            var handler = (byte*)Marshal.GetFunctionPointerForDelegate(PacketDispatcherPointers.HandleForcedFollowPacket);
            // The handler's `lea rcx, [rip + disp]` sits at +7.
            return handler + 14 + *(int*)(handler + 10);
        }
    }

    private static string DescribeController(byte* controller)
    {
        var mode = *(byte**)(controller + 0x38);
        var modeName = mode == null ? "idle"
            : mode == controller + 0x40 ? "approach"
            : mode == controller + 0x50 ? "position"
            : mode == controller + 0x68 ? "follow"
            : $"?{(nint)mode:X}";
        var target = *(uint*)(controller + 0x08);
        var approach = *(uint*)(controller + 0x04);
        return $"mode={modeName} target={target:X8} ({TargetSummary(target)}) flag={*(uint*)(controller + 0x70)} "
               + $"range@0={*(float*)controller:F2} entity@4={approach:X8} ({TargetSummary(approach)})";
    }

    private static string TargetSummary(uint entityId)
    {
        if (entityId is 0 or 0xE0000000) return "none";
        var target = Plugin.ObjectTable.SearchByEntityId(entityId);
        if (target == null) return "not in object table";
        var player = Plugin.ObjectTable.LocalPlayer;
        var distance = player == null ? float.NaN : Vector3.Distance(player.Position, target.Position);
        // The follow's target lookup refuses an object with bit 4 of +0x190 set.
        var flags190 = *((byte*)target.Address + 0x190);
        var facing = "";
        if (player != null)
        {
            var heading = MathF.Atan2(target.Position.X - player.Position.X, target.Position.Z - player.Position.Z);
            var off = MathF.Abs(MathF.IEEERemainder(heading - player.Rotation, MathF.Tau));
            facing = $" facing={player.Rotation:F2} toTarget={heading:F2} off={off:F2}";
        }
        return $"{target.Name} {distance:F1}y hitbox={target.HitboxRadius:F1} kind={target.ObjectKind} +0x190={flags190:X2}{facing}";
    }

    // The controller's completion: a mode's step reported done; the client tells the server and
    // drops the mode. Logged before the original clears it.
    private delegate void CompleteForcedMoveDelegate(byte* controller);

    private Hook<CompleteForcedMoveDelegate>? completeHook;

    private void InstallCompletionLog()
    {
        var scanner = Plugin.SigScanner;
        if (!scanner.TryScanText("40 53 48 81 EC 80 0F 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 70 0F 00 00 48 8B D9 48 8B 49 38 48 8D 43 40", out var address))
        {
            Plugin.Log.Warning("[ForcedFollow] completion signature not found");
            return;
        }
        completeHook = Plugin.GameInterop.HookFromAddress<CompleteForcedMoveDelegate>(address, OnCompleteForcedMove);
        completeHook.Enable();
    }

    private void OnCompleteForcedMove(byte* controller)
    {
        var description = DescribeController(controller);
        Plugin.Log.Info($"[ForcedFollow] complete: {description}");
        writer?.WriteLine($"{clock.ElapsedMilliseconds} FOLLOW_DONE {description}");
        completeHook!.Original(controller);
    }

    private void Start()
    {
        var dir = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "captures");
        Directory.CreateDirectory(dir);
        path = Path.Combine(dir, $"packets_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        writer = new StreamWriter(path, false, Encoding.ASCII, 1 << 20);
        writer.WriteLine($"# local player {Plugin.ObjectTable.LocalPlayer?.EntityId:X8}");
        statuses = [];
        conditions.Clear();
        lastPositionMs = -PositionIntervalMs;
        packets = 0;
        clock.Restart();
        hook.Enable();
    }

    private void Stop()
    {
        hook.Disable();
        writer?.Flush();
        writer?.Dispose();
        writer = null;
        clock.Stop();
    }

    private void OnReceivePacket(PacketDispatcher* self, uint targetId, byte* packet)
    {
        try
        {
            if (writer != null && packet != null) Write(targetId, packet);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "[PacketCapture] write failed");
        }
        hook.Original(self, targetId, packet);
    }

    private void Write(uint targetId, byte* packet)
    {
        var opcode = *(ushort*)(packet + 2);
        var line = new StringBuilder(32 + DumpBytes * 2);
        line.Append(clock.ElapsedMilliseconds).Append(' ')
            .Append(opcode.ToString("X4")).Append(' ')
            .Append(targetId.ToString("X8")).Append(' ');
        var payload = packet + IpcHeaderSize;
        for (var i = 0; i < DumpBytes; i++) line.Append(payload[i].ToString("x2"));
        writer!.WriteLine(line);
        packets++;
    }

    // Status and condition edges on the local player, so the packets that caused them can be found
    // without marking by hand.
    public void Tick()
    {
        if (writer == null) return;
        var ms = clock.ElapsedMilliseconds;
        if (Plugin.ObjectTable.LocalPlayer is { } player)
        {
            var now = new HashSet<uint>();
            foreach (var status in player.StatusList)
                if (status.StatusId != 0) now.Add(status.StatusId);
            foreach (var id in now.Except(statuses)) writer.WriteLine($"{ms} STATUS +{id}");
            foreach (var id in statuses.Except(now)) writer.WriteLine($"{ms} STATUS -{id}");
            statuses = now;
            WritePosition(ms, player.Position, player.Rotation);
        }
        foreach (ConditionFlag flag in Enum.GetValues<ConditionFlag>())
        {
            var on = Plugin.Condition[flag];
            if (on == conditions.Contains(flag)) continue;
            writer.WriteLine($"{ms} COND {(on ? '+' : '-')}{(int)flag} {flag}");
            if (on) conditions.Add(flag);
            else conditions.Remove(flag);
        }
    }
}
#endif
