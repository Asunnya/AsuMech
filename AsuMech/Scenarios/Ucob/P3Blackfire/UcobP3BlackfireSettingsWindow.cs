using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Ucob.P3Blackfire;

public sealed class UcobP3BlackfireSettingsWindow
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    public UcobP3BlackfireStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.NaelFrom = null;
            Overrides.LiquidHellTarget = null;
            Overrides.StackIncludes = null;
            Overrides.TowerIncludes = null;
        }

        if (!SettingsGrid.Begin("##ucobp3blackfire")) return;
        SettingsGrid.Row("Nael lands:");
        if (ImGui.RadioButton("Random##bfnael", Overrides.NaelFrom == null)) Overrides.NaelFrom = null;
        foreach (var d in Direction.All)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton($"{d.Name()}##bfnael", Overrides.NaelFrom == d)) Overrides.NaelFrom = d;
        }
        SettingsGrid.Row("Liquid Hell chases:");
        Overrides.LiquidHellTarget = RoleCombo("##bfliquidhell", Overrides.LiquidHellTarget);
        SettingsGrid.Row("Megaflare stack on:");
        Overrides.StackIncludes = RoleCombo("##bfstack", Overrides.StackIncludes);
        SettingsGrid.Row("Megaflare tower for:");
        Overrides.TowerIncludes = RoleCombo("##bftower", Overrides.TowerIncludes);
        SettingsGrid.End();
    }

    private static PartyRole? RoleCombo(string id, PartyRole? current)
    {
        var labels = new[] { "Random" }.Concat(Everyone.Select(SettingsGrid.RoleLabel)).ToArray();
        var selected = current is { } role ? Array.IndexOf(Everyone, role) + 1 : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : Everyone[selected - 1];
    }
}
