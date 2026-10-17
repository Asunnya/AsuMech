using System.Linq;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.InsaneAir;

public sealed class M10sInsaneAirSettingsWindow
{
    private static readonly string[] VariantLabels = ["Random", .. InsaneAirVariant.All.Select(v => v.Name)];

    public M10sInsaneAirStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.Variant = null;

        if (SettingsGrid.Begin("##m10sinsane"))
        {
            SettingsGrid.Row("Surfboards:");
            var variant = Overrides.Variant is { } v ? v + 1 : 0;
            SettingsGrid.ItemWidth(240);
            if (ImGui.Combo("##m10ssurf", ref variant, VariantLabels, VariantLabels.Length))
                Overrides.Variant = variant == 0 ? null : variant - 1;
            SettingsGrid.End();
        }
    }
}
