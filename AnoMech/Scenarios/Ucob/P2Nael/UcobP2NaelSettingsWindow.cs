using System;
using System.Linq;
using AnoMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Ucob.P2Nael;

public sealed class UcobP2NaelSettingsWindow
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    public UcobP2NaelStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Array.Clear(Overrides.Quotes);
            Array.Clear(Overrides.FirePicks);
            Array.Clear(Overrides.ThunderIncludes);
            Array.Clear(Overrides.DoomIncludes);
        }

        if (!SettingsGrid.Begin("##ucobp2")) return;
        for (var i = 0; i < Overrides.Quotes.Length; i++)
        {
            SettingsGrid.Row($"Quote {i + 1}:");
            var labels = new[] { "Random" }.Concat(UcobP2NaelState.QuoteChoices[i].Select(q => $"{q.First} > {q.Second}")).ToArray();
            var selected = Overrides.Quotes[i] is { } pick ? pick + 1 : 0;
            SettingsGrid.ItemWidth(220);
            ImGui.Combo($"##ucobp2quote{i}", ref selected, labels, labels.Length);
            Overrides.Quotes[i] = selected == 0 ? null : selected - 1;
        }
        for (var i = 0; i < Overrides.FirePicks.Length; i++)
        {
            SettingsGrid.Row($"Fire tether {i + 1} on:");
            Overrides.FirePicks[i] = RoleCombo($"##ucobp2fire{i}", Overrides.FirePicks[i]);
        }
        for (var i = 0; i < Overrides.ThunderIncludes.Length; i++)
        {
            SettingsGrid.Row($"Thunderstruck {i + 1} on:");
            Overrides.ThunderIncludes[i] = RoleCombo($"##ucobp2thunder{i}", Overrides.ThunderIncludes[i]);
        }
        for (var i = 0; i < Overrides.DoomIncludes.Length; i++)
        {
            SettingsGrid.Row($"Doom {i + 1} on:");
            Overrides.DoomIncludes[i] = RoleCombo($"##ucobp2doom{i}", Overrides.DoomIncludes[i]);
        }
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
