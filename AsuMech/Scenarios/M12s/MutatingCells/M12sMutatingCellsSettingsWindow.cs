using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.MutatingCells;

public sealed class M12sMutatingCellsSettingsWindow
{
    private static readonly string[] MutationLabels = ["Random", "Alpha", "Beta"];
    private static readonly string[] SideLabels = ["Random", "West", "East"];
    private static readonly string[] PairLabels = ["Random", "Water + thunder", "Aero + fire"];
    private static readonly string[] NetherworldLabels = ["Random", "Near", "Far"];
    private static readonly string[] TurnLabels = ["Random", "Same half", "Turned left", "Opposite half", "Turned right"];

    public M12sMutatingCellsStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerAlphaFirst = null;
            Overrides.CloseSide = null;
            Overrides.CloseWaterThunder = null;
            Overrides.NetherworldFar = null;
            Overrides.SobatTurn = null;
        }

        if (SettingsGrid.Begin("##m12smutating"))
        {
            SettingsGrid.Row("My first mutation:");
            Overrides.PlayerAlphaFirst = BoolCombo("##m12smutation", Overrides.PlayerAlphaFirst, MutationLabels);
            SettingsGrid.Row("Close orbs' side:");
            var side = Overrides.CloseSide is { } s ? (s < 0 ? 1 : 2) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12sside", ref side, SideLabels, SideLabels.Length))
                Overrides.CloseSide = side == 0 ? null : side == 1 ? -1 : 1;
            SettingsGrid.Row("Close orbs:");
            Overrides.CloseWaterThunder = BoolCombo("##m12spair", Overrides.CloseWaterThunder, PairLabels);
            SettingsGrid.Row("Netherworld:");
            var far = Overrides.NetherworldFar is { } f ? (f ? 2 : 1) : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12snether", ref far, NetherworldLabels, NetherworldLabels.Length))
                Overrides.NetherworldFar = far == 0 ? null : far == 2;
            SettingsGrid.Row("Double Sobat's second kick:");
            var turn = (Overrides.SobatTurn ?? -1) + 1;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12ssobat", ref turn, TurnLabels, TurnLabels.Length))
                Overrides.SobatTurn = turn == 0 ? null : turn - 1;
            SettingsGrid.End();
        }
    }

    // Index 1 is true, 2 false.
    private static bool? BoolCombo(string id, bool? current, string[] labels)
    {
        var selected = current is { } v ? (v ? 1 : 2) : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : selected == 1;
    }
}
