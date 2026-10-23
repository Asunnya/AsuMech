using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P3UltimateRelativity;

public sealed class FruP3UltimateRelativitySettingsWindow
{
    public FruP3UltimateRelativityStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.DpsIce = null;
        if (SettingsGrid.Begin("##frup3relativity"))
        {
            SettingsGrid.FightOnlyNote();
            var v = Overrides.DpsIce;
            SettingsGrid.Row("Dark Blizzard:");
            if (ImGui.RadioButton("Auto##urice", v == null)) Overrides.DpsIce = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Long DPS##urice", v == true)) Overrides.DpsIce = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("Short support##urice", v == false)) Overrides.DpsIce = false;
            SettingsGrid.End();
        }
    }
}
