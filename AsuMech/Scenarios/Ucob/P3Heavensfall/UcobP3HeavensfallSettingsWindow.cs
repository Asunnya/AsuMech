using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Scenarios.Ucob.P3Heavensfall;

public sealed class UcobP3HeavensfallSettingsWindow
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();
    private static readonly HeavensfallFormation[] Formations = Enum.GetValues<HeavensfallFormation>();
    private static readonly string[] FormationLabels = ["Random", "Nael between", "Nael left", "Nael right"];
    private static readonly string[] SweepLabels = ["Random", "Clockwise", "Counter-clockwise"];

    public UcobP3HeavensfallStateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto"))
        {
            Overrides.Formation = null;
            Overrides.BurstsClockwise = null;
            Overrides.DiveIncludes = null;
            Overrides.TwisterIncludes = null;
            Overrides.MegaflarePuddleIncludes = null;
            Overrides.FireballTarget = null;
        }

        if (!SettingsGrid.Begin("##ucobp3heavensfall")) return;
        SettingsGrid.Row("Nael lands:");
        var formation = Overrides.Formation is { } f ? Array.IndexOf(Formations, f) + 1 : 0;
        SettingsGrid.ItemWidth(160);
        ImGui.Combo("##ucobp3heavensfallformation", ref formation, FormationLabels, FormationLabels.Length);
        Overrides.Formation = formation == 0 ? null : Formations[formation - 1];
        SettingsGrid.Row("Pie slices sweep:");
        var sweep = Overrides.BurstsClockwise switch { true => 1, false => 2, null => 0 };
        SettingsGrid.ItemWidth(160);
        ImGui.Combo("##ucobp3heavensfallsweep", ref sweep, SweepLabels, SweepLabels.Length);
        Overrides.BurstsClockwise = sweep switch { 1 => true, 2 => false, _ => null };
        SettingsGrid.Row("Dive marker on:");
        Overrides.DiveIncludes = RoleCombo("##ucobp3heavensfalldive", Overrides.DiveIncludes);
        SettingsGrid.Row("Twister on:");
        Overrides.TwisterIncludes = RoleCombo("##ucobp3heavensfalltwister", Overrides.TwisterIncludes);
        SettingsGrid.Row("Megaflare puddle on:");
        Overrides.MegaflarePuddleIncludes = RoleCombo("##ucobp3heavensfallpuddle", Overrides.MegaflarePuddleIncludes);
        SettingsGrid.Row("Fireball on:");
        Overrides.FireballTarget = RoleCombo("##ucobp3heavensfallfireball", Overrides.FireballTarget);
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
