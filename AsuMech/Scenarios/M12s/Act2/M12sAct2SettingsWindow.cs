using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.Act2;

public sealed class M12sAct2SettingsWindow
{
    private static readonly string[] OrderLabels = ["Random", "First", "Second", "Third", "Fourth"];
    private static readonly string[] BlobLabels = ["Random", "North-east", "South-east", "South-west", "North-west"];
    private static readonly string[] FacingLabels = ["Random", "North", "East", "South", "West"];

    public M12sAct2StateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerOrder = null;
            Overrides.FirstBlob = null;
            Overrides.FirstFacing = null;
        }

        if (SettingsGrid.Begin("##m12sact2"))
        {
            SettingsGrid.Row("My chain:");
            var order = Overrides.PlayerOrder ?? 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m12sact2order", ref order, OrderLabels, OrderLabels.Length))
                Overrides.PlayerOrder = order == 0 ? null : order;

            SettingsGrid.Row("First blob:");
            Overrides.FirstBlob = Combo("##m12sact2blob", Overrides.FirstBlob, BlobLabels);

            SettingsGrid.Row("First spin faces:");
            Overrides.FirstFacing = Combo("##m12sact2facing", Overrides.FirstFacing, FacingLabels);
            SettingsGrid.End();
        }
    }

    private static int? Combo(string id, int? current, string[] labels)
    {
        var selected = current is { } c ? c + 1 : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : selected - 1;
    }
}
