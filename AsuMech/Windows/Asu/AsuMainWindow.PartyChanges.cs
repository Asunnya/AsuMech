using System.Linq;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const int PartyChangesShown = 3;

    // The game only learns of these once the sim is left, so the window lists them meanwhile.
    private static void DrawPartyChanges()
    {
        var changes = MainWindow.PartyChanges();
        if (changes.Count == 0 && !MainWindow.HasPartyAlert()) return;
        Gap();
        Muted("Your party since you entered the sim");
        MainWindow.DrawPartyAlerts();
        foreach (var change in changes.TakeLast(PartyChangesShown))
            Muted($"{change.ShownAt.ToLocalTime():HH:mm}  {change.Text}");
    }
}
