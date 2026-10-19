using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.M10s.DeepVarial;

public sealed class M10sDeepVarialSettingsWindow
{
    private static readonly string[] LaneLabels = ["Random", "West", "Middle", "East"];

    public M10sDeepVarialStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.PlayerFire = null;
            Overrides.WaveFromNorth = null;
            Overrides.VarialSpread = null;
            Overrides.Lane = null;
            Overrides.TakeoffSpread = null;
        }

        if (SettingsGrid.Begin("##m10svarial"))
        {
            SettingsGrid.Row("Your debuff:");
            if (ImGui.RadioButton("Random##m10svelement", Overrides.PlayerFire == null)) Overrides.PlayerFire = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Fire##m10svelement", Overrides.PlayerFire == true)) Overrides.PlayerFire = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("Water##m10svelement", Overrides.PlayerFire == false)) Overrides.PlayerFire = false;

            SettingsGrid.Row("Big wave from:");
            if (ImGui.RadioButton("Random##m10svwave", Overrides.WaveFromNorth == null)) Overrides.WaveFromNorth = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("North##m10svwave", Overrides.WaveFromNorth == true)) Overrides.WaveFromNorth = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("South##m10svwave", Overrides.WaveFromNorth == false)) Overrides.WaveFromNorth = false;

            SettingsGrid.Row("Deep Varial orbs:");
            if (ImGui.RadioButton("Random##m10svorbs", Overrides.VarialSpread == null)) Overrides.VarialSpread = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("One (stack)##m10svorbs", Overrides.VarialSpread == false)) Overrides.VarialSpread = false;
            ImGui.SameLine();
            if (ImGui.RadioButton("Four (spread)##m10svorbs", Overrides.VarialSpread == true)) Overrides.VarialSpread = true;

            SettingsGrid.Row("Deep Blue's lane:");
            var lane = Overrides.Lane is { } l ? l + 2 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##m10svlane", ref lane, LaneLabels, LaneLabels.Length))
                Overrides.Lane = lane == 0 ? null : lane - 2;

            SettingsGrid.Row("Take-off orbs:");
            if (ImGui.RadioButton("Random##m10svtorbs", Overrides.TakeoffSpread == null)) Overrides.TakeoffSpread = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("Stack##m10svtorbs", Overrides.TakeoffSpread == false)) Overrides.TakeoffSpread = false;
            ImGui.SameLine();
            if (ImGui.RadioButton("Spread##m10svtorbs", Overrides.TakeoffSpread == true)) Overrides.TakeoffSpread = true;
            SettingsGrid.End();
        }
    }
}
