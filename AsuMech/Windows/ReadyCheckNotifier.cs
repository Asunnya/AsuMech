using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace AsuMech.Windows;

// The game can't show a ready check the firewall holds, and the window's alert is easy to miss
// mid-fight: the first frame one is up, say so in chat with a sound, and leave if asked to.
internal sealed class ReadyCheckNotifier
{
    private const uint ChatSound = 6;
    private string? notifiedFrom;

    public void Tick()
    {
        if (MainWindow.PartyReadyCheck() is not { } check)
        {
            notifiedFrom = null;
            return;
        }
        if (notifiedFrom == check.From) return;
        notifiedFrom = check.From;
        var leave = Plugin.Config.LeaveSimOnReadyCheck;
        Plugin.ChatGui.Print(new XivChatEntry
        {
            Type = XivChatType.Urgent,
            Message = $"[AsuMech] Ready check from {check.From}: {check.Left.TotalSeconds:F0}s to answer. "
                      + (leave ? "Leaving the sim so you can answer it." : "Leave the sim to answer it."),
        });
        UIGlobals.PlayChatSoundEffect(ChatSound);
        if (leave) Plugin.Game.Leave();
    }
}
