using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.Snaking;

public sealed class M10sSnakingSettingsWindow
{
    public M10sSnakingStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerFire = null;
            Overrides.FireOnTheNearest = null;
            Overrides.DeepBlueSouth = null;
            Overrides.DoubleDip = null;
            Overrides.WaveFromNorth = null;
        }

        if (SettingsGrid.Begin("##m10ssnaking"))
        {
            SettingsGrid.Row("Your debuff:");
            if (ImGui.RadioButton("Random##m10selement", Overrides.PlayerFire == null)) Overrides.PlayerFire = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Fire##m10selement", Overrides.PlayerFire == true)) Overrides.PlayerFire = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("Water##m10selement", Overrides.PlayerFire == false)) Overrides.PlayerFire = false;

            SettingsGrid.Row("Firesnaking takes:");
            if (ImGui.RadioButton("Random##m10snear", Overrides.FireOnTheNearest == null)) Overrides.FireOnTheNearest = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Nearest##m10snear", Overrides.FireOnTheNearest == true)) Overrides.FireOnTheNearest = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("Furthest##m10snear", Overrides.FireOnTheNearest == false)) Overrides.FireOnTheNearest = false;

            SettingsGrid.Row("Deep Blue's corner:");
            if (ImGui.RadioButton("Random##m10scorner", Overrides.DeepBlueSouth == null)) Overrides.DeepBlueSouth = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("North##m10scorner", Overrides.DeepBlueSouth == false)) Overrides.DeepBlueSouth = false;
            ImGui.SameLine();
            if (ImGui.RadioButton("South##m10scorner", Overrides.DeepBlueSouth == true)) Overrides.DeepBlueSouth = true;

            SettingsGrid.Row("Alley-oop:");
            if (ImGui.RadioButton("Random##m10salley2", Overrides.DoubleDip == null)) Overrides.DoubleDip = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Reverse##m10salley2", Overrides.DoubleDip == false)) Overrides.DoubleDip = false;
            ImGui.SameLine();
            if (ImGui.RadioButton("Double-dip##m10salley2", Overrides.DoubleDip == true)) Overrides.DoubleDip = true;

            SettingsGrid.Row("Wave from:");
            if (ImGui.RadioButton("Random##m10swave2", Overrides.WaveFromNorth == null)) Overrides.WaveFromNorth = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("North##m10swave2", Overrides.WaveFromNorth == true)) Overrides.WaveFromNorth = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("South##m10swave2", Overrides.WaveFromNorth == false)) Overrides.WaveFromNorth = false;
            SettingsGrid.End();
        }
    }
}
