using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.AlleyOopTwo;

public sealed class M10sAlleyOopTwoSettingsWindow
{
    public M10sAlleyOopTwoStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.DoubleDip = null;
            Overrides.PyrotationTarget = null;
        }

        if (SettingsGrid.Begin("##m10salleytwo"))
        {
            SettingsGrid.Row("Alley-oop:");
            if (ImGui.RadioButton("Random##m10sa2dip", Overrides.DoubleDip == null)) Overrides.DoubleDip = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Reverse##m10sa2dip", Overrides.DoubleDip == false)) Overrides.DoubleDip = false;
            ImGui.SameLine();
            if (ImGui.RadioButton("Double-dip##m10sa2dip", Overrides.DoubleDip == true)) Overrides.DoubleDip = true;

            SettingsGrid.Row("Pyrotation on:");
            if (ImGui.RadioButton("Random##m10sa2pyro", Overrides.PyrotationTarget == null)) Overrides.PyrotationTarget = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("H1##m10sa2pyro", Overrides.PyrotationTarget == PartyRole.RegenHealer)) Overrides.PyrotationTarget = PartyRole.RegenHealer;
            ImGui.SameLine();
            if (ImGui.RadioButton("H2##m10sa2pyro", Overrides.PyrotationTarget == PartyRole.ShieldHealer)) Overrides.PyrotationTarget = PartyRole.ShieldHealer;
            SettingsGrid.End();
        }
    }
}
