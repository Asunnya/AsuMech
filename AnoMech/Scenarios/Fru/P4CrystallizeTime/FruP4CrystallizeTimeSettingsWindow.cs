using System;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Fru.P4CrystallizeTime;

public sealed class FruP4CrystallizeTimeSettingsWindow
{
    private static readonly (string Label, CrystallizeDebuff[]? Debuffs)[] PlayerChoices =
    [
        ("Random", null),
        ("Red + Ice", [CrystallizeDebuff.RedIce]),
        ("Red + Aero", [CrystallizeDebuff.RedAero]),
        ("Blue + Darkness", [CrystallizeDebuff.BlueDarkness]),
        ("Blue + Ice / Water / Eruption", [CrystallizeDebuff.BlueIce, CrystallizeDebuff.BlueWater, CrystallizeDebuff.BlueEruption]),
    ];

    private int selected;

    public FruP4CrystallizeTimeStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) Reset();
        if (SettingsGrid.Begin("##frup4crystallize"))
        {
            SettingsGrid.FightOnlyNote();
            SettingsGrid.Row("Your debuffs:");
            SettingsGrid.ItemWidth(300);
            var labels = Array.ConvertAll(PlayerChoices, c => c.Label);
            if (ImGui.Combo("##ctplayer", ref selected, labels, labels.Length)) Select(selected);

            var slow = Overrides.SlowNorthwest;
            SettingsGrid.Row("Slowed hourglasses:");
            if (ImGui.RadioButton("Auto##ctslow", slow == null)) Overrides.SlowNorthwest = null;
            ImGui.SameLine();
            if (ImGui.RadioButton("NW + SE##ctslow", slow == true)) Overrides.SlowNorthwest = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("NE + SW##ctslow", slow == false)) Overrides.SlowNorthwest = false;

            var corner = Overrides.Corner;
            SettingsGrid.Row("Last safe corner:");
            if (ImGui.RadioButton("Auto##ctcorner", corner == null)) Overrides.Corner = null;
            foreach (var choice in Enum.GetValues<CrystallizeCorner>())
            {
                ImGui.SameLine();
                if (ImGui.RadioButton($"{choice}##ctcorner", corner == choice)) Overrides.Corner = choice;
            }
            SettingsGrid.End();
        }
    }

    private void Select(int index)
    {
        selected = index;
        Overrides.PlayerDebuffs = PlayerChoices[index].Debuffs;
    }

    private void Reset()
    {
        Select(0);
        Overrides.SlowNorthwest = null;
        Overrides.Corner = null;
    }
}
