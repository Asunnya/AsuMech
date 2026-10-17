using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.SickestTakeoff;

public sealed class M10sSickestTakeoffSettingsWindow
{
    private static readonly string[] WaveLabels = ["Random", "North", "East", "South", "West"];
    private static readonly string[] LaneLabels = ["Random", "Right", "Middle", "Left"];

    public M10sSickestTakeoffStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.WaveFrom = null;
            Overrides.Lane = null;
            Overrides.EightOrbs = null;
            Overrides.DoubleDip = null;
        }

        if (SettingsGrid.Begin("##m10ssickest"))
        {
            SettingsGrid.Row("Wave from:");
            var wave = Overrides.WaveFrom is { } w ? w + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m10swave", ref wave, WaveLabels, WaveLabels.Length))
                Overrides.WaveFrom = wave == 0 ? null : wave - 1;

            SettingsGrid.Row("Deep Blue's lane:");
            var lane = Overrides.Lane is { } l ? l + 2 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m10slane", ref lane, LaneLabels, LaneLabels.Length))
                Overrides.Lane = lane == 0 ? null : lane - 2;

            SettingsGrid.Row("Orbs:");
            if (ImGui.RadioButton("Random##m10sorbs", Overrides.EightOrbs == null)) Overrides.EightOrbs = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Two (stacks)##m10sorbs", Overrides.EightOrbs == false)) Overrides.EightOrbs = false;
            ImGui.SameLine();
            if (ImGui.RadioButton("Eight (spread)##m10sorbs", Overrides.EightOrbs == true)) Overrides.EightOrbs = true;

            SettingsGrid.Row("Alley-oop:");
            if (ImGui.RadioButton("Random##m10salley", Overrides.DoubleDip == null)) Overrides.DoubleDip = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Reverse##m10salley", Overrides.DoubleDip == false)) Overrides.DoubleDip = false;
            ImGui.SameLine();
            if (ImGui.RadioButton("Double-dip##m10salley", Overrides.DoubleDip == true)) Overrides.DoubleDip = true;
            SettingsGrid.End();
        }
    }
}
