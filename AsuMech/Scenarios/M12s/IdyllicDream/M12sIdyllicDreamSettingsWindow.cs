using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.M12s.IdyllicDream;

public sealed class M12sIdyllicDreamSettingsWindow
{
    private static readonly string[] CloneLabels = ["Random", "North", "North-east", "East", "South-east", "South", "South-west", "West", "North-west"];
    private static readonly string[] FirstLabels = ["Random", "Cardinals", "Intercardinals"];
    private static readonly string[] BossCloneLabels = ["Random", "Cardinals defamation", "Cardinals stack"];
    private static readonly string[] SpreadLabels = ["Random", "Supports", "DPS"];
    private static readonly string[] KickLabels = ["Random", "West", "East"];
    private static readonly string[] TearLabels = ["Random", "E/W cones (N/S safe)", "N/S cones (E/W safe)"];

    public M12sIdyllicDreamStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerCloneDirection = null;
            Overrides.CardinalClonesFirst = null;
            Overrides.CardinalBossClonesDefa = null;
            Overrides.SpreadOnSupports = null;
            Overrides.KickPlatform = null;
            Overrides.TearTakesEastWestClone = null;
        }

        if (!SettingsGrid.Begin("##m12sidyllic")) return;
        SettingsGrid.Row("My clone:");
        var clone = Overrides.PlayerCloneDirection is { } d ? d + 1 : 0;
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo("##m12sidyllicclone", ref clone, CloneLabels, CloneLabels.Length))
            Overrides.PlayerCloneDirection = clone == 0 ? null : clone - 1;

        SettingsGrid.Row("Player clones first:");
        Overrides.CardinalClonesFirst = BoolCombo("##m12sidyllicfirst", Overrides.CardinalClonesFirst, FirstLabels);
        SettingsGrid.Row("Boss clones:");
        Overrides.CardinalBossClonesDefa = BoolCombo("##m12sidyllicboss", Overrides.CardinalBossClonesDefa, BossCloneLabels);
        SettingsGrid.Row("Light spreads on:");
        Overrides.SpreadOnSupports = BoolCombo("##m12sidyllicspread", Overrides.SpreadOnSupports, SpreadLabels);

        SettingsGrid.Row("Snaking Kick lands:");
        var kick = Overrides.KickPlatform switch { -1 => 1, 1 => 2, _ => 0 };
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo("##m12sidyllickick", ref kick, KickLabels, KickLabels.Length))
            Overrides.KickPlatform = kick switch { 1 => -1, 2 => 1, _ => null };

        SettingsGrid.Row("Into the tear:");
        Overrides.TearTakesEastWestClone = BoolCombo("##m12sidyllictear", Overrides.TearTakesEastWestClone, TearLabels);
        SettingsGrid.End();
    }

    private static bool? BoolCombo(string id, bool? current, string[] labels)
    {
        var selected = current is { } v ? (v ? 1 : 2) : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : selected == 1;
    }
}
