using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Fru.P5ParadiseLost;

public sealed class FruP5ParadiseLostSettingsWindow
{
    public FruP5ParadiseLostStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.PlayOutEnrage = false;
        if (SettingsGrid.Begin("##frup5paradiselost"))
        {
            SettingsGrid.FightOnlyNote();
            SettingsGrid.Row("Enrage:");
            var enrage = Overrides.PlayOutEnrage;
            if (ImGui.Checkbox("Play out the enrage##pllost", ref enrage)) Overrides.PlayOutEnrage = enrage;
            SettingsGrid.End();
        }
    }
}
