using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static AsuMech.Tests.ILRules;

namespace AsuMech.Tests;

// Rules over the built plugin that keep the packet firewall's guarantees as the code grows: who
// may touch the filter hooks, who may raise or lower them, and the checks every path must pass
// first. A failure names the code that broke the rule. A new call site that is genuinely safe goes
// into the rule's allowlist, reviewed like any change to the firewall itself.
public class ZoneControlFirewallRulesTests
{
    private const string Zone = "AsuMech.Core.Native.Implementations.ZoneControl";
    private const string IZone = "AsuMech.Core.Native.Interfaces.IZoneControl";
    private const string PeerZone = "AsuMech.Multiplayer.PeerNatives/PeerZone";
    private const string RecordingZone = "AsuMech.Multiplayer.PacketRecorder/RecordingZone";
    private const string TimelineDebug = "AsuMech.Core.Native.Implementations.Interop.TimelineDebug";

    private static PluginIL IL => PluginIL.Build;

    private static readonly Dictionary<string, string[]> HookOperations = new()
    {
        [$"{Zone}::EnableFirewall"] = ["send:Enable", "receive:Enable", "receive:get_IsEnabled"],
        [$"{Zone}::DisableFirewall"] = ["send:Disable", "send:get_IsEnabled"],
        // The receive hook stays on for good; whether it filters is receiveFilterUp.
        [$"{Zone}::.ctor"] = ["receive:Enable"],
        [$"{Zone}::get_ReceiveFiltering"] = ["receive:get_IsEnabled", "receive:get_IsDisposed"],
        [$"{Zone}::DispatchUnfiltered"] = ["receive:get_OriginalDisposeSafe"],
        [$"{Zone}::DisposeHooks"] = ["send:Dispose", "receive:Dispose"],
        [$"{Zone}::HoldSendFirewall"] = ["send:Enable", "send:Disable", "send:get_IsEnabled"],
        [$"{Zone}::get_HoldsSends"] = ["send:get_IsEnabled"],
        [$"{Zone}::SendPacketDetour"] = ["send:get_Original"],
        [$"{Zone}::ReceivePacketDetour"] = ["receive:get_Original"],
        [$"{Zone}::InjectIncomingPacket"] = ["receive:get_Original"],
        [$"{Zone}::FirewallArmed"] = ["send:get_IsEnabled", "send:get_IsDisposed", "receive:get_IsEnabled", "receive:get_IsDisposed"],
        [$"{Zone}::TickSessionGuard"] = ["send:get_IsEnabled", "receive:get_IsEnabled"],
        [$"{Zone}::LiftBlockedReason"] = ["send:get_IsEnabled", "receive:get_IsEnabled"],
        [$"{Zone}::BuildStateSnapshot"] = ["send:get_IsEnabled", "send:get_IsDisposed", "receive:get_IsEnabled", "receive:get_IsDisposed"],
        [$"{Zone}::Dispose"] = ["send:Dispose", "receive:Dispose"],
    };

    [Test]
    public void TheFilterHooks_AreOnlyTouchedWhereTheirUseIsVerified()
    {
        AssertNone(Check());

        static IEnumerable<string> Check()
        {
            var fields = new Dictionary<string, string> { [$"{Zone}::sendPacketHook"] = "send", [$"{Zone}::receivePacketHook"] = "receive" };
            var seen = new HashSet<string>();
            foreach (var (m, i, f) in IL.FieldAccesses())
            {
                if (!fields.TryGetValue(PluginIL.Name(f), out var which)) continue;
                var owner = PluginIL.Owner(m);
                if (i.OpCode.Code is Code.Stfld)
                {
                    if (owner != $"{Zone}::.ctor") yield return $"{owner} assigns the {which} filter hook; only the constructor may.";
                    continue;
                }
                if (NextHookMember(i) is not { } op)
                {
                    yield return $"{owner} uses the {which} filter hook in a way this rule can't read (stored or passed on?). Keep every use a direct call on the field.";
                    continue;
                }
                seen.Add($"{owner}|{which}:{op}");
                if (!HookOperations.TryGetValue(owner, out var allowed) || !allowed.Contains($"{which}:{op}"))
                    yield return $"{owner} calls {op} on the {which} filter hook; only the methods in HookOperations may, each for the listed operations.";
            }
            foreach (var needed in new[] { $"{Zone}::EnableFirewall|send:Enable", $"{Zone}::EnableFirewall|receive:Enable", $"{Zone}::SendPacketDetour|send:get_Original" })
                if (!seen.Contains(needed)) yield return $"expected {needed.Replace("|", " to call ")} and found no such call.";
        }

        static string? NextHookMember(Instruction load)
        {
            for (var i = load.Next; i != null && i.Offset - load.Offset < 64; i = i.Next)
                if (i.Operand is MethodReference m)
                    return PluginIL.TypeName(m.DeclaringType) == "Dalamud.Hooking.Hook" ? m.Name : null;
            return null;
        }
    }

