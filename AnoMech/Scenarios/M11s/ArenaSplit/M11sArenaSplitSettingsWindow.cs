using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M11s.ArenaSplit;

public sealed class M11sArenaSplitSettingsWindow
{
    private static readonly string[] TetherLabels = ["Random", "First round", "Second round"];
    private static readonly string[] LineLabels = ["Random", "x -21 and 11", "x -11 and 21"];
    private static readonly string[] AvalancheLabels = ["Random", "North-east", "South-east", "South-west", "North-west"];

    public M11sArenaSplitStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerTetherRound = null;
            Overrides.FirstLinesWestOuter = null;
            Overrides.SecondLinesWestOuter = null;
            Overrides.Avalanche = null;
        }

        if (SettingsGrid.Begin("##m11ssplit"))
        {
            SettingsGrid.Row("My Meteowrath tether:");
            var tether = Overrides.PlayerTetherRound ?? 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m11ssplittether", ref tether, TetherLabels, TetherLabels.Length))
                Overrides.PlayerTetherRound = tether == 0 ? null : tether;

            SettingsGrid.Row("First meteor lines:");
            Overrides.FirstLinesWestOuter = LinesCombo("##m11ssplitlines1", Overrides.FirstLinesWestOuter);
            SettingsGrid.Row("Second meteor lines:");
            Overrides.SecondLinesWestOuter = LinesCombo("##m11ssplitlines2", Overrides.SecondLinesWestOuter);

            SettingsGrid.Row("Avalanche spares:");
            var avalanche = Overrides.Avalanche is { } a ? a + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m11ssplitavalanche", ref avalanche, AvalancheLabels, AvalancheLabels.Length))
                Overrides.Avalanche = avalanche == 0 ? null : avalanche - 1;
            SettingsGrid.End();
        }
    }

    private static bool? LinesCombo(string id, bool? current)
    {
        var selected = current is { } outer ? (outer ? 1 : 2) : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, LineLabels, LineLabels.Length);
        return selected == 0 ? null : selected == 1;
    }
}
