using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.Act3;

public sealed class M12sAct3SettingsWindow
{
    private static readonly string[] GrowthLabels = ["Random", "North", "East", "South", "West"];
    private static readonly string[] PlatformLabels = ["Random", "Cardinal", "Intercardinal"];

    public M12sAct3StateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerGrowth = null;
            Overrides.CardinalPlatforms = null;
        }

        if (SettingsGrid.Begin("##m12sact3"))
        {
            SettingsGrid.Row("My growth points:");
            var growth = Overrides.PlayerGrowth is { } g ? g + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12sact3growth", ref growth, GrowthLabels, GrowthLabels.Length))
                Overrides.PlayerGrowth = growth == 0 ? null : growth - 1;

            SettingsGrid.Row("Platforms that stay:");
            var platforms = Overrides.CardinalPlatforms is { } c ? (c ? 1 : 2) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12sact3platforms", ref platforms, PlatformLabels, PlatformLabels.Length))
                Overrides.CardinalPlatforms = platforms == 0 ? null : platforms == 1;
            SettingsGrid.End();
        }
    }
}