    [Test]
    public void RaisingAndLoweringTheFilter_HasExactlyTheReviewedCallers()
    {
        AssertNone(new[]
        {
            CallersMustBeIn($"{Zone}::EnableFirewall", true, $"{Zone}::Enter"),
            CallersMustBeIn($"{Zone}::DisableFirewall", true, $"{Zone}::Enter", $"{Zone}::TryLift", $"{Zone}::DisposeOnFrameworkThread"),
            CallersMustBeIn($"{Zone}::TryLift", true, $"{Zone}::LiftFirewallOrDie", $"{Zone}::TickSessionGuard"),
            CallersMustBeIn($"{Zone}::LiftFirewallOrDie", true, $"{Zone}::Revert", $"{Zone}::DisposeOnFrameworkThread"),
            CallersMustBeIn($"{Zone}::LoadZoneInternal", true, $"{Zone}::Enter", $"{Zone}::Revert"),
            CallersMustBeIn($"{Zone}::Revert", true, $"{Zone}::Enter", $"{Zone}::{IZone}.Revert", "AsuMech.Plugin::DisposeOnceOutOfStay"),
            CallersMustBeIn($"{IZone}::Revert", true, "AsuMech.Core.Game.InstanceSession::Exit", $"{PeerZone}::Revert", $"{RecordingZone}::Revert"),
            CallersMustBeIn($"{Zone}::Enter", false),
            CallersMustBeIn($"{IZone}::Enter", true, "AsuMech.Core.Game.InstanceSession::Enter", $"{PeerZone}::Enter", $"{RecordingZone}::Enter"),
            CallersMustBeIn($"{Zone}::HoldSendFirewall", true, $"{TimelineDebug}::Arm", $"{TimelineDebug}::OnUpdate", $"{TimelineDebug}::Shutdown", $"{Zone}::DisposeOnFrameworkThread"),
            CallersMustBeIn($"{IZone}::HoldSendFirewall", false, $"{PeerZone}::HoldSendFirewall", $"{RecordingZone}::HoldSendFirewall"),
            // The lift replays the party packets the filter held.
            CallersMustBeIn($"{Zone}::InjectIncomingPacket", false, $"{Zone}::TryLift"),
            CallersMustBeIn($"{IZone}::InjectIncomingPacket", false, $"{PeerZone}::InjectIncomingPacket", $"{RecordingZone}::InjectIncomingPacket"),
            CallersMustBeIn($"{Zone}::Die", true, $"{Zone}::Enter", $"{Zone}::Revert", $"{Zone}::Trip", $"{Zone}::TryLift", $"{Zone}::DisposeOnFrameworkThread"),
            CallersMustBeIn($"{Zone}::StopQuietly", true, $"{Zone}::DisposeOnFrameworkThread"),
            CallersMustBeIn($"{Zone}::SetLocalPlayerPosition", true, $"{Zone}::LoadZoneInternal", $"{Zone}::RestoreBeforeLift", $"{Zone}::HoldSendFirewall", $"{Zone}::DisposeOnFrameworkThread"),
            CallersMustBeIn("AsuMech.Core.Native.Implementations.ClientTerritory::TrySet", true, $"{Zone}::Enter", $"{Zone}::Revert"),
        }.SelectMany(x => x));
    }

