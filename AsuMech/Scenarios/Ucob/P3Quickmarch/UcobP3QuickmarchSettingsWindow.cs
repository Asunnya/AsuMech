using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;
using State = AsuMech.Scenarios.Ucob.P3Quickmarch.UcobP3QuickmarchState;

namespace AsuMech.Scenarios.Ucob.P3Quickmarch;

public sealed class UcobP3QuickmarchSettingsWindow
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    public UcobP3QuickmarchStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.DiveFrom = null;
            Overrides.EarthShakerDps = null;
            Overrides.SpreadIncludes = null;
            Overrides.PuddleIncludes = null;
        }

        if (!SettingsGrid.Begin("##ucobp3quickmarch")) return;
        SettingsGrid.Row("Dives from:");
        if (ImGui.RadioButton("Random##qmdive", Overrides.DiveFrom == null)) Overrides.DiveFrom = null;
        foreach (var d in Direction.All)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton($"{d.Name()}##qmdive", Overrides.DiveFrom == d)) Overrides.DiveFrom = d;
        }
        SettingsGrid.Row("Earth Shaker DPS:");
        Overrides.EarthShakerDps = RoleCombo("##qmearthshaker", Overrides.EarthShakerDps, State.Dps);
        SettingsGrid.Row("Megaflare spread on:");
        Overrides.SpreadIncludes = RoleCombo("##qmspread", Overrides.SpreadIncludes, State.NonTanks);
        SettingsGrid.Row("Megaflare puddle under:");
        Overrides.PuddleIncludes = RoleCombo("##qmpuddle", Overrides.PuddleIncludes, Everyone);
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
