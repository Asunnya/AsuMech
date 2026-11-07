using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Ucob.P2Nael;

public sealed class UcobP2NaelSettingsWindow
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();
    private static readonly PartyRole[] NotMainTank = Everyone.Where(r => r != PartyRole.MainTank).ToArray();
    private static readonly string[] PushLabels = ["Push (clear)", "No push (divebombs)"];

    public UcobP2NaelStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Array.Clear(Overrides.Quotes);
            Array.Clear(Overrides.FirePicks);
            Array.Clear(Overrides.ThunderIncludes);
            Array.Clear(Overrides.DoomIncludes);
            Array.Clear(Overrides.DivebombPicks);
            Overrides.DivebombQuote = null;
        }

        if (!SettingsGrid.Begin("##ucobp2")) return;
        SettingsGrid.Row("Nael push:");
        var push = (int)Overrides.Push;
        SettingsGrid.ItemWidth(220);
        ImGui.Combo("##ucobp2push", ref push, PushLabels, PushLabels.Length);
        Overrides.Push = (NaelPush)push;
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
        if (Overrides.Push == NaelPush.NoPush)
        {
            SettingsGrid.Row("Divebomb quote:");
            var labels = new[] { "Random" }.Concat(UcobP2NaelState.DivebombQuoteChoices.Select(q => $"{q.First} > {q.Second}")).ToArray();
            var selected = Overrides.DivebombQuote is { } pick ? pick + 1 : 0;
            SettingsGrid.ItemWidth(220);
            ImGui.Combo("##ucobp2divequote", ref selected, labels, labels.Length);
            Overrides.DivebombQuote = selected == 0 ? null : selected - 1;
            for (var i = 0; i < Overrides.DivebombPicks.Length; i++)
            {
                SettingsGrid.Row($"Divebomb {i + 1} on:");
                Overrides.DivebombPicks[i] = RoleCombo($"##ucobp2dive{i}", Overrides.DivebombPicks[i], NotMainTank);
            }
        }
        SettingsGrid.End();
    }

    private static PartyRole? RoleCombo(string id, PartyRole? current, PartyRole[]? choices = null)
    {
        choices ??= Everyone;
        var labels = new[] { "Random" }.Concat(choices.Select(SettingsGrid.RoleLabel)).ToArray();
        var selected = current is { } role ? Array.IndexOf(choices, role) + 1 : 0;
        SettingsGrid.ItemWidth(140);
        ImGui.Combo(id, ref selected, labels, labels.Length);
        return selected == 0 ? null : choices[selected - 1];
    }
}
