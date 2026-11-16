using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.ReplicationOne;

public sealed class M12sReplicationOneSettingsWindow
{
    private static readonly string[] CardinalLabels = ["Random", "North", "East", "South", "West"];
    private static readonly string[] IntercardLabels = ["Random", "North-east", "South-east", "South-west", "North-west"];
    private static readonly string[] CleaveLabels = ["Random", "North-south", "East-west"];
    private static readonly string[] KickLabels = ["Random", "North-east", "North-west"];
    private static readonly string[] TurnLabels = ["Random", "Same side", "Quarter right", "Opposite", "Quarter left"];

    public M12sReplicationOneStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.FireCardinal = null;
            Overrides.VerticalCleaves = null;
            Overrides.OuterFireIntercard = null;
            Overrides.SnakingKickEast = null;
            Overrides.SobatTurns = null;
        }

        if (SettingsGrid.Begin("##m12sreplication1"))
        {
            Overrides.FireCardinal = Choice("Fire clone (dark opposite):", "##m12sfire", CardinalLabels, Overrides.FireCardinal);
            Overrides.VerticalCleaves = Flag("Clone cleaves:", "##m12scleaves", CleaveLabels, Overrides.VerticalCleaves);
            Overrides.OuterFireIntercard = Choice("Second set, outer fire clone:", "##m12souterfire", IntercardLabels, Overrides.OuterFireIntercard);
            Overrides.SnakingKickEast = Flag("Snaking Kick faces:", "##m12skick", KickLabels, Overrides.SnakingKickEast);
            Overrides.SobatTurns = Choice("Double Sobat follow-up:", "##m12ssobat", TurnLabels, Overrides.SobatTurns);
            SettingsGrid.End();
        }
    }

    private static int? Choice(string label, string id, string[] labels, int? current)
    {
        SettingsGrid.Row(label);
        var selected = (current ?? -1) + 1;
        SettingsGrid.ItemWidth(160);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : selected - 1;
    }

    private static bool? Flag(string label, string id, string[] labels, bool? current)
        => Choice(label, id, labels, current is { } on ? (on ? 0 : 1) : null) is { } picked ? picked == 0 : null;
}
