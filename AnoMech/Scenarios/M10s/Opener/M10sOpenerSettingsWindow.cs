using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.Opener;

public sealed class M10sOpenerSettingsWindow
{
    private static readonly string[] WallLabels = ["Random", "North", "East", "South", "West"];
    private static readonly string[] FloaterLabels = ["Random", "Not tethered", "1st", "2nd", "3rd", "4th"];

    public M10sOpenerStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.Wall = null;
            Overrides.PlayerFloaterNumber = null;
        }

        if (SettingsGrid.Begin("##m10sopener"))
        {
            SettingsGrid.Row("Red Hot leaps to:");
            var wall = Overrides.Wall is { } w ? w + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m10swall", ref wall, WallLabels, WallLabels.Length))
                Overrides.Wall = wall == 0 ? null : wall - 1;

            SettingsGrid.Row("My Flame Floater:");
            var floater = Overrides.PlayerFloaterNumber is { } n ? n + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m10sfloater", ref floater, FloaterLabels, FloaterLabels.Length))
                Overrides.PlayerFloaterNumber = floater == 0 ? null : floater - 1;
            SettingsGrid.End();
        }
    }
}
