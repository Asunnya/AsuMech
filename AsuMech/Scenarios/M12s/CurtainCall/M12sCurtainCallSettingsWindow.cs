using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.CurtainCall;

public sealed class M12sCurtainCallSettingsWindow
{
    private static readonly string[] DebuffLabels = ["Random", "Rotting Flesh (in the cleave)", "Bursting Grotesquerie (spread)"];
    private static readonly string[] DragonLabels = ["Random", "East", "West"];
    private static readonly string[] CornerLabels = ["Random", "North-west and south-east", "North-east and south-west"];

    public M12sCurtainCallStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerRotting = null;
            Overrides.DragonEast = null;
            Overrides.NorthWestSafe = null;
        }

        if (SettingsGrid.Begin("##m12scurtain"))
        {
            SettingsGrid.Row("My debuff:");
            Overrides.PlayerRotting = Choice("##m12scurtaindebuff", Overrides.PlayerRotting, DebuffLabels, 220);
            SettingsGrid.Row("Dragon head:");
            Overrides.DragonEast = Choice("##m12scurtaindragon", Overrides.DragonEast, DragonLabels, 140);
            SettingsGrid.Row("Safe corners:");
            Overrides.NorthWestSafe = Choice("##m12scurtaincorners", Overrides.NorthWestSafe, CornerLabels, 220);
            SettingsGrid.End();
        }
    }

    private static bool? Choice(string id, bool? current, string[] labels, float width)
    {
        var selected = current is { } value ? (value ? 1 : 2) : 0;
        SettingsGrid.ItemWidth(width);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : selected == 1;
    }
}
