using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M11s.Ecliptic;

public sealed class M11sEclipticSettingsWindow
{
    private static readonly string[] MammothLabels = ["Random", "North-east + south-west", "North-west + south-east"];
    private static readonly string[] MarkedLabels = ["Random", "Yes (stand out)", "No"];

    public M11sEclipticStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.MammothNortheast = null;
            Overrides.PlayerMarked = null;
        }

        if (SettingsGrid.Begin("##m11secliptic"))
        {
            SettingsGrid.Row("Mammoth Meteors:");
            var mammoth = Overrides.MammothNortheast is { } ne ? (ne ? 1 : 2) : 0;
            SettingsGrid.ItemWidth(180);
            if (ImGui.Combo("##m11smammoth", ref mammoth, MammothLabels, MammothLabels.Length))
                Overrides.MammothNortheast = mammoth == 0 ? null : mammoth == 1;

            SettingsGrid.Row("Atomic Impact on me:");
            var marked = Overrides.PlayerMarked is { } m ? (m ? 1 : 2) : 0;
            SettingsGrid.ItemWidth(180);
            if (ImGui.Combo("##m11satomic", ref marked, MarkedLabels, MarkedLabels.Length))
                Overrides.PlayerMarked = marked == 0 ? null : marked == 1;
            SettingsGrid.End();
        }
    }
}
