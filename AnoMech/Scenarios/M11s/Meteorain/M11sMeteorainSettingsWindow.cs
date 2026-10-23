using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M11s.Meteorain;

public sealed class M11sMeteorainSettingsWindow
{
    private static readonly string[] SideLabels = ["Random", "North + East", "North + West", "East + West", "South + North"];
    private static readonly (int, int)[] Sides = [(0, 1), (0, 3), (1, 3), (2, 0)];

    public M11sMeteorainStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
            Overrides.ShadeSides = null;

        if (SettingsGrid.Begin("##m11smeteorain"))
        {
            SettingsGrid.Row("Shades every set:");
            var selected = 0;
            if (Overrides.ShadeSides is { Count: > 0 } current)
                selected = System.Array.IndexOf(Sides, current[0]) + 1;
            SettingsGrid.ItemWidth(160);
            if (ImGui.Combo("##m11sshades", ref selected, SideLabels, SideLabels.Length))
                Overrides.ShadeSides = selected == 0 ? null : [Sides[selected - 1], Sides[selected - 1], Sides[selected - 1]];
            SettingsGrid.End();
        }
    }
}
