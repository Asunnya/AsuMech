using System;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Umad.P3DecisiveBattle;

public sealed class UmadP3DecisiveBattleSettingsWindow
{
    public UmadP3DecisiveBattleStateOverrides Overrides { get; } = new();

    private static readonly string[] RoleLabels =
        ["Random", "Main tank", "Off tank", "Regen healer", "Shield healer", "Melee A", "Melee B", "Phys ranged", "Caster"];
    private static readonly string[] SpotLabels = ["Random", "North-east", "South-east", "South-west", "North-west"];

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.FireShort = Overrides.Latitudinal = Overrides.FireClockwiseOfWind = null;
            Overrides.EntropyOn = Overrides.DynamicFluidOn = null;
            Overrides.WindCrystalSpot = null;
        }

        if (!SettingsGrid.Begin("##umadp3decisive")) return;
        Choice("Short debuffs:", "##short", Overrides.FireShort, ["Random", "Water (Dynamic Fluid)", "Fire (Entropy)"], v => Overrides.FireShort = v);
        RoleChoice("Entropy on:", "##entropy", Overrides.EntropyOn, v => Overrides.EntropyOn = v);
        RoleChoice("Dynamic Fluid on:", "##fluid", Overrides.DynamicFluidOn, v => Overrides.DynamicFluidOn = v);
        SettingsGrid.Row("Wind crystal:");
        var spot = (Overrides.WindCrystalSpot ?? -1) + 1;
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo("##windspot", ref spot, SpotLabels, SpotLabels.Length))
            Overrides.WindCrystalSpot = spot == 0 ? null : spot - 1;
        Choice("Fire crystal:", "##firecw", Overrides.FireClockwiseOfWind, ["Random", "Counter-clockwise of wind", "Clockwise of wind"], v => Overrides.FireClockwiseOfWind = v);
        Choice("Implosion:", "##implosion", Overrides.Latitudinal, ["Random", "Longitudinal", "Latitudinal"], v => Overrides.Latitudinal = v);
        SettingsGrid.End();
    }

    // Labels: Random, then the false option, then the true one.
    private static void Choice(string label, string id, bool? current, string[] labels, Action<bool?> set)
    {
        SettingsGrid.Row(label);
        var index = current switch { null => 0, false => 1, true => 2 };
        SettingsGrid.ItemWidth(160);
        if (ImGui.Combo(id, ref index, labels, labels.Length))
            set(index switch { 1 => false, 2 => true, _ => null });
    }

    private static void RoleChoice(string label, string id, PartyRole? current, Action<PartyRole?> set)
    {
        SettingsGrid.Row(label);
        var index = current is { } role ? (int)role + 1 : 0;
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo(id, ref index, RoleLabels, RoleLabels.Length))
            set(index == 0 ? null : (PartyRole)(index - 1));
    }
}
