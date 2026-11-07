using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Ucob.P3Fellruin;

public sealed class UcobP3FellruinSettingsWindow
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    public UcobP3FellruinStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.Quote = null;
            Overrides.RavenDiveTarget = null;
            Overrides.MeteorIncludes = null;
        }

        if (!SettingsGrid.Begin("##ucobp3fellruin")) return;
        SettingsGrid.Row("Quote:");
        var labels = new[] { "Random" }.Concat(UcobP3FellruinState.QuoteChoices.Select(q => $"{q.First} > {q.Second}")).ToArray();
        var selected = Overrides.Quote is { } pick ? pick + 1 : 0;
        SettingsGrid.ItemWidth(220);
        ImGui.Combo("##ucobp3fellruinquote", ref selected, labels, labels.Length);
        Overrides.Quote = selected == 0 ? null : selected - 1;
        SettingsGrid.Row("Raven Dive on:");
        Overrides.RavenDiveTarget = RoleCombo("##ucobp3fellruindive", Overrides.RavenDiveTarget);
        SettingsGrid.Row("Meteor Stream on:");
        Overrides.MeteorIncludes = RoleCombo("##ucobp3fellruinmeteor", Overrides.MeteorIncludes);
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
