using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P5ParadiseRegained;

public sealed class FruP5ParadiseRegainedSettingsWindow
{
    public FruP5ParadiseRegainedStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup5paradise"))
        {
            SettingsGrid.FightOnlyNote();
            DrawWings();
            DrawSecondTower();
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.Rotation = null;
        Overrides.EastSecond = null;
        Overrides.DarkFirst = null;
    }

    private void DrawWings()
    {
        var v = Overrides.DarkFirst;
        SettingsGrid.Row("First wing:");
        if (ImGui.RadioButton("Auto##prwing", v == null)) Overrides.DarkFirst = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Dark##prwing", v == true)) Overrides.DarkFirst = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Light##prwing", v == false)) Overrides.DarkFirst = false;
    }

    private void DrawSecondTower()
    {
        var v = Overrides.EastSecond;
        SettingsGrid.Row("Second tower:");
        if (ImGui.RadioButton("Auto##prtower", v == null)) Overrides.EastSecond = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("East##prtower", v == true)) Overrides.EastSecond = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("West##prtower", v == false)) Overrides.EastSecond = false;
    }
}
