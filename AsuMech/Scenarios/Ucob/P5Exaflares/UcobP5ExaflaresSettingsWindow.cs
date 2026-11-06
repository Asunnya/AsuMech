using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Ucob.P5Exaflares;

// Settings panel for the P5 exaflares scenario: pin or randomize the direction the lanes roll.
// Mirrors TopP2PartySynergySettingsWindow's direction rows.
public sealed class UcobP5ExaflaresSettingsWindow
{
    public UcobP5ExaflaresStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Overrides.Direction = null;

        if (SettingsGrid.Begin("##ucobp5exaflares"))
        {
            SettingsGrid.Row("Rolls toward:");
            if (ImGui.RadioButton("Auto##exadir", Overrides.Direction == null)) Overrides.Direction = null;
            foreach (var d in Direction.All)
            {
                ImGui.SameLine();
                if (ImGui.RadioButton($"{d.Name()}##exadir", Overrides.Direction == d)) Overrides.Direction = d;
            }
            SettingsGrid.End();
        }
    }
}
