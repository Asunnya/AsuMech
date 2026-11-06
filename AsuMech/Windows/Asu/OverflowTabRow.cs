using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;

namespace AsuMech.Windows.Asu;

// Group -1 means ungrouped; GroupLabels/GroupNames are indexed by Group.
internal sealed record TabRowItem(string Label, int Group = -1, string? Tooltip = null);

// A row of text tabs that folds what doesn't fit into a "☰ N" popup on its left.
internal static class OverflowTabRow
{
    private const float SeparatorPx = 14f;
    private const float OverflowButtonPx = 52f;

    // Returns the clicked index, or -1.
    public static int Draw(string id, IReadOnlyList<TabRowItem> items, int selected,
        IReadOnlyList<string>? groupLabels = null, IReadOnlyList<string>? groupNames = null)
    {
        if (items.Count == 0) return -1;
        var scale = AsuTheme.Scale;
        var padding = AsuTheme.TabPadding;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var measures = items.Select(it => new TabMeasure(ImGui.CalcTextSize(it.Label).X + padding.X * 2 + spacing, it.Group)).ToList();
        float LabelWidth(int g) => groupLabels is null || g >= groupLabels.Count ? 0f : ImGui.CalcTextSize(groupLabels[g]).X + spacing;
        var fit = TabFit.Compute(measures, LabelWidth, SeparatorPx * scale, ImGui.GetContentRegionAvail().X,
            OverflowButtonPx * scale, selected);

        var clicked = -1;
        ImGui.PushID(id);
        // Every piece is placed at an explicit x on one row top: SameLine after a moved cursor
        // drifts the rest of the row vertically.
        var origin = ImGui.GetCursorScreenPos();
        var rowHeight = ImGui.GetTextLineHeight() + padding.Y * 2;
        var x = origin.X;
        if (fit.Overflows)
        {
            var buttonHeight = ImGui.GetFrameHeight();
            ImGui.SetCursorScreenPos(new Vector2(x, origin.Y + (rowHeight - buttonHeight) * 0.5f));
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Bars, fit.Hidden.Count.ToString()))
                ImGui.OpenPopup("overflow");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"{fit.Hidden.Count} more");
            x = ImGui.GetItemRectMax().X + spacing;
            clicked = DrawPopup(items, selected, fit, groupNames);
        }

        var draw = ImGui.GetWindowDrawList();
        int? lastGroup = null;
        foreach (var i in fit.Visible)
        {
            var item = items[i];
            if (item.Group >= 0 && item.Group != lastGroup)
            {
                if (lastGroup != null)
                {
                    var sx = x + SeparatorPx * scale * 0.5f - spacing * 0.5f;
                    draw.AddLine(new Vector2(sx, origin.Y + rowHeight * 0.25f), new Vector2(sx, origin.Y + rowHeight * 0.75f),
                        AsuTheme.U32(AsuTheme.Line));
                    x += SeparatorPx * scale;
                }
                if (fit.ShowGroupLabels && groupLabels is not null && item.Group < groupLabels.Count)
                {
                    draw.AddText(new Vector2(x, origin.Y + padding.Y), AsuTheme.U32(AsuTheme.Muted), groupLabels[item.Group]);
                    x += LabelWidth(item.Group);
                }
                lastGroup = item.Group;
            }
            ImGui.SetCursorScreenPos(new Vector2(x, origin.Y));
            if (DrawTab(i, item, i == selected, padding)) clicked = i;
            x = ImGui.GetItemRectMax().X + spacing;
        }
        ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y));
        ImGui.Dummy(new Vector2(System.Math.Max(0f, x - origin.X - spacing), rowHeight));
        ImGui.PopID();
        return clicked;
    }

    private static bool DrawTab(int index, TabRowItem item, bool selected, Vector2 padding)
    {
        var textSize = ImGui.CalcTextSize(item.Label);
        var size = textSize + padding * 2;
        var pos = ImGui.GetCursorScreenPos();
        var pressed = ImGui.InvisibleButton($"tab{index}", size);
        var hovered = ImGui.IsItemHovered();
        var draw = ImGui.GetWindowDrawList();
        var color = selected || hovered ? AsuTheme.Text : AsuTheme.Muted;
        draw.AddText(pos + padding, AsuTheme.U32(color), item.Label);
        if (selected)
            draw.AddRectFilled(pos with { Y = pos.Y + size.Y - AsuTheme.Underline }, pos + size, AsuTheme.U32(AsuTheme.Accent));
        if (hovered && item.Tooltip is { } tip) ImGui.SetTooltip(tip);
        return pressed;
    }

    private static int DrawPopup(IReadOnlyList<TabRowItem> items, int selected, TabFitResult fit, IReadOnlyList<string>? groupNames)
    {
        var clicked = -1;
        if (!ImGui.BeginPopup("overflow")) return clicked;
        int? lastGroup = null;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.Group >= 0 && item.Group != lastGroup && groupNames is not null && item.Group < groupNames.Count)
            {
                using (AsuTheme.PushTextColor(AsuTheme.Muted))
                    ImGui.TextUnformatted(groupNames[item.Group]);
                lastGroup = item.Group;
            }
            if (ImGui.Selectable($"{item.Label}##pop{i}", i == selected)) clicked = i;
            if (fit.Visible.Contains(i))
            {
                ImGui.SameLine();
                using (AsuTheme.PushTextColor(AsuTheme.Muted))
                    ImGui.TextUnformatted("in row");
            }
        }
        ImGui.EndPopup();
        return clicked;
    }
}
