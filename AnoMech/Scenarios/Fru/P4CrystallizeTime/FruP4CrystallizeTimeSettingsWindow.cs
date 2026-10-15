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
        if (ImGui.Button("Auto")) Select(0);
        if (SettingsGrid.Begin("##frup4crystallize"))
        {
            SettingsGrid.FightOnlyNote();
            SettingsGrid.Row("Your debuffs:");
            SettingsGrid.ItemWidth(300);
            var labels = Array.ConvertAll(PlayerChoices, c => c.Label);
            if (ImGui.Combo("##ctplayer", ref selected, labels, labels.Length)) Select(selected);
            SettingsGrid.End();
        }
    }

    private void Select(int index)
    {
        selected = index;
        Overrides.PlayerDebuffs = PlayerChoices[index].Debuffs;
    }
}