    // A second hook could pass traffic around the filter, or act on the character from a detour.
    private static readonly string[] HookSites =
    [
        $"{Zone}::.ctor",
        "AsuMech.Core.Native.Implementations.MapEffects::.ctor",
        "AsuMech.Core.Native.Implementations.LocalPlayerInputHooks::.ctor",
        "AsuMech.Core.Native.Implementations.Interop.PreloadActionTmbTap::.ctor",
        // Observes inbound packets for the pull recording and calls each original unchanged.
        "AsuMech.Core.Native.Implementations.PullRecorder::TryHook",
        // Observe the party's packets and handlers and call each original unchanged.
        "AsuMech.Core.Native.Implementations.ServerPartyWatch::.ctor",
        "AsuMech.Core.Native.Implementations.ServerPartyWatch::HookOrWarn",
        // Debug windows.
        "AsuMech.Windows.DebugMenu::TogglePositionLog",
        "AsuMech.Windows.DebugMenu::InstallRecastTimesTap",
        "AsuMech.Windows.PacketCaptureDebug::.ctor",
        "AsuMech.Windows.PacketCaptureDebug::InstallCompletionLog",
    ];

    [Test]
    public void Hooks_AreOnlyCreatedAtReviewedSites()
    {
        var failures = new List<string>();
        foreach (var (m, _, callee) in IL.Calls())
        {
            var name = PluginIL.Name(callee);
            var owner = PluginIL.Owner(m);
            if (Regex.IsMatch(name, @"IGameInteropProvider::HookFrom") && !HookSites.Contains(owner))
                failures.Add($"{owner} creates a hook ({callee.Name}); hooks may only be created at: {string.Join(", ", HookSites)}.");
            if (name.EndsWith("IGameInteropProvider::InitializeFromAttributes")
                && !(owner.StartsWith("AsuMech.Core.Native.Implementations.Pointers.") && owner.EndsWith("::Initialize"))
                && owner is not ("AsuMech.Core.Native.Implementations.LocalPlayerInputHooks::.ctor" or "AsuMech.Windows.LockReplayDebug::.ctor"))
                failures.Add($"{owner} initializes signature attributes; only the Pointers classes, LocalPlayerInputHooks and LockReplayDebug may.");
        }
        // Read from the raw attribute blob: decoding it would need Dalamud's own assemblies.
        var detourName = Encoding.UTF8.GetBytes("DetourName");
        foreach (var t in IL.Module.GetTypes())
        foreach (var member in t.Fields.Cast<IMemberDefinition>().Concat(t.Properties).Concat(t.Methods))
        foreach (var a in member.CustomAttributes.Where(a => a.AttributeType.Name == "SignatureAttribute"))
            if (a.GetBlob().AsSpan().IndexOf(detourName) >= 0)
                failures.Add($"{t.FullName}.{member.Name} declares a signature-attribute hook (DetourName); create hooks at a reviewed site instead.");
        AssertNone(failures);
    }

    // Hooks on the receive function other than the filter's only watch: each calls its original.
    private static readonly string[] ReceiveFunctionReaders =
    [
        $"{Zone}::.ctor",
        "AsuMech.Core.Native.Implementations.PullRecorder::.ctor",
        "AsuMech.Windows.PacketCaptureDebug::.ctor",
    ];

