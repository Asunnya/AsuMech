using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Scenarios;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const float UnknownDurationSeconds = 60f;
    private float detailRightWidth;
    private const string WholePhaseLabel = "Whole phase";
    private readonly Dictionary<IScenario, IScenario> lastPart = new();
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

        var draw = ImGui.GetWindowDrawList();
        var top = ImGui.GetCursorScreenPos().Y;
        draw.ChannelsSplit(2);
        draw.ChannelsSetCurrent(1);
        var zone = DrawFightRow(groups);
        if (zone != null) DrawScenarioRow(zone);
        DrawHeaderBand(draw, top);
        if (zone == null)
        {
            Muted("Pick a fight.");
            return;
        }
        DrawPartRow(inSession);
        DrawSectionRow();
        DrawDetailLine(inSession);
    }

    // A scenario played one stretch at a time picks the stretch here, where it can't be missed.
    private void DrawSectionRow()
    {
        if (Selected is not ISectionedScenario sectioned) return;
        var runToEnd = Plugin.Practice.RunToEnd;
        var items = sectioned.Sections
            .Select(s => new TabRowItem(s.Label, Tooltip: runToEnd ? "Run to end plays the whole phase." : s.Covers)).ToList();
        var selected = runToEnd ? 0 : sectioned.SelectedSection;
        ImGui.BeginDisabled(runToEnd || RunState.IsGuest);
        var clicked = OverflowTabRow.Draw("sections", items, selected);
        ImGui.EndDisabled();
        if (clicked >= 0 && clicked != selected) sectioned.SelectedSection = clicked;
        if (selected >= 0 && selected < items.Count) Muted(sectioned.Sections[selected].Covers);
    }

    // Edge to edge behind the tab rows, drawn on the channel under them.
    private static void DrawHeaderBand(ImDrawListPtr draw, float top)
    {
        var halfGap = ImGui.GetStyle().ItemSpacing.Y * 0.5f;
        var bottom = ImGui.GetCursorScreenPos().Y - halfGap;
        var left = ImGui.GetWindowPos().X;
        draw.ChannelsSetCurrent(0);
        draw.AddRectFilled(new Vector2(left, top - halfGap), new Vector2(left + ImGui.GetWindowSize().X, bottom), AsuTheme.U32(AsuTheme.HeaderBand));
        draw.ChannelsMerge();
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
        var groups = Catalog.PhaseGroupsOf(zone);
        var items = groups.Select(g => g.Count > 1
            ? new TabRowItem(g[0].Phase.Name, Tooltip: string.Join(" · ", g.Select(s => s.Name)))
            : new TabRowItem(g[0].DisplayName(),
                Tooltip: Pickable(g[0], out var why) ? BestStreakText(g[0]).TrimStart(' ', '·') is { Length: > 0 } best ? best : null : why)).ToList();
        var selected = -1;
        for (var i = 0; i < groups.Count; i++)
            if (Selected != null && groups[i].Contains(Selected)) selected = i;
        var clicked = OverflowTabRow.Draw("scenarios", items, selected);
        if (clicked < 0 || clicked == selected) return;
        var parts = groups[clicked];
        var pick = lastPart.GetValueOrDefault(parts[0]) is { } last && Pickable(last, out _) ? last : parts.FirstOrDefault(s => Pickable(s, out _));
        if (pick != null) Picker.Select(pick);
    }

    // A phase split into parts picks the part here, under the phase tabs.
    private void DrawPartRow(bool inSession)
    {
        if (Selected is not { } scenario) return;
        var parts = Catalog.PartsOf(scenario);
        var practice = Plugin.Practice;
        if (parts.Count < 2)
        {
            practice.WholePhase = false;
            return;
        }
        lastPart[parts[0]] = scenario;
        var items = new List<TabRowItem> { new(WholePhaseLabel, Tooltip: inSession ? "Disabled in a multiplayer session." : $"Start plays all of {scenario.Phase.Name}, each part going straight on to the next. A death ends the run.") };
        items.AddRange(parts.Select(s => new TabRowItem(s.Name,
            Tooltip: Pickable(s, out var why) ? BestStreakText(s).TrimStart(' ', '·') is { Length: > 0 } best ? best : null : why)));
        var selected = practice.WholePhase ? 0 : parts.ToList().IndexOf(scenario) + 1;
        ImGui.BeginDisabled(RunState.IsGuest);
        var clicked = OverflowTabRow.Draw("parts", items, selected);
        ImGui.EndDisabled();
        if (clicked < 0 || clicked == selected) return;
        if (clicked == 0)
        {
            if (inSession) return;
            practice.WholePhase = true;
            if (Pickable(parts[0], out _)) Picker.Select(parts[0]);
            return;
        }
        practice.WholePhase = false;
        if (Pickable(parts[clicked - 1], out _)) Picker.Select(parts[clicked - 1]);
    }

    private void DrawDetailLine(bool inSession)
    {
        if (Selected is not { } scenario) return;
        var length = scenario.Duration is { } d ? FormatTime(d) : "length unknown";
        var best = BestStreakText(scenario);
        Muted($"{scenario.Phase.Zone.Name} · {length}{best}");

        var practice = Plugin.Practice;
        if (inSession)
        {
            practice.AutoRestart = false;
            practice.RunToEnd = false;
            practice.WholePhase = false;
        }
        Flow.SameLineRight(detailRightWidth);
        ImGui.BeginGroup();
        ImGui.BeginDisabled(inSession);
        bool single, loop, toEnd;
        using (!practice.AutoRestart && !practice.RunToEnd && !practice.WholePhase ? AsuTheme.PushAccentButton() : null)
            single = ImGui.SmallButton("Single");
        ImGui.SameLine(0f, 0f);
        using (practice.AutoRestart ? AsuTheme.PushAccentButton() : null)
            loop = ImGui.SmallButton("Loop");
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(inSession
                ? "Disabled in a multiplayer session."
                : "Restart the same scenario after every clean run. A failed run turns this back off.");
        ImGui.SameLine(0f, 0f);
        using (practice.RunToEnd ? AsuTheme.PushAccentButton() : null)
            toEnd = ImGui.SmallButton("Run to end");
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(inSession
                ? "Disabled in a multiplayer session."
                : $"Start plays all of {scenario.Phase.Zone.Name} from its first scenario, each one going straight on to the next. A death ends the run.");
        if (single)
        {
            practice.AutoRestart = false;
            practice.RunToEnd = false;
            practice.WholePhase = false;
        }
        if (loop) practice.AutoRestart = true;
        if (toEnd) practice.RunToEnd = true;
        ImGui.EndGroup();
        detailRightWidth = ImGui.GetItemRectSize().X;
    }

    private static string FormatTime(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";
}
