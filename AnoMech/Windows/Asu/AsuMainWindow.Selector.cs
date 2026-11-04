using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const float SelectorPopupMinPx = 300f;
    private const float SelectorListPx = 260f;

    private string searchQuery = "";
    private int highlighted;
    private bool focusSearch;

    private void DrawSelector(float width)
    {
        if (ImGui.Button($"{SelectorLabel()} ▾##selector", new Vector2(width, 0f)))
        {
            searchQuery = "";
            highlighted = 0;
            focusSearch = true;
            ImGui.OpenPopup("selector");
        }

        var scale = AsuTheme.Scale;
        ImGui.SetNextWindowSizeConstraints(new Vector2(System.Math.Max(width, SelectorPopupMinPx * scale), 0f), new Vector2(float.MaxValue, float.MaxValue));
        if (!ImGui.BeginPopup("selector")) return;

        if (focusSearch)
        {
            ImGui.SetKeyboardFocusHere();
            focusSearch = false;
        }
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputTextWithHint("##search", "Search every fight and scenario", ref searchQuery, 64))
            highlighted = 0;

        var (sections, flat) = SelectorEntries();
        if (flat.Count == 0)
        {
            Muted($"No scenario matches \"{searchQuery}\".");
            DrawRequestLink(FeedbackKind.Scenario);
        }
        else
        {
            highlighted = System.Math.Clamp(highlighted, 0, flat.Count - 1);
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) highlighted = (highlighted + 1) % flat.Count;
            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) highlighted = (highlighted - 1 + flat.Count) % flat.Count;
            if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
                PickFromSelector(flat[highlighted]);

            if (ImGui.BeginChild("##selector-list", new Vector2(0f, System.Math.Min(SelectorListPx * scale, ImGui.GetTextLineHeightWithSpacing() * (flat.Count + sections.Count) + ImGui.GetStyle().WindowPadding.Y))))
            {
                var index = 0;
                foreach (var (heading, scenarios) in sections)
                {
                    if (heading != null) Muted(heading);
                    foreach (var scenario in scenarios)
                    {
                        var pickable = Pickable(scenario, out var why);
                        ImGui.BeginDisabled(!pickable);
                        var isHighlighted = index == highlighted;
                        if (ImGui.Selectable($"{scenario.Phase.Zone.ShortName} › {scenario.DisplayName()}##sel{index}", isHighlighted || scenario == Selected))
                            PickFromSelector(scenario);
                        if (isHighlighted) ImGui.SetScrollHereY();
                        ImGui.EndDisabled();
                        if (why != null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(why);
                        index++;
                    }
                }
            }
            ImGui.EndChild();
        }
        Muted("↑↓ move · Enter selects · Start runs");
        if (ImGui.IsKeyPressed(ImGuiKey.Escape)) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private (List<(string? Heading, IReadOnlyList<IScenario> Scenarios)> Sections, List<IScenario> Flat) SelectorEntries()
    {
        var sections = new List<(string?, IReadOnlyList<IScenario>)>();
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            var recent = ScenarioSearch.Recent(Catalog.Scenarios, Plugin.Config.RecentScenarios);
            if (recent.Count > 0) sections.Add(("Recent", recent));
            sections.Add(("All", ScenarioSearch.Matches(Catalog.Scenarios, "")));
        }
        else
            sections.Add((null, ScenarioSearch.Matches(Catalog.Scenarios, searchQuery)));
        return (sections, sections.SelectMany(s => s.Item2).ToList());
    }

    // Enter only selects: a quick filter must never start the wrong scenario.
    private void PickFromSelector(IScenario scenario)
    {
        if (!Pickable(scenario, out _)) return;
        Picker.Select(scenario);
        RevealInScope(scenario);
        ImGui.CloseCurrentPopup();
    }
}
