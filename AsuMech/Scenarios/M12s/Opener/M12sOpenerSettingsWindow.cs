using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.Opener;

public sealed class M12sOpenerSettingsWindow
{
    private static readonly string[] PurpleLabels = ["Random", "West", "East"];

    public M12sOpenerStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
            Overrides.PurpleEast = null;

        if (SettingsGrid.Begin("##m12sopener"))
        {
            SettingsGrid.Row("Purple orbs:");
            var purple = Overrides.PurpleEast is { } east ? (east ? 2 : 1) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12spurple", ref purple, PurpleLabels, PurpleLabels.Length))
                Overrides.PurpleEast = purple == 0 ? null : purple == 2;
            SettingsGrid.End();
        }
    }
}
