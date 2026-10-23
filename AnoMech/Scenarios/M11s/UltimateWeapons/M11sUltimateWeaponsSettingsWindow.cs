using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M11s.UltimateWeapons;

public sealed class M11sUltimateWeaponsSettingsWindow
{
    private static readonly string[] SideLabels = ["Random", "South", "East", "North", "West"];
    private static readonly string[] TrophyLabels = ["Random", "Axe", "Scythe"];
    private static readonly string[] MirrorLabels = ["Random", "East", "West"];

    public M11sUltimateWeaponsStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.OpenSide = null;
            Overrides.AxeTrophy = null;
            Overrides.Mirrored = null;
            Overrides.Weapons = null;
        }

        if (SettingsGrid.Begin("##m11sultimate"))
        {
            SettingsGrid.Row("Lines open to the:");
            var side = Overrides.OpenSide is { } s ? s + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m11sopenside", ref side, SideLabels, SideLabels.Length))
                Overrides.OpenSide = side == 0 ? null : side - 1;

            SettingsGrid.Row("Raw Steel Trophy:");
            var trophy = Overrides.AxeTrophy is { } axe ? (axe ? 1 : 2) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m11strophy2", ref trophy, TrophyLabels, TrophyLabels.Length))
                Overrides.AxeTrophy = trophy == 0 ? null : trophy == 1;

            SettingsGrid.Row("First weapon on the:");
            var mirror = Overrides.Mirrored is { } m ? (m ? 2 : 1) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m11smirror", ref mirror, MirrorLabels, MirrorLabels.Length))
                Overrides.Mirrored = mirror == 0 ? null : mirror == 2;
            SettingsGrid.End();
        }
    }
}
