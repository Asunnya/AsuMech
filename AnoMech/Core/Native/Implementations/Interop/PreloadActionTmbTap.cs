#if DEBUG
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;

namespace AnoMech.Core.Native.Implementations.Interop;

// Read-only tap on ActionTimelineManager.PreloadActionTmb: logs every call with its native callers,
// to find out what makes the game preload a timeline (packet handler, spawn, zone init).
// The hook trampoline and the managed detour have no usable unwind chain back into the game, so
// callers come from a heuristic stack scan: every stack qword that points into the game's .text
// right after a call instruction. Expect the odd stale false positive deeper down.
internal sealed unsafe class PreloadActionTmbTap : IDisposable
{
    private const int MaxCallers = 10;
    private const int ScanBytes = 0x4000;

    private readonly Hook<ActionTimelineManager.Delegates.PreloadActionTmb>? hook;
    private readonly nint textStart;
    private readonly nint textEnd;
    private readonly nint moduleBase;

    public PreloadActionTmbTap()
    {
        moduleBase = Plugin.SigScanner.Module.BaseAddress;
        textStart = Plugin.SigScanner.TextSectionBase;
        textEnd = textStart + Plugin.SigScanner.TextSectionSize;
        try
        {
            hook = Plugin.GameInterop.HookFromAddress<ActionTimelineManager.Delegates.PreloadActionTmb>(
                ActionTimelineManager.Addresses.PreloadActionTmb.Value, Detour);
            hook.Enable();
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[PreloadTmb] tap not installed: {e.Message}");
        }
    }

    public void Dispose()
    {
        hook?.Disable();
        hook?.Dispose();
    }

    private bool Detour(ActionTimelineManager* manager, ActionTimelineManager.PreloadActionTmbInfo* info)
    {
        var result = hook!.Original(manager, info);
        try
        {
            var key = info->Key.ToString();
            var index = info->Index;
            var kind = index >= 0x20000 ? $"WeaponTimeline {index - 0x20000}" : $"ActionTimeline {index}";
            Plugin.Log.Info($"[PreloadTmb] territory {Plugin.ClientState.TerritoryType} {kind} \"{key}\" -> {result}; "
                            + $"callers {string.Join(" ", ScanCallers())}");
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[PreloadTmb] tap failed: {e.Message}");
        }
        return result;
    }

    private List<string> ScanCallers()
    {
        var callers = new List<string>();
        GetCurrentThreadStackLimits(out _, out var high);
        var here = 0UL;
        var cursor = (ulong)&here & ~7UL;
        var end = Math.Min(high, cursor + ScanBytes);
        for (; cursor + 8 <= end && callers.Count < MaxCallers; cursor += 8)
        {
            var value = *(nint*)cursor;
            if (IsReturnAddress(value))
                callers.Add($"+0x{(long)(value - moduleBase):X}");
        }
        return callers;
    }

    private bool IsReturnAddress(nint address)
    {
        if (address < textStart + 7 || address >= textEnd) return false;
        var p = (byte*)address;
        if (p[-5] == 0xE8) return true;
        // FF /2: call r/m64 in its 2-, 3- (disp8 or REX), 6- and 7-byte (disp32) encodings.
        foreach (var length in stackalloc[] { 2, 3, 6, 7 })
            if (p[-length] == 0xFF && ((p[-length + 1] >> 3) & 7) == 2)
                return true;
        return false;
    }

    [DllImport("kernel32.dll")]
    private static extern void GetCurrentThreadStackLimits(out ulong lowLimit, out ulong highLimit);
}
#endif
