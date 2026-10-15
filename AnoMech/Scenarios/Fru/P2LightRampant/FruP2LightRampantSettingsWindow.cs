using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P2LightRampant;

public sealed class FruP2LightRampantSettingsWindow
{
    public FruP2LightRampantStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup2lightrampant"))
        {
            SettingsGrid.FightOnlyNote();
            DrawOrbs();
            DrawBanish();
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.NorthOrbsFirst = null;
        Overrides.Pairs = null;
    }

    private void DrawOrbs()
    {
        var v = Overrides.NorthOrbsFirst;
        SettingsGrid.Row("First orbs:");
        if (ImGui.RadioButton("Auto##lrorbs", v == null)) Overrides.NorthOrbsFirst = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("North##lrorbs", v == true)) Overrides.NorthOrbsFirst = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("South##lrorbs", v == false)) Overrides.NorthOrbsFirst = false;
    }

    private void DrawBanish()
    {
        var v = Overrides.Pairs;
        SettingsGrid.Row("Banish III:");
        if (ImGui.RadioButton("Auto##lrbanish", v == null)) Overrides.Pairs = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Pairs##lrbanish", v == true)) Overrides.Pairs = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Spread##lrbanish", v == false)) Overrides.Pairs = false;
    }
}
