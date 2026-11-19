using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Umad.P1Opening;

public sealed class UmadP1OpeningSettingsWindow
{
    public UmadP1OpeningStateOverrides Overrides { get; } = new();

    private static readonly string[] RoleLabels =
        ["Random", "Main tank", "Off tank", "Regen healer", "Shield healer", "Melee A", "Melee B", "Phys ranged", "Caster"];

    private static readonly PartyRole[] StackRoles = [.. UmadP1OpeningState.Healers, .. UmadP1OpeningState.Dps];

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.TetheredAreDps = Overrides.FireIsStack = Overrides.FireIsLie = null;
            Overrides.FirstIceIsLie = Overrides.SecondIceIsLie = Overrides.ThunderIsLie = Overrides.ThirdIceIsLie = null;
            Overrides.FirstGravitasOnDps = Overrides.SecondGravitasOnDps = Overrides.FirstWillEast = Overrides.SecondWillEast = null;
            Overrides.FireStackOn = Overrides.LaserOn = Overrides.FirstConfettiOn = Overrides.SecondConfettiOn = null;
        }

        if (!SettingsGrid.Begin("##umadp1opening")) return;
        SettingsGrid.Section("First Graven Image");
        Choice("Knockback tethers:", "##tethered", Overrides.TetheredAreDps, ["Random", "Supports", "DPS"], v => Overrides.TetheredAreDps = v);
        Choice("Fire:", "##fire", Overrides.FireIsStack, ["Random", "Spread", "Stack"], v => Overrides.FireIsStack = v);
        Choice("Fire orb:", "##firelie", Overrides.FireIsLie, ["Random", "True", "? (lie)"], v => Overrides.FireIsLie = v);
        RoleChoice("Stack marker on:", "##firestack", Overrides.FireStackOn, v => Overrides.FireStackOn = v, StackRoles);
        Choice("First ice orb:", "##ice1", Overrides.FirstIceIsLie, ["Random", "True", "? (lie)"], v => Overrides.FirstIceIsLie = v);
        RoleChoice("Wave Cannon on:", "##laser", Overrides.LaserOn, v => Overrides.LaserOn = v);
        RoleChoice("First Double-trouble Trap on:", "##confetti1", Overrides.FirstConfettiOn, v => Overrides.FirstConfettiOn = v);
        Choice("Second ice orb:", "##ice2", Overrides.SecondIceIsLie, ["Random", "True", "? (lie)"], v => Overrides.SecondIceIsLie = v);
        Choice("Lightning orb:", "##thunder", Overrides.ThunderIsLie, ["Random", "True", "? (lie)"], v => Overrides.ThunderIsLie = v);
        SettingsGrid.Section("Second Graven Image");
        Choice("Ice orb:", "##ice3", Overrides.ThirdIceIsLie, ["Random", "True", "? (lie)"], v => Overrides.ThirdIceIsLie = v);
        Choice("First gravity tethers:", "##grav1", Overrides.FirstGravitasOnDps, ["Random", "Supports", "DPS"], v => Overrides.FirstGravitasOnDps = v);
        Choice("First Will:", "##will1", Overrides.FirstWillEast, ["Random", "West half", "East half"], v => Overrides.FirstWillEast = v);
        Choice("Second gravity tethers:", "##grav2", Overrides.SecondGravitasOnDps, ["Random", "Supports", "DPS"], v => Overrides.SecondGravitasOnDps = v);
        Choice("Second Will:", "##will2", Overrides.SecondWillEast, ["Random", "West half", "East half"], v => Overrides.SecondWillEast = v);
        RoleChoice("Second Double-trouble Trap on:", "##confetti2", Overrides.SecondConfettiOn, v => Overrides.SecondConfettiOn = v);
        SettingsGrid.End();
    }

    // Labels: Random, then the false option, then the true one.
    private static void Choice(string label, string id, bool? current, string[] labels, Action<bool?> set)
    {
        SettingsGrid.Row(label);
        var index = current switch { null => 0, false => 1, true => 2 };
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo(id, ref index, labels, labels.Length))
            set(index switch { 1 => false, 2 => true, _ => null });
    }

    private static void RoleChoice(string label, string id, PartyRole? current, Action<PartyRole?> set, PartyRole[]? roles = null)
    {
        roles ??= Enum.GetValues<PartyRole>();
        var labels = roles.Select(r => RoleLabels[(int)r + 1]).Prepend(RoleLabels[0]).ToArray();
        SettingsGrid.Row(label);
        var index = current is { } role ? Array.IndexOf(roles, role) + 1 : 0;
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo(id, ref index, labels, labels.Length))
            set(index == 0 ? null : roles[index - 1]);
    }
}
