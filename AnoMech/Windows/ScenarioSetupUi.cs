using System.Collections.Generic;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AnoMech.Windows;

// The setup rows (role, region, strat, waymarks) over the shared ScenarioPicker, drawn inside a
// SettingsGrid by both main windows.
internal static class ScenarioSetupUi
{
    private const float DropdownWidth = 180f;

    // Index 0 = Auto (null override); indices 1..8 map to (PartyRole)(idx - 1).
    internal static readonly string[] RoleLabels = ["Auto", "MT", "OT", "H1", "H2", "M1", "M2", "R1", "R2"];

    internal static void DrawRoleSelector(ScenarioPicker picker)
    {
        var idx = picker.RoleOverride is { } role ? (int)role + 1 : 0;
        SettingsGrid.Row("Role:");
        ImGui.SetNextItemWidth(DropdownWidth * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("##role", ref idx, RoleLabels, RoleLabels.Length))
            picker.RoleOverride = idx == 0 ? null : (PartyRole)(idx - 1);
    }

    // Hidden unless the scenario offers a choice. Grouped scenarios get a region-button row above a
    // dropdown filtered to that region.
    internal static void DrawStratSelector(ScenarioPicker picker, bool guest)
    {
        if (picker.Scenario is not { } scenario) return;
        var strats = scenario.AiStrats;
        var groups = ScenarioPicker.StratGroups(scenario);
        if (groups.Count == 0 && strats.Count <= 1) return;
        ImGui.BeginDisabled(guest);
        if (groups.Count > 0)
            DrawGroupedStratSelector(picker, strats, groups);
        else
        {
            var labels = new string[strats.Count];
            for (var i = 0; i < strats.Count; i++) labels[i] = strats[i].Name;
            SettingsGrid.Row("Strategy:");
            ImGui.SetNextItemWidth(DropdownWidth * ImGuiHelpers.GlobalScale);
            var strat = picker.Strat;
            if (ImGui.Combo("##strat", ref strat, labels, labels.Length)) picker.SelectStrat(strat);
        }
        ImGui.EndDisabled();
        if (guest && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Only the host's selection is used in multiplayer.");
    }

    private static void DrawGroupedStratSelector(ScenarioPicker picker, IReadOnlyList<IScenarioAi> strats, IReadOnlyList<string> groups)
    {
        SettingsGrid.Row("Region:");
        for (var i = 0; i < groups.Count; i++)
        {
            if (i > 0) ImGui.SameLine();
            var group = groups[i];
            var selected = picker.StratGroup == group;
            if (selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
            ImGui.PushID($"region{i}");
            if (ImGui.Button(group)) picker.SelectGroup(group);
            ImGui.PopID();
            if (selected) ImGui.PopStyleColor();
        }

        var filtered = picker.RegionStrats();
        SettingsGrid.Row("Strategy:");
        if (filtered.Count == 0)
        {
            ImGui.TextDisabled("(no strats for this region yet)");
            return;
        }

        var localIdx = filtered.IndexOf(picker.Strat);
        var labels = new string[filtered.Count];
        for (var i = 0; i < filtered.Count; i++) labels[i] = strats[filtered[i]].Name;
        ImGui.SetNextItemWidth(DropdownWidth * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("##strat", ref localIdx, labels, labels.Length))
            picker.SelectStrat(filtered[localIdx]);
    }

    // Changing it while a scenario is loaded re-places the markers immediately.
    internal static void DrawWaymarkSelector(ScenarioPicker picker)
    {
        if (picker.Scenario is not { } scenario) return;
        var presets = scenario.Phase.Zone.WaymarkPresets;
        if (presets.Count <= 1) return;

        var labels = new string[presets.Count];
        for (var i = 0; i < presets.Count; i++) labels[i] = presets[i].Name;

        SettingsGrid.Row("Waymarks:");
        ImGui.SetNextItemWidth(DropdownWidth * ImGuiHelpers.GlobalScale);
        var waymark = picker.Waymark;
        if (ImGui.Combo("##waymarks", ref waymark, labels, labels.Length))
        {
            picker.SelectWaymark(waymark);
            if (Plugin.Game.World.Map.IsInInstance)
                Plugin.Game.World.PlaceWaymarks(presets[waymark].Markers);
        }
    }

    internal static void DrawSetupGrid(ScenarioPicker picker, bool inSession, bool guest)
    {
        if (!SettingsGrid.Begin("##scenario-setup-grid")) return;
        if (!inSession) DrawRoleSelector(picker);
        DrawStratSelector(picker, guest);
        DrawWaymarkSelector(picker);
        SettingsGrid.End();
    }
}
