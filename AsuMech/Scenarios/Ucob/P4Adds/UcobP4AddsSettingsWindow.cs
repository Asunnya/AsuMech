using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Scenarios.Ucob.P4Adds;

public sealed class UcobP4AddsSettingsWindow
{
    private static readonly PartyRole[] Tanks = [MainTank, OffTank];
    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();
    private static readonly string[] SecondQuoteLabels = ["Random", "... > spread", "... > stack"];

    public UcobP4AddsStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.TwintaniaTank = MainTank;
            Overrides.FirstQuote = null;
            Overrides.SecondQuote = null;
            Array.Clear(Overrides.HatchIncludes);
            Array.Clear(Overrides.RavenDiveTargets);
            Array.Clear(Overrides.BeamTargets);
        }

        if (!SettingsGrid.Begin("##ucobp4")) return;
        SettingsGrid.Row("Twintania tank:");
        var tank = Array.IndexOf(Tanks, Overrides.TwintaniaTank);
        var tankLabels = Tanks.Select(SettingsGrid.RoleLabel).ToArray();
        SettingsGrid.ItemWidth(140);
        ImGui.Combo("##ucobp4twintank", ref tank, tankLabels, tankLabels.Length);
        Overrides.TwintaniaTank = Tanks[Math.Clamp(tank, 0, 1)];

        SettingsGrid.Row("Quote 1:");
        var quoteLabels = new[] { "Random" }.Concat(UcobP4AddsState.QuoteChoices.Select(q => string.Join(" > ", q.Moves))).ToArray();
        var first = Overrides.FirstQuote is { } pick ? pick + 1 : 0;
        SettingsGrid.ItemWidth(260);
        ImGui.Combo("##ucobp4quote1", ref first, quoteLabels, quoteLabels.Length);
        Overrides.FirstQuote = first == 0 ? null : first - 1;

        SettingsGrid.Row("Quote 2:");
        var second = Overrides.SecondQuote is { } index ? index + 1 : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo("##ucobp4quote2", ref second, SecondQuoteLabels, SecondQuoteLabels.Length);
        Overrides.SecondQuote = second == 0 ? null : second - 1;

        for (var i = 0; i < Overrides.HatchIncludes.Length; i++)
        {
            SettingsGrid.Row($"Generate {i + 1} marks:");
            Overrides.HatchIncludes[i] = RoleCombo($"##ucobp4hatch{i}", Overrides.HatchIncludes[i], Dps);
        }
        for (var i = 0; i < Overrides.RavenDiveTargets.Length; i++)
        {
            SettingsGrid.Row($"Raven Dive {i + 1} on:");
            Overrides.RavenDiveTargets[i] = RoleCombo($"##ucobp4dive{i}", Overrides.RavenDiveTargets[i], Everyone);
        }
        for (var i = 0; i < Overrides.BeamTargets.Length; i++)
        {
            SettingsGrid.Row($"Thermionic Beam {i + 1} on:");
            Overrides.BeamTargets[i] = RoleCombo($"##ucobp4beam{i}", Overrides.BeamTargets[i], Everyone);
        }
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
