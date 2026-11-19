using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;

namespace AsuMech.Windows;

// The game can't show a ready check the firewall holds, and an answer is a packet the firewall
// keeps from the server. So the run's alert takes the answer: picking one leaves the sim, and once
// the lift reopens the check, the game's own Yes/No prompt is answered with it.
internal sealed unsafe class ReadyCheckNotifier : IDisposable
{
    private const uint ChatSound = 6;
    // Addon row: "A ready check has been commenced. Inform your comrades that you are ready?"
    private const uint ReadyCheckPromptRow = 8133;

    private string? notifiedFrom;
    private bool? answer;
    private DateTime answerBefore;
    private readonly string? promptText;

    public ReadyCheckNotifier()
    {
        // Its first line: the prompt carries the rest on lines of its own.
        promptText = Plugin.DataManager.GetExcelSheet<Addon>().TryGetRow(ReadyCheckPromptRow, out var row) ? Squash(row.Text.ExtractText().Split('\n')[0]) : null;
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", OnSelectYesno);
    }

    public void Dispose() => Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectYesno", OnSelectYesno);

    public bool? PendingAnswer => answer;

    // From the alert's buttons.
    public void Answer(bool ready)
    {
        if (MainWindow.PartyReadyCheck() is not { } check) return;
        answer = ready;
        answerBefore = DateTime.UtcNow + check.Left;
        Core.DiagnosticLog.Info($"[ReadyCheck] Answered {(ready ? "ready" : "not ready")} in the sim; leaving so the game can send it.");
        Plugin.Game.Leave();
    }

    public void Tick()
    {
        if (answer != null && DateTime.UtcNow > answerBefore)
        {
            answer = null;
            Print("The ready check ran out before the game could take your answer.");
        }
        if (MainWindow.PartyReadyCheck() is not { } check)
        {
            notifiedFrom = null;
            return;
        }
        if (notifiedFrom == check.From) return;
        notifiedFrom = check.From;
        var leave = Plugin.Config.LeaveSimOnReadyCheck;
        Print($"Ready check from {check.From}: {check.Left.TotalSeconds:F0}s to answer. "
              + (leave ? "Leaving the sim so you can answer it." : "Answer it from the AsuMech window."));
        UIGlobals.PlayChatSoundEffect(ChatSound);
        if (leave) Plugin.Game.Leave();
    }

    private void OnSelectYesno(AddonEvent type, AddonArgs args)
    {
        if (answer is not { } ready || promptText == null) return;
        var addon = (AddonSelectYesno*)args.Addon.Address;
        if (addon == null || addon->PromptText == null) return;
        if (promptText.Length == 0 || !Squash(addon->PromptText->NodeText.ToString()).StartsWith(promptText, StringComparison.Ordinal)) return;
        answer = null;
        addon->AtkUnitBase.FireCallbackInt(ready ? 0 : 1);
        Core.DiagnosticLog.Info($"[ReadyCheck] Sent the answer chosen in the sim: {(ready ? "ready" : "not ready")}.");
        Print($"Answered the ready check: {(ready ? "ready" : "not ready")}.");
    }

    private static string Squash(string text) => new(text.Where(c => !char.IsWhiteSpace(c)).ToArray());

    private static void Print(string text)
        => Plugin.ChatGui.Print(new XivChatEntry { Type = XivChatType.Urgent, Message = $"[AsuMech] {text}" });
}
