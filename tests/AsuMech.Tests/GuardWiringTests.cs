using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static AsuMech.Tests.ILRules;

namespace AsuMech.Tests;

// What the zone guard needs from the rest of the plugin: a tick every frame that nothing else can
// skip, the game events it judges, and a teardown that reaches it whatever else fails.
public class GuardWiringTests
{
    private const string Zone = "AsuMech.Core.Native.Implementations.ZoneControl";
    private const string Natives = "AsuMech.Core.Native.Interfaces.Natives";

    private static PluginIL IL => PluginIL.Build;

    private static List<Instruction> Calls(MethodDefinition m) => m.Body.Instructions.Where(i => i.Operand is MethodReference).ToList();

    private static string Name(Instruction call) => PluginIL.Name((MethodReference)call.Operand);

    private static ExceptionHandler? TryAround(MethodDefinition m, Instruction i)
        => m.Body.ExceptionHandlers.FirstOrDefault(h => i.Offset >= h.TryStart.Offset && i.Offset < h.TryEnd.Offset);

    [Test]
    public void TheGuardTicksFirstEveryFrame_InItsOwnTry()
    {
        var failures = new List<string>();
        var tick = IL.Method(Natives, "Tick");
        var guard = Calls(tick).FirstOrDefault(c => Name(c) == "AsuMech.Core.Native.Interfaces.IZoneControl::TickGuard");
        if (guard == null) AssertNone(["Natives.Tick no longer ticks the zone guard."]);
        var firstPluginCall = Calls(tick).First(c => Name(c).StartsWith("AsuMech.") && Name(c) != $"{Natives}::get_Zone");
        if (firstPluginCall != guard) failures.Add($"Natives.Tick runs {Name(firstPluginCall)} before the zone guard.");
        var guardTry = TryAround(tick, guard!);
        if (guardTry is not { HandlerType: ExceptionHandlerType.Catch })
            failures.Add("Natives.Tick does not catch around the zone guard: a throw there would stop the rest of the frame.");
        else if (Calls(tick).Any(c => c != guard && c.Offset >= guardTry.TryStart.Offset && c.Offset < guardTry.TryEnd.Offset
                                      && Name(c).StartsWith("AsuMech.") && Name(c) != $"{Natives}::get_Zone"))
            failures.Add("The zone guard shares its try with other plugin code, which could keep it from running.");

        var update = IL.Method("AsuMech.Plugin", "OnFrameworkUpdate");
        var nativesTick = Calls(update).FirstOrDefault(c => Name(c) == $"{Natives}::Tick");
        if (nativesTick == null) failures.Add("Plugin.OnFrameworkUpdate no longer calls Natives.Tick.");
        else
        {
            if (TryAround(update, nativesTick) != null) failures.Add("Plugin.OnFrameworkUpdate wraps Natives.Tick in a try that could skip it.");
            foreach (var later in new[] { "AsuMech.Core.Game.Game::Tick", "AsuMech.Multiplayer.MultiplayerManager::Tick" })
                if (Calls(update).FirstOrDefault(c => Name(c) == later) is { } call && call.Offset < nativesTick.Offset)
                    failures.Add($"Plugin.OnFrameworkUpdate runs {later} before the zone guard.");
        }
        AssertNone(failures);
    }

    [Test]
    public void GameEventsReachTheGuard()
    {
        var failures = new List<string>();
        var ctor = IL.Method("AsuMech.Plugin", ".ctor");
        foreach (var (handler, evt) in new[] { ("OnTerritoryChanged", "add_TerritoryChanged"), ("OnLogout", "add_Logout") })
            if (!ctor.Body.Instructions.Any(i => i.OpCode.Code == Code.Ldftn && i.Operand is MethodReference m && m.Name == handler && NextCall(i)?.Name == evt))
                failures.Add($"Plugin no longer subscribes {handler} to the client state's {evt[4..]} event.");
        var first = Calls(IL.Method("AsuMech.Plugin", "OnTerritoryChanged")).First();
        if (Name(first) != $"{Zone}::NoteTerritoryChanged")
            failures.Add("Plugin.OnTerritoryChanged must tell the zone guard first, before anything that could throw or return.");
        if (!IL.CallersOf($"^{Regex.Escape(Zone)}::NoteLogout$").Contains("AsuMech.Plugin::OnLogout"))
            failures.Add("Plugin.OnLogout no longer tells the zone guard.");
        if (!IL.CallersOf($"^{Regex.Escape(Zone)}::NoteActionPressed$").Contains("AsuMech.Core.Native.Implementations.LocalPlayerInputHooks::RecordRecentAction"))
            failures.Add("LocalPlayerInputHooks.RecordRecentAction no longer reports Teleport/Return presses to the zone guard.");
        var detour = Calls(IL.Method("AsuMech.Core.Native.Implementations.LocalPlayerInputHooks", "UseActionDetour")).Select(Name).ToList();
        var record = detour.FindIndex(c => c.EndsWith("::RecordRecentAction"));
        var original = detour.FindIndex(c => c.EndsWith("::get_Original"));
        if (record < 0 || original < 0 || record > original)
            failures.Add("UseActionDetour must record the press (and so latch a Teleport/Return) before it can pass the action on.");
        AssertNone(failures);

        static MethodReference? NextCall(Instruction i)
        {
            for (var n = i.Next; n != null; n = n.Next)
                if (n.Operand is MethodReference m && n.OpCode.Code is Code.Call or Code.Callvirt) return m;
            return null;
        }
    }

    // Dalamud disposes the plugin's hooks once Dispose returns or throws: a stay must be lifted or
    // the game stopped by then, whatever else in the teardown fails.
    [Test]
    public void Unload_ReachesTheZoneGuardEvenWhenTheTeardownThrows()
    {
        var dispose = IL.Method("AsuMech.Plugin", "Dispose");
        var zoneDispose = dispose.Body.Instructions.FirstOrDefault(i => i.Operand is MethodReference m && Name(i) == $"{Zone}::Dispose");
        if (zoneDispose == null) AssertNone(["Plugin.Dispose no longer disposes ZoneControl itself."]);
        var inFinally = dispose.Body.ExceptionHandlers.FirstOrDefault(h => h.HandlerType == ExceptionHandlerType.Finally
            && zoneDispose!.Offset >= h.HandlerStart.Offset && (h.HandlerEnd == null || zoneDispose.Offset < h.HandlerEnd.Offset));
        if (inFinally == null) AssertNone(["Plugin.Dispose disposes ZoneControl outside a finally: a throw earlier in the teardown would skip it."]);
        AssertNone(Calls(dispose)
            .Where(i => Name(i).StartsWith("AsuMech.") && i != zoneDispose && Name(i) is not ($"{Zone}::get_Current" or "AsuMech.Core.DiagnosticLog::Shutdown"))
            .Where(i => !(i.Offset >= inFinally!.TryStart.Offset && i.Offset < inFinally.TryEnd.Offset))
            .Select(i => $"Plugin.Dispose calls {Name(i)} outside the try its ZoneControl finally covers."));
    }
}
