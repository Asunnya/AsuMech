using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P2DiamondDust;

public sealed class FruP2DiamondDustSettingsWindow
{
    public FruP2DiamondDustStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup2diamonddust"))
        {
            SettingsGrid.FightOnlyNote();
            DrawKick();
            DrawTwin();
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.Axe = null;
        Overrides.Stillness = null;
    }

    private void DrawKick()
    {
        var v = Overrides.Axe;
        SettingsGrid.Row("Opening kick:");
        if (ImGui.RadioButton("Auto##ddkick", v == null)) Overrides.Axe = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Axe (out)##ddkick", v == true)) Overrides.Axe = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Scythe (in)##ddkick", v == false)) Overrides.Axe = false;
    }

    private void DrawTwin()
    {
        var v = Overrides.Stillness;
        SettingsGrid.Row("Twin:");
        if (ImGui.RadioButton("Auto##ddtwin", v == null)) Overrides.Stillness = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("Stillness##ddtwin", v == true)) Overrides.Stillness = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("Silence##ddtwin", v == false)) Overrides.Stillness = false;
    }
}
