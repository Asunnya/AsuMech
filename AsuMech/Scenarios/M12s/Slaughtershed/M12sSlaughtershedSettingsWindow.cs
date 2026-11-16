using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.Slaughtershed;

public sealed class M12sSlaughtershedSettingsWindow
{
    private static readonly string[] PurpleLabels = ["Random", "East", "West"];
    private static readonly string[] MarkerLabels = ["Random", "Spread", "Stack"];
    private static readonly string[] SerpentineLabels = ["Random", "First", "Second", "Third"];

    public M12sSlaughtershedStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PurpleEast = null;
            Overrides.PlayerSpreads = null;
            Overrides.SerpentineRound = null;
        }

        if (SettingsGrid.Begin("##m12sslaughter"))
        {
            SettingsGrid.Row("Purple orbs:");
            Overrides.PurpleEast = Choice("##m12sslaughterpurple", Overrides.PurpleEast, PurpleLabels);
            SettingsGrid.Row("My Slaughtershed role:");
            Overrides.PlayerSpreads = Choice("##m12sslaughtermarker", Overrides.PlayerSpreads, MarkerLabels);
            SettingsGrid.Row("Cleaving Slaughtershed:");
            var round = Overrides.SerpentineRound is { } r ? r + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12sslaughterserpentine", ref round, SerpentineLabels, SerpentineLabels.Length))
                Overrides.SerpentineRound = round == 0 ? null : round - 1;
            SettingsGrid.End();
        }
    }

    private static bool? Choice(string id, bool? current, string[] labels)
    {
        var selected = current is { } value ? (value ? 1 : 2) : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : selected == 1;
    }
}
