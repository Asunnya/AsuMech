using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.XtremeSnaking;

public sealed class M10sXtremeSnakingSettingsWindow
{
    public M10sXtremeSnakingStateOverrides Overrides { get; } = new();

    private static readonly string[] PatternNames = ["Snap, Vertical, Re-entry, Snap", "Snap, Re-entry, Snap, Vertical", "Vertical, Snap, Re-entry, Snap", "Snap, Re-entry, Vertical, Snap"];

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerFire = null;
            Overrides.FireOnTheNearest = null;
            Overrides.Pattern = null;
            Overrides.RedHotEndsInTheMiddle = null;
        }

        if (SettingsGrid.Begin("##m10sxtreme"))
        {
            SettingsGrid.Row("Your debuff:");
            if (ImGui.RadioButton("Random##m10sxelement", Overrides.PlayerFire == null)) Overrides.PlayerFire = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Fire##m10sxelement", Overrides.PlayerFire == true)) Overrides.PlayerFire = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("Water##m10sxelement", Overrides.PlayerFire == false)) Overrides.PlayerFire = false;

            SettingsGrid.Row("Firesnaking takes:");
            if (ImGui.RadioButton("Random##m10sxnear", Overrides.FireOnTheNearest == null)) Overrides.FireOnTheNearest = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Nearest##m10sxnear", Overrides.FireOnTheNearest == true)) Overrides.FireOnTheNearest = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("Furthest##m10sxnear", Overrides.FireOnTheNearest == false)) Overrides.FireOnTheNearest = false;

            SettingsGrid.Row("Surfboard moves:");
            if (ImGui.RadioButton("Random##m10sxpattern", Overrides.Pattern == null)) Overrides.Pattern = null;
            for (var i = 0; i < PatternNames.Length; i++)
                if (ImGui.RadioButton($"{PatternNames[i]}##m10sxpattern{i}", Overrides.Pattern == i)) Overrides.Pattern = i;

            SettingsGrid.Row("Red Hot ends:");
            if (ImGui.RadioButton("Random##m10sxend", Overrides.RedHotEndsInTheMiddle == null)) Overrides.RedHotEndsInTheMiddle = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("In the middle##m10sxend", Overrides.RedHotEndsInTheMiddle == true)) Overrides.RedHotEndsInTheMiddle = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("In the corner##m10sxend", Overrides.RedHotEndsInTheMiddle == false)) Overrides.RedHotEndsInTheMiddle = false;
            SettingsGrid.End();
        }
    }
}