    [Test]
    public void TheFilteredFunctions_AreOnlyLocatedByReviewedCode()
    {
        const string sendSignature = "48 89 5C 24 ?? 48 89 74 24 ?? 4C 89 64 24 ?? 55 41 56 41 57 48 8B EC 48 83 EC 70";
        var failures = new List<string>();
        var found = false;
        foreach (var m in IL.Methods)
        foreach (var i in m.Body.Instructions)
        {
            var owner = PluginIL.Owner(m);
            if (i.Operand is string s && s == sendSignature)
            {
                found = true;
                if (owner != $"{Zone}::.ctor") failures.Add($"{owner} scans for the filtered send function; only ZoneControl may.");
            }
            if (i.Operand is MemberReference r && r.Name.Contains("StaticVirtualTablePointer") && r.DeclaringType.Name == "PacketDispatcher" && !ReceiveFunctionReaders.Contains(owner))
                failures.Add($"{owner} reads the packet dispatcher's vtable (the filtered receive function); only {string.Join(", ", ReceiveFunctionReaders)} may.");
        }
        if (!found) failures.Add("ZoneControl no longer scans for the send signature this rule knows; update it with the new one.");
        AssertNone(failures);
    }

    // The inbound allowlist decides which server packets reach a client in the sim.
    [Test]
    public void TheFilterConfiguration_IsOnlyWrittenByItsOwners()
    {
        const string updater = "AsuMech.Core.Native.Implementations.Interop.OpcodeUpdater::DownloadOpcodes";
        const string keeper = "AsuMech.Core.Native.Implementations.Interop.OpcodeUpdater::KeepStoredOpcodes";
        AssertNone(new[]
        {
            CallersMustBeIn("AsuMech.Configuration::set_ZoneDownOpcodes", true, updater),
            CallersMustBeIn("AsuMech.Configuration::set_ZoneUpOpcode", true, updater),
            CallersMustBeIn("AsuMech.Configuration::set_ZoneFirewallGameVersion", true, updater, keeper),
            CallersMustBeIn("AsuMech.Configuration::set_ZoneFirewallOpcodesFrom", true, updater, keeper),
            CallersMustBeIn("AsuMech.Configuration::set_SafeMode", false, "AsuMech.Windows.ConfigWindow::DrawContent"),
        }.SelectMany(x => x));
    }

    // ---- the safety stop ----------------------------------------------------------------------

    [Test]
    public void OnlyTheSafetyStop_EndsTheProcess()
    {
        AssertNone(IL.Calls()
            .Where(c => Regex.IsMatch(PluginIL.Name(c.Callee), @"^System\.Environment::(FailFast|Exit)$|^System\.Diagnostics\.Process::Kill$"))
            .Select(c => PluginIL.Owner(c.Method)).Where(o => o is not ($"{Zone}::Die" or $"{Zone}::StopQuietly")).Distinct()
            .Select(o => $"{o} ends the process; only ZoneControl.Die and StopQuietly may (they log first and never lower the filter)."));
    }

    // Callers fall through after Die and StopQuietly on the understanding that neither returns.
    [TestCase("Die")]
    [TestCase("StopQuietly")]
    public void TheSafetyStop_NeverReturns(string stop)
    {
        var method = IL.Method(Zone, stop);
        var escapes = new ControlFlow(method, "System.Environment::FailFast").Reachable()
            .Where(i => i.OpCode.Code is Code.Ret or Code.Throw or Code.Rethrow).ToList();
        var failFast = method.Body.Instructions.Where(CallTo("System.Environment::FailFast")).ToList();
        var failures = new List<string>();
        if (failFast.Count == 0) failures.Add($"ZoneControl.{stop} no longer calls Environment.FailFast.");
        if (escapes.Count > 0) failures.Add($"ZoneControl.{stop} can return or throw without reaching Environment.FailFast (at {string.Join(", ", escapes.Select(e => $"IL_{e.Offset:X4}"))}).");
        foreach (var f in failFast)
            if (method.Body.ExceptionHandlers.Any(h => h.HandlerType == ExceptionHandlerType.Catch && f.Offset >= h.TryStart.Offset && f.Offset < h.TryEnd.Offset))
                failures.Add($"ZoneControl.{stop} calls FailFast inside a try with a catch.");
        AssertNone(failures);
    }

    // ---- what each path must pass first ---------------------------------------------------------

