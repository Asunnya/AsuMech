using System.Collections.Generic;
using System.Linq;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const float UnknownDurationSeconds = 60f;
    private static readonly string[] TypeLabels = ["All", CatalogView.Ultimate, CatalogView.Savage];
    private static readonly Dictionary<string, string> GroupShortLabels = new()
    {
        [CatalogView.Ultimate] = "Ult",
        ["Heavyweight"] = "Heavy",
        ["Cruiserweight"] = "Cruiser",
        ["Light-heavyweight"] = "L-heavy",
    };

    private void DrawBrowser(bool inSession)
    {
        DrawTypeSwitch();
        var groups = CatalogView.Groups(Catalog.Zones, ExpansionScope, TypeScope);
        if (groups.Count == 0)
        {
            Muted($"No {TypeScope?.ToLowerInvariant() ?? ""} fights for {ExpansionScope ?? "this scope"} in AsuMech yet.".Replace("  ", " "));
            DrawRequestLink(FeedbackKind.Scenario);
            return;
        }

        var zone = DrawFightRow(groups);
        if (zone == null)
        {
            Muted("Pick a fight.");
            return;
        }
        DrawScenarioRow(zone);
        DrawDetailLine(inSession);
    }

    private static void DrawTypeSwitch()
    {
        for (var i = 0; i < TypeLabels.Length; i++)
        {
            if (i > 0) ImGui.SameLine(0f, 0f);
            var type = i == 0 ? null : TypeLabels[i];
            var active = TypeScope == type;
            bool clicked;
            using (active ? AsuTheme.PushAccentButton() : null)
                clicked = ImGui.SmallButton($"{TypeLabels[i]}##type");
            if (clicked && !active) SetScope(ExpansionScope, type);
        }
    }

    // The fight holding the selection, when it's in scope.
    private IZone? DrawFightRow(IReadOnlyList<FightGroup> groups)
    {
        var fights = groups.SelectMany(g => g.Fights).ToList();
        var items = new List<TabRowItem>();
        for (var g = 0; g < groups.Count; g++)
            foreach (var fight in groups[g].Fights)
                items.Add(new TabRowItem(fight.ShortName, g, Pickable(fight, out var why) ? fight.Name : why));
        var labels = groups.Select(g => GroupShortLabels.GetValueOrDefault(g.Label, g.Label)).ToList();
        var names = groups.Select(g => g.Label).ToList();
        var current = Selected?.Phase.Zone;
        var selected = current == null ? -1 : fights.IndexOf(current);

        var clicked = OverflowTabRow.Draw("fights", items, selected, labels, names);
        if (clicked >= 0 && clicked != selected && Pickable(fights[clicked], out _))
        {
            var first = ScenariosOf(fights[clicked]).FirstOrDefault(s => Pickable(s, out _));
            if (first != null) Picker.Select(first);
        }
        return Selected is { } s && fights.Contains(s.Phase.Zone) ? s.Phase.Zone : null;
    }

    private void DrawScenarioRow(IZone zone)
    {
        var scenarios = ScenariosOf(zone).ToList();
        var items = scenarios.Select(s => new TabRowItem(s.DisplayName(), Tooltip: Pickable(s, out var why) ? null : why)).ToList();
        var selected = Selected == null ? -1 : scenarios.IndexOf(Selected);
        var clicked = OverflowTabRow.Draw("scenarios", items, selected);
        if (clicked >= 0 && clicked != selected && Pickable(scenarios[clicked], out _))
            Picker.Select(scenarios[clicked]);
    }

    private void DrawDetailLine(bool inSession)
    {
        if (Selected is not { } scenario) return;
        var length = scenario.Duration is { } d ? FormatTime(d) : "length unknown";
        var best = BestStreakText(scenario);
        Muted($"{scenario.Phase.Zone.Name} · {length}{best}");

        var practice = Plugin.Practice;
        if (inSession) practice.AutoRestart = false;
        var style = ImGui.GetStyle();
        var width = ImGui.CalcTextSize("Single").X + ImGui.CalcTextSize("Loop").X + ImGui.CalcTextSize("Run to end").X
                    + style.FramePadding.X * 6 + style.ItemSpacing.X;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - width);
        ImGui.BeginDisabled(inSession);
        bool single, loop;
        using (!practice.AutoRestart ? AsuTheme.PushAccentButton() : null)
            single = ImGui.SmallButton("Single");
        ImGui.SameLine(0f, 0f);
        using (practice.AutoRestart ? AsuTheme.PushAccentButton() : null)
            loop = ImGui.SmallButton("Loop");
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(inSession
                ? "Disabled in a multiplayer session."
                : "Restart the same scenario after every clean run. A failed run turns this back off.");
        if (single) practice.AutoRestart = false;
        if (loop) practice.AutoRestart = true;
        ImGui.SameLine();
        ImGui.BeginDisabled();
        ImGui.SmallButton("Run to end");
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Needs scenario chaining in the engine.");
    }

    private static string FormatTime(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";
}
