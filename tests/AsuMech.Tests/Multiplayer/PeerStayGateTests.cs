using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static AsuMech.Tests.ILRules;

namespace AsuMech.Tests;

// What a host sends acts on the peer's real character. Applied outside the peer's own firewalled
// stay, it would move or change that character with nothing holding the result from the server.
public class PeerStayGateTests
{
    private const string Manager = "AsuMech.Multiplayer.MultiplayerManager";

    private static readonly Regex ActsOnTheCharacter = new(
        @"^AsuMech\.Core\.SimObjects\..*::(TeleportTo|CarryTo|Push|MarchTo|ForceFollow|Knockback|SetPosition|SetRotation|ApplyNetworkPose)$"
        + @"|^AsuMech\.Multiplayer\.PacketReplay::Apply$|^AsuMech\.Core\.Game\.Game::Kill$");

    // Host-side handlers, which only ever pose another player's puppet, never the local character.
    private static readonly string[] HostSide = [$"{Manager}::OnSelfPoseReceived"];

    private static bool IsStayCheck(Instruction i)
        => i.Operand is MethodReference m && PluginIL.Name(m) is $"{Manager}::get_PeerInRun" or $"{Manager}::InFirewalledStay";

    [Test]
    public void HostMessagesThatActOnTheCharacter_AreDroppedOutsideTheStay()
    {
        var failures = new List<string>();
        var checkedAny = false;
        foreach (var m in PluginIL.Build.Methods.Where(m => PluginIL.Owner(m).StartsWith($"{Manager}::")))
        {
            var acts = (Func<Instruction, bool>)(i => i.Operand is MethodReference r && i.OpCode.Code is Code.Call or Code.Callvirt && ActsOnTheCharacter.IsMatch(PluginIL.Name(r)));
            if (!m.Body.Instructions.Any(acts) || HostSide.Contains(PluginIL.Owner(m))) continue;
            checkedAny = true;
            failures.AddRange(EveryPathPasses(m, IsStayCheck, "PeerInRun / InFirewalledStay", true, acts, "an action on the character"));
        }
        if (!checkedAny) failures.Add("found no MultiplayerManager code acting on the character; update this rule.");
        AssertNone(failures);
    }
}
