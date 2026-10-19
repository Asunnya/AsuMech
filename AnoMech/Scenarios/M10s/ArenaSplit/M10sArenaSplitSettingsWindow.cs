using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.ArenaSplit;

public sealed class M10sArenaSplitSettingsWindow
{
    public M10sArenaSplitStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.DeepBlueWest = null;
            Overrides.DoubleDip = null;
            Overrides.WaveFrom = null;
            Overrides.PlayerPyrotation = null;
        }

        if (SettingsGrid.Begin("##m10ssplit"))
        {
            SettingsGrid.Row("Deep Blue's proteans:");
            if (ImGui.RadioButton("Random##m10sside", Overrides.DeepBlueWest == null)) Overrides.DeepBlueWest = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("West##m10sside", Overrides.DeepBlueWest == true)) Overrides.DeepBlueWest = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("East##m10sside", Overrides.DeepBlueWest == false)) Overrides.DeepBlueWest = false;

            SettingsGrid.Row("Alley-oop:");
            if (ImGui.RadioButton("Random##m10ssalley", Overrides.DoubleDip == null)) Overrides.DoubleDip = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Reverse##m10ssalley", Overrides.DoubleDip == false)) Overrides.DoubleDip = false;
            ImGui.SameLine();
            if (ImGui.RadioButton("Double-dip##m10ssalley", Overrides.DoubleDip == true)) Overrides.DoubleDip = true;

            SettingsGrid.Row("Wave from:");
            if (ImGui.RadioButton("Random##m10sswave", Overrides.WaveFrom == null)) Overrides.WaveFrom = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("North##m10sswave", Overrides.WaveFrom == 0)) Overrides.WaveFrom = 0;
            ImGui.SameLine();
            if (ImGui.RadioButton("East##m10sswave", Overrides.WaveFrom == 1)) Overrides.WaveFrom = 1;
            ImGui.SameLine();
            if (ImGui.RadioButton("West##m10sswave", Overrides.WaveFrom == 3)) Overrides.WaveFrom = 3;

            SettingsGrid.Row("Pyrotation marker:");
            if (ImGui.RadioButton("Random##m10sspyro", Overrides.PlayerPyrotation == null)) Overrides.PlayerPyrotation = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("On you##m10sspyro", Overrides.PlayerPyrotation == true)) Overrides.PlayerPyrotation = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("On your partner##m10sspyro", Overrides.PlayerPyrotation == false)) Overrides.PlayerPyrotation = false;
            SettingsGrid.End();
        }
    }
}