    // During a stay nothing but the heartbeat leaves the client.
    [Test]
    public void TheSendFilter_PassesOnlyTheHeartbeat()
    {
        var detour = IL.Method(Zone, "SendPacketDetour");
        var failures = new List<string>();
        var compares = detour.Body.Instructions.Where(LoadOf($"{Zone}::heartbeatOpcode")).ToList();
        var originals = detour.Body.Instructions.Where(CallTo("Dalamud.Hooking.Hook::get_Original")).ToList();
        if (compares.Count != 1 || originals.Count == 0)
        {
            AssertNone([$"SendPacketDetour: expected one heartbeat comparison and a call to the original, found {compares.Count} and {originals.Count}; update this rule."]);
            return;
        }
        if (OpcodeMatches(compares[0]) is not { } matches)
        {
            AssertNone(["SendPacketDetour compares the heartbeat opcode in a shape this rule can't read; keep it `if (opcode == heartbeatOpcode)`."]);
            return;
        }
        var reached = new ControlFlow(detour).FeasiblyReachableAvoiding([matches]);
        failures.AddRange(originals.Where(reached.Contains).Select(o => $"SendPacketDetour reaches the original send (IL_{o.Offset:X4}) for an opcode other than the heartbeat."));
        AssertNone(failures);

        // The edge taken when the opcode equals the heartbeat's.
        static (Instruction, Instruction)? OpcodeMatches(Instruction load)
        {
            var next = load.Next;
            return next.OpCode.Code switch
            {
                Code.Bne_Un or Code.Bne_Un_S => (next, next.Next),
                Code.Beq or Code.Beq_S => (next, (Instruction)next.Operand),
                Code.Ceq when ControlFlow.OutcomeOf(next) is { } o => (o.Branch, o.WhenTruthy),
                _ => null,
            };
        }
    }

    [Test]
    public void Enter_RaisesTheFilterOnlyPastTheGate_AndLoadsOnlyWithItConfirmedUp()
    {
        var enter = IL.Method(Zone, "Enter");
        var gate = CallTo($"{Zone}::StartBlockedReason");
        AssertNone(EveryPathPasses(enter, gate, "StartBlockedReason", false, CallTo($"{Zone}::EnableFirewall"), "EnableFirewall")
            .Concat(EveryPathPasses(enter, gate, "StartBlockedReason", false, CallTo($"{Zone}::LoadZoneInternal"), "LoadZoneInternal"))
            .Concat(EveryPathPasses(enter, CallTo($"{Zone}::FirewallArmed"), "FirewallArmed", true, CallTo($"{Zone}::LoadZoneInternal"), "LoadZoneInternal", $"{Zone}::Die")));
    }

    // What the lift releases (the filter, the character's held conditions, the action counters)
    // goes only once the inn, the position and the hooks check out.
    [TestCase("DisableFirewall")]
    [TestCase("ReleasePlayerConditions")]
    [TestCase("RestoreClientCounters")]
    [TestCase("OpenSocialForPartyResync")]
    public void TryLift_ReleasesOnlyWhenNothingBlocksTheLift(string release)
    {
        AssertNone(EveryPathPasses(IL.Method(Zone, "TryLift"), CallTo($"{Zone}::LiftBlockedReason"), "LiftBlockedReason", false,
            CallTo($"{Zone}::{release}"), release, $"{Zone}::Die"));
    }

    // Outside a session the saved inn state is stale: a reload or restore would move the real
    // character with no filter up.
    [TestCase($"{Zone}::LoadZoneInternal")]
    [TestCase("AsuMech.Core.Native.Implementations.ClientTerritory::TrySet")]
    [TestCase("System.Threading.Tasks.Task::Delay")]
    public void Revert_DoesNothingOutsideASession(string target)
    {
        AssertNone(EveryPathPasses(IL.Method(Zone, "Revert"), CallTo($"{Zone}::get_IsActive"), "IsActive", true,
            CallTo(target), target, $"{Zone}::Die"));
    }

