using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Dsr.P2Thordan;

public sealed class DsrP2ThordanSettingsWindow
{
    public DsrP2ThordanStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.MeteorOnMe = null;

        if (SettingsGrid.Begin("##dsrp2thordan"))
        {
            SettingsGrid.Row("Meteor:");
            if (ImGui.RadioButton("Random##dsrmeteor", Overrides.MeteorOnMe != true)) Overrides.MeteorOnMe = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Always on me##dsrmeteor", Overrides.MeteorOnMe == true)) Overrides.MeteorOnMe = true;
            SettingsGrid.End();
        }
    }
}
