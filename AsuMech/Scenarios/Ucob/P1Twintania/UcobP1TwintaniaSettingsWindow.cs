using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Scenarios.Ucob.P1Twintania;

public sealed class UcobP1TwintaniaSettingsWindow
{
    private static readonly PartyRole[] NonTanks = [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    public UcobP1TwintaniaStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Array.Clear(Overrides.FireballTargets);
            Array.Clear(Overrides.HatchIncludes);
            Overrides.LiquidHellTarget = null;
        }

        if (!SettingsGrid.Begin("##ucobp1")) return;
        for (var i = 0; i < Overrides.FireballTargets.Length; i++)
        {
            SettingsGrid.Row($"Fireball {i + 1} on:");
            Overrides.FireballTargets[i] = RoleCombo($"##ucobp1fireball{i}", Overrides.FireballTargets[i], NonTanks);
        }
        for (var i = 0; i < Overrides.HatchIncludes.Length; i++)
        {
            SettingsGrid.Row($"Generate {i + 1} marks:");
            Overrides.HatchIncludes[i] = RoleCombo($"##ucobp1hatch{i}", Overrides.HatchIncludes[i], Dps);
        }
        SettingsGrid.Row("Targeted Liquid Hell on:");
        Overrides.LiquidHellTarget = RoleCombo("##ucobp1liquidhell", Overrides.LiquidHellTarget, Everyone);
        SettingsGrid.End();
    }

    private static PartyRole? RoleCombo(string id, PartyRole? current, PartyRole[] roles)
    {
        var labels = new[] { "Random" }.Concat(roles.Select(SettingsGrid.RoleLabel)).ToArray();
        var selected = current is { } role ? Array.IndexOf(roles, role) + 1 : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : roles[selected - 1];
    }
}