    // The delayed restore runs a second after the reload; by then an unload may have lifted the
    // filter, and the move would reach the server.
    [Test]
    public void TheDelayedLift_RestoresOnlyWhileTheGuardHoldsTheStay()
    {
        var lambdas = IL.Methods.Where(m => PluginIL.Owner(m) == $"{Zone}::Revert" && m.Body.Instructions.Any(CallTo($"{Zone}::RestoreBeforeLift"))).ToList();
        Assert.That(lambdas, Has.Count.EqualTo(1), "Revert's delayed lift no longer calls RestoreBeforeLift from one callback; update this rule.");
        AssertNone(EveryPathPasses(lambdas[0], LoadOf($"{Zone}::guardArmed"), "guardArmed", true, CallTo($"{Zone}::RestoreBeforeLift"), "RestoreBeforeLift"));
    }

    // The debug send hold belongs to the inn: during a stay and its lift the guard owns the hooks.
    [TestCase("get_IsActive", true)]
    [TestCase("guardArmed", false)]
    public void TheDebugHold_LeavesTheHooksAloneDuringAStay(string check, bool isCall)
    {
        var hold = IL.Method(Zone, "HoldSendFirewall");
        var guard = isCall ? CallTo($"{Zone}::{check}") : LoadOf($"{Zone}::{check}");
        var hookChange = (Func<Instruction, bool>)(i => i.Operand is MethodReference m && PluginIL.TypeName(m.DeclaringType) == "Dalamud.Hooking.Hook" && m.Name is "Enable" or "Disable");
        AssertNone(EveryPathPasses(hold, guard, check, false, hookChange, "a send hook Enable/Disable"));
    }

    // A replayed server packet bypasses the receive filter and runs the client's own handlers;
    // outside a session the server never sent it and nothing would undo what it did. The lift's
    // replay of the packets the filter held is the one exception.
    [Test]
    public void InjectedPackets_ReachTheClientOnlyDuringASession()
    {
        var sessionOrReplay = (Func<Instruction, bool>)(i => CallTo($"{Zone}::get_IsActive")(i) || LoadOf($"{Zone}::replayingHeldPackets")(i));
        AssertNone(EveryPathPasses(IL.Method(Zone, "InjectIncomingPacket"), sessionOrReplay, "IsActive or the lift's replay", true,
            CallTo("Dalamud.Hooking.Hook::get_Original"), "the original receive"));
    }

    // A lift that fails part-way leaves the send filter up: nothing the client does reaches the server.
    // The receive hook stays on; the filter is down once receiveFilterUp is cleared.
    [Test]
    public void DisableFirewall_LowersTheSendFilterOnlyOnceTheReceiveFilterIsDown()
    {
        var disable = IL.Method(Zone, "DisableFirewall");
        var receiveDown = disable.Body.Instructions.FirstOrDefault(i => i.OpCode == Mono.Cecil.Cil.OpCodes.Stfld
            && i.Operand is FieldReference f && PluginIL.Name(f) == $"{Zone}::receiveFilterUp" && i.Previous?.OpCode == Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
        var sendDisable = (Func<Instruction, bool>)(i => PluginIL.Calls(i, "Dalamud.Hooking.Hook::Disable")
            && i.Previous?.Operand is FieldReference f && PluginIL.Name(f) == $"{Zone}::sendPacketHook");
        Assert.That(receiveDown, Is.Not.Null, "DisableFirewall no longer clears receiveFilterUp; update this rule.");
        // Cleared in the entry block, ahead of any branch, so every path to the send Disable has passed it.
        var body = disable.Body.Instructions.ToList();
        var storeAt = body.IndexOf(receiveDown!);
        Assert.That(body.Take(storeAt).Any(i => i.OpCode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return), Is.False,
            "receiveFilterUp is no longer cleared before DisableFirewall's first branch.");
        Assert.That(body.Where(sendDisable).All(d => body.IndexOf(d) > storeAt), Is.True, "the send filter can come down before receiveFilterUp is cleared.");
    }
}
