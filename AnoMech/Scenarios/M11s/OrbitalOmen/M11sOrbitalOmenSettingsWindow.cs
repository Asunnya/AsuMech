using System.Linq;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M11s.OrbitalOmen;

public sealed class M11sOrbitalOmenSettingsWindow
{
    private static readonly string[] PatternLabels =
        ["Random", .. M11sOrbitalOmenState.Patterns.Select(p => string.Join(" ", p.Select(pair => $"({pair.North},{pair.East})")))];

    public M11sOrbitalOmenStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
            Overrides.Pattern = null;

        if (SettingsGrid.Begin("##m11sorbital"))
        {
            SettingsGrid.Row("Line pairs (x, z) in order:");
            var selected = (Overrides.Pattern ?? -1) + 1;
            SettingsGrid.ItemWidth(260);
            ImGui.Combo("##m11spattern", ref selected, PatternLabels, PatternLabels.Length);
            Overrides.Pattern = selected == 0 ? null : selected - 1;
            SettingsGrid.End();
        }
    }
}
