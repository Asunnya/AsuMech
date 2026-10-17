using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P4DarklitDragonsong;

public sealed class FruP4DarklitDragonsongSettingsWindow
{
    public FruP4DarklitDragonsongStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.EastWing = null;
        if (SettingsGrid.Begin("##frup4darklit"))
        {
            SettingsGrid.FightOnlyNote();
            var v = Overrides.EastWing;
            SettingsGrid.Row("Hallowed Wings:");
            if (ImGui.RadioButton("Auto##dlwing", v == null)) Overrides.EastWing = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("East##dlwing", v == true)) Overrides.EastWing = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("West##dlwing", v == false)) Overrides.EastWing = false;
            SettingsGrid.End();
        }
    }
}
