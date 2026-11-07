using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Ucob.P3GrandOctet;

public sealed class UcobP3GrandOctetSettingsWindow
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();
    private static readonly string[] SlotNames = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];

    public UcobP3GrandOctetStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.BahamutSlot = null;
            Overrides.TwintaniaTarget = null;
            Overrides.StackInclude = null;
        }

        if (!SettingsGrid.Begin("##ucobp3octet")) return;
        SettingsGrid.Row("Bahamut on:");
        if (ImGui.RadioButton("Random##octetbahamut", Overrides.BahamutSlot == null)) Overrides.BahamutSlot = null;
        for (var slot = 0; slot < SlotNames.Length; slot++)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton($"{SlotNames[slot]}##octetbahamut", Overrides.BahamutSlot == slot)) Overrides.BahamutSlot = slot;
        }
        SettingsGrid.Row("Twintania's dive on:");
        Overrides.TwintaniaTarget = RoleCombo("##octettwin", Overrides.TwintaniaTarget);
        SettingsGrid.Row("Megaflare stack on:");
        Overrides.StackInclude = RoleCombo("##octetstack", Overrides.StackInclude);
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
