using System;
using System.Collections.Generic;
using System.Linq;

namespace AsuMech.Windows.Asu;

// Group -1 means ungrouped.
internal readonly record struct TabMeasure(float Width, int Group = -1);

internal sealed record TabFitResult(IReadOnlyList<int> Visible, IReadOnlyList<int> Hidden, bool ShowGroupLabels)
{
    public bool Overflows => Hidden.Count > 0;
}

// Which tabs of a row fit, as indices into the row. Pure, so the folding rules stay testable
// without ImGui: group labels go before any tab does, the overflow button takes its width only
// when something is hidden, and a hidden selection replaces the last visible tabs.
internal static class TabFit
{
    public static TabFitResult Compute(
        IReadOnlyList<TabMeasure> tabs, Func<int, float> groupLabelWidth, float separatorWidth,
        float available, float overflowButtonWidth, int selected)
    {
        var all = Enumerable.Range(0, tabs.Count).ToList();
        if (Width(tabs, all, groupLabelWidth, separatorWidth, labels: true) <= available)
            return new(all, [], true);
        if (Width(tabs, all, groupLabelWidth, separatorWidth, labels: false) <= available)
            return new(all, [], false);

        var room = available - overflowButtonWidth;
        var visible = new List<int>();
        foreach (var i in all)
        {
            visible.Add(i);
            if (Width(tabs, visible, groupLabelWidth, separatorWidth, labels: false) > room)
            {
                visible.RemoveAt(visible.Count - 1);
                break;
            }
        }

        if (selected >= 0 && selected < tabs.Count && !visible.Contains(selected))
        {
            while (visible.Count > 0 && Width(tabs, [.. visible, selected], groupLabelWidth, separatorWidth, labels: false) > room)
                visible.RemoveAt(visible.Count - 1);
            visible.Add(selected);
        }
        return new(visible, all.Except(visible).ToList(), false);
    }

    private static float Width(IReadOnlyList<TabMeasure> tabs, IReadOnlyList<int> row, Func<int, float> groupLabelWidth,
        float separatorWidth, bool labels)
    {
        var width = 0f;
        int? lastGroup = null;
        foreach (var i in row)
        {
            var tab = tabs[i];
            if (tab.Group >= 0 && tab.Group != lastGroup)
            {
                if (lastGroup != null) width += separatorWidth;
                if (labels) width += groupLabelWidth(tab.Group);
                lastGroup = tab.Group;
            }
            width += tab.Width;
        }
        return width;
    }
}
