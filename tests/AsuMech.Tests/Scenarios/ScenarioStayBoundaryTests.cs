using System.Text.RegularExpressions;
using Mono.Cecil;

namespace AsuMech.Tests;

// A scenario runs inside a stay, on its event timeline, and must not outlive it: work it schedules
// anywhere else can run after the lift and act on the real character with nothing holding the
// result from the server. It reaches the game only through the Natives interfaces, never the
// firewall itself.
public class ScenarioStayBoundaryTests
{
    private static readonly (string Pattern, string Why)[] Forbidden =
    [
        (@"^System\.Threading\.Tasks\.(Task|TaskFactory|Parallel|ValueTask)::", "starts work outside the sim's timeline; use world.Events"),
        (@"^System\.Threading\.(Thread|ThreadPool|Timer)::", "starts work outside the sim's timeline; use world.Events"),
        (@"^System\.Timers\.", "starts work outside the sim's timeline; use world.Events"),
        (@"^System\.Runtime\.CompilerServices\.Async\w*MethodBuilder", "is async; its continuations outlive the stay"),
        (@"^Dalamud\.Plugin\.Services\.IFramework::", "runs on Dalamud's framework, outside the sim's timeline"),
        (@"^Dalamud\.Plugin\.Services\.IGameInteropProvider::|^Dalamud\.Hooking\.", "hooks the game"),
        (@"^Dalamud\.Plugin\.Services\.(IClientState|ICondition|IDutyState)::add_", "subscribes to game events that outlive the stay"),
        (@"^AsuMech\.Plugin::get_(Framework|GameInterop)$", "reaches Dalamud services that outlive the stay"),
        (@"^AsuMech\.Core\.Native\.Implementations\.", "names a native implementation; reach the game through Natives"),
        (@"^AsuMech\.Core\.Native\.Interfaces\.IZoneControl::(Enter|Revert|HoldSendFirewall|InjectIncomingPacket)$", "drives the firewall"),
        (@"^AsuMech\.Core\.Game\.Game::(Leave\w*|RunScenario\w*|StartScenario\w*|Dispose)$", "starts or ends runs"),
        (@"^System\.Environment::(FailFast|Exit)$", "ends the process"),
    ];

    [Test]
    public void Scenarios_NeverActOutsideTheirStay()
    {
        var failures = new List<string>();
        foreach (var m in PluginIL.Build.Methods)
        {
            var top = m.DeclaringType;
            while (top.IsNested) top = top.DeclaringType;
            if (!top.Namespace.StartsWith("AsuMech.Scenarios")) continue;
            var owner = PluginIL.Owner(m);
            if (m.DeclaringType.Interfaces.Any(i => i.InterfaceType.Name == "IAsyncStateMachine"))
                failures.Add($"{owner} is async; a scenario's continuations would outlive its stay.");
            foreach (var i in m.Body.Instructions)
            {
                var name = i.Operand switch
                {
                    MethodReference r => PluginIL.Name(r),
                    FieldReference f => PluginIL.Name(f),
                    _ => null,
                };
                if (name == null) continue;
                foreach (var (pattern, why) in Forbidden)
                    if (Regex.IsMatch(name, pattern))
                        failures.Add($"{owner} uses {name}: a scenario {why}.");
            }
        }
        ILRules.AssertNone(failures.Distinct());
    }
}
