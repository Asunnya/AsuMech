using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M11s.OrbitalOmen;

public sealed class M11sOrbitalOmenSettingsWindow
{
    private static readonly string[] FirstLabels = ["Random", "-15", "-5", "5", "15"];

    public M11sOrbitalOmenStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.NorthLines = null;
            Overrides.EastLines = null;
        }

        if (SettingsGrid.Begin("##m11sorbital"))
        {
            SettingsGrid.Row("First north line at x:");
            Overrides.NorthLines = FirstCombo("##m11snorth", Overrides.NorthLines);
            SettingsGrid.Row("First east line at z:");
            Overrides.EastLines = FirstCombo("##m11seast", Overrides.EastLines);
            SettingsGrid.End();
        }
    }

    // Pins only the first line; the rest keep their order after it.
    private static int[]? FirstCombo(string id, System.Collections.Generic.IReadOnlyList<int>? current)
    {
        var selected = current is { Count: > 0 } ? System.Array.IndexOf(M11sOrbitalOmenState.Offsets, current[0]) + 1 : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, FirstLabels, FirstLabels.Length);
        if (selected == 0) return null;
        var first = M11sOrbitalOmenState.Offsets[selected - 1];
        return [first, .. System.Linq.Enumerable.Where(M11sOrbitalOmenState.Offsets, o => o != first)];
    }
}
