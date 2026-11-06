using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Uwu.UltimateSuppression;

public class UltimateSuppressionSettingsWindow
{
    public UltimateSuppressionStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            ResetAll();
        }

        if (SettingsGrid.Begin("##ultimatesuppression"))
        {
            DrawAssignment();
            SettingsGrid.End();
        }
    }

    private void DrawAssignment()
    {
        var v = Overrides.Assignment;
        SettingsGrid.Row("Assignment (Non-Tank Only):");
        if (ImGui.RadioButton("Auto##assignment", v == UltimateSuppressionAssignment.Auto)) Overrides.Assignment = UltimateSuppressionAssignment.Auto;
        ImGui.SameLine();
        if (ImGui.RadioButton("Light Pillar##assignment", v == UltimateSuppressionAssignment.LightPillar)) Overrides.Assignment = UltimateSuppressionAssignment.LightPillar;
        ImGui.SameLine();
        if (ImGui.RadioButton("Mistral Song##assignment", v == UltimateSuppressionAssignment.MistralSong)) Overrides.Assignment = UltimateSuppressionAssignment.MistralSong;
        ImGui.SameLine();
        if (ImGui.RadioButton("Eruption##assignment", v == UltimateSuppressionAssignment.Eruption)) Overrides.Assignment = UltimateSuppressionAssignment.Eruption;
        ImGui.SameLine();
        if (ImGui.RadioButton("Gaol##assignment", v == UltimateSuppressionAssignment.Gaol)) Overrides.Assignment = UltimateSuppressionAssignment.Gaol;
    }

    private void ResetAll()
    {
        Overrides.Assignment = UltimateSuppressionAssignment.Auto;
    }
}
