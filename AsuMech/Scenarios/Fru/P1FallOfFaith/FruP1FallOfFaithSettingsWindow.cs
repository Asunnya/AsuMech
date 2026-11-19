using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Fru.P1UtopianSky;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Fru.P1FallOfFaith;

public sealed class FruP1FallOfFaithSettingsWindow
{
    public FruP1FallOfFaithStateOverrides Overrides { get; } = new();

    private PartyRole editingSeat = PartyRole.MainTank;

    private static readonly string[] PatternLabels =
        new[] { "Random" }.Concat(FruP1FallOfFaithState.TowerPatterns.Select(p => string.Join("/", p))).ToArray();

    public void Draw()
    {
        var solo = !PerRole.SeatsActive;
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##frup1faith"))
        {
            if (solo) DrawTether();
            for (var k = 0; k < 4; k++)
            {
                var faith = k;
                FruP1UtopianSkySettingsWindow.ElementRow($"Fall of Faith {faith + 1}:", $"##fof{faith}", Overrides.FaithFire[faith], v => Overrides.FaithFire[faith] = v);
            }
            FruP1UtopianSkySettingsWindow.ElementRow("Burnt Strike:", "##fofstrike", Overrides.TowersFire, v => Overrides.TowersFire = v);
            DrawTowerSide();
            DrawTowerPattern();
            SettingsGrid.End();
        }
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) Overrides.Tether.Clear();
        if (SettingsGrid.Begin("##frup1faithplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##fofseat", editingSeat);
            DrawTether();
            SettingsGrid.ForcedRecapRow("Tethers set:", Overrides.Tether);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }

    private void ResetAll()
    {
        Array.Clear(Overrides.FaithFire);
        Overrides.TowersWest = null;
        Overrides.TowersFire = null;
        Overrides.TowerPattern = null;
        Overrides.Tether.Mine = null;
    }

    private void DrawTether()
    {
        var v = Overrides.Tether.Effective(editingSeat);
        SettingsGrid.PlayerRow("Fall of Faith:");
        if (ImGui.RadioButton("Auto##foftether", v == null)) Overrides.Tether.Set(editingSeat, null);
        foreach (var slot in Enum.GetValues<FallOfFaithTether>())
        {
            ImGui.SameLine();
            var label = slot == FallOfFaithTether.None ? "None" : ((int)slot).ToString();
            if (ImGui.RadioButton($"{label}##foftether", v == slot)) Overrides.Tether.Set(editingSeat, slot);
        }
    }

    private void DrawTowerSide()
    {
        var v = Overrides.TowersWest;
        SettingsGrid.Row("Towers:");
        if (ImGui.RadioButton("Auto##fofside", v == null)) Overrides.TowersWest = null;
        ImGui.SameLine();
        if (ImGui.RadioButton("West##fofside", v == true)) Overrides.TowersWest = true;
        ImGui.SameLine();
        if (ImGui.RadioButton("East##fofside", v == false)) Overrides.TowersWest = false;
    }

    private void DrawTowerPattern()
    {
        SettingsGrid.Row("Soakers N/mid/S:");
        var selected = (Overrides.TowerPattern ?? -1) + 1;
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo("##fofpattern", ref selected, PatternLabels, PatternLabels.Length))
            Overrides.TowerPattern = selected == 0 ? null : selected - 1;
    }
}
