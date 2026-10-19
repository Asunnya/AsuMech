using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.DeepAerial;

public sealed class M10sDeepAerialSettingsWindow
{
    private static readonly string[] FirstLabels = ["Random", "1st fire", "1st water", "2nd fire", "2nd water", "3rd fire", "3rd water"];
    private static readonly string[] SecondLabels = ["Random", "4th fire", "4th water", "5th fire", "5th water", "6th fire", "6th water"];

    public M10sDeepAerialStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.BossesNorth = null;
            Overrides.PlayerFirstTether = null;
            Overrides.PlayerSecondTether = null;
        }

        if (SettingsGrid.Begin("##m10saerial"))
        {
            SettingsGrid.Row("Brothers start:");
            if (ImGui.RadioButton("Random##m10saside", Overrides.BossesNorth == null)) Overrides.BossesNorth = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("North##m10saside", Overrides.BossesNorth == true)) Overrides.BossesNorth = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("South##m10saside", Overrides.BossesNorth == false)) Overrides.BossesNorth = false;

            SettingsGrid.Row("Your first tether:");
            var first = Overrides.PlayerFirstTether is { } f ? f + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m10safirst", ref first, FirstLabels, FirstLabels.Length))
                Overrides.PlayerFirstTether = first == 0 ? null : first - 1;

            SettingsGrid.Row("Your second tether:");
            var second = Overrides.PlayerSecondTether is { } s ? s + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m10sasecond", ref second, SecondLabels, SecondLabels.Length))
                Overrides.PlayerSecondTether = second == 0 ? null : second - 1;
            SettingsGrid.End();
        }
    }
}
