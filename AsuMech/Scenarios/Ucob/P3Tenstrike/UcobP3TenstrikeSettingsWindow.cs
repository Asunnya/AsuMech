using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Ucob.P3Tenstrike;

public sealed class UcobP3TenstrikeSettingsWindow
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    public UcobP3TenstrikeStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.HatchInclude = null;
            Overrides.FirstEarthShakerInclude = null;
            Overrides.SecondEarthShakerInclude = null;
        }

        if (!SettingsGrid.Begin("##ucobp3tenstrike")) return;
        SettingsGrid.Row("Hatch marks:");
        Overrides.HatchInclude = RoleCombo("##ucobp3hatch", Overrides.HatchInclude, UcobP3TenstrikeState.HatchPool);
        SettingsGrid.Row("First Earth Shakers mark:");
        Overrides.FirstEarthShakerInclude = RoleCombo("##ucobp3shaker1", Overrides.FirstEarthShakerInclude, Everyone);
        SettingsGrid.Row("Second Earth Shakers mark:");
        Overrides.SecondEarthShakerInclude = RoleCombo("##ucobp3shaker2", Overrides.SecondEarthShakerInclude, Everyone);
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
