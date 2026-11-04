using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const float StripButtonPx = 90f;
    private const float SegmentGapPx = 2f;
    private const float SegmentHeightPx = 4f;
    private const float DotRadiusPx = 4f;

    private DateTime runStartedAt;
    private TimeSpan runElapsed;
    private bool expandedDuringRun;
    private float stripRightWidth;
    private readonly HashSet<IScenario> clearedThisSession = [];

    private void OnRunStarted(RunScenarioParams p, bool local)
    {
        runStartedAt = DateTime.UtcNow;
        runElapsed = TimeSpan.Zero;
    }

    private void OnRunFinished(bool clean)
    {
        if (clean && Plugin.Game.LastRun is { } run) clearedThisSession.Add(run.Scenario);
    }

    private bool ShowRunStrip => RunState.Active && !expandedDuringRun;

    private void DrawRunStrip(Game game, bool inSession)
    {
        var run = game.LastRun;
        var scenario = run?.Scenario ?? Selected;
        if (RunState.Live(game)) runElapsed = DateTime.UtcNow - runStartedAt;
        var (label, color) = RunState.Status(game, scenario);
        var failed = run != null && RunState.IsFailed(run.Status);

        var pos = ImGui.GetCursorScreenPos();
        var lineHeight = ImGui.GetFrameHeight();
        var radius = DotRadiusPx * AsuTheme.Scale;
        ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(pos.X + radius, pos.Y + lineHeight * 0.5f), radius, AsuTheme.U32(color));
        ImGui.Dummy(new Vector2(radius * 2f, lineHeight));
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(scenario is null ? "AsuMech" : $"{scenario.Phase.Zone.ShortName} · {scenario.DisplayName()}");
        var status = $"{label} · {runElapsed:mm\\:ss} · {StreakText()}{(Plugin.Practice.AutoRestart ? " · loop" : "")}{(watch != null ? " · watching" : "")}{(Windows.Learn.LearnSession.Current is { Ended: false } learn ? $" · {learn.Level.ToString().ToLowerInvariant()}" : "")}";
        Flow.SameLineIfFits(Flow.TextWidth(status));
        Muted(status);

        var size = new Vector2(StripButtonPx * AsuTheme.Scale, 0f);
        Flow.SameLineRight(stripRightWidth);
        ImGui.BeginGroup();
        if (!inSession)
        {
            bool restart;
            using (failed ? AsuTheme.PushAccentButton() : null)
                restart = ImGui.Button("↻ Restart##strip-restart", size);
            if (restart)
            {
                EndWatch();
                plugin.Runs.Start(Picker.Selection, SoloSelected);
            }
            ImGui.SameLine();
        }
        bool stop;
        using (AsuTheme.PushTextColor(AsuTheme.Bad))
            stop = ImGui.Button("■ Stop##strip-stop", size);
        if (stop) { EndWatch(); plugin.Runs.Reset(); }
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("expand", FontAwesomeIcon.Expand)) expandedDuringRun = true;
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Show the full window");
        ImGui.EndGroup();
        stripRightWidth = ImGui.GetItemRectSize().X;

        if (scenario != null) DrawSegments(scenario);
        if (failed && run!.Issues.Count > 0)
            using (AsuTheme.PushTextColor(AsuTheme.Bad))
                ImGui.TextWrapped($"Mechanic failed: {run.Issues[^1].Description}. Streak reset.");

        DrawRunToggles(game, inSession);
        if (scenario != null) DrawScenarioSettings(scenario, inSession);
    }

    // One segment per scenario of the fight, as wide as its length.
    private void DrawSegments(IScenario current)
    {
        var scenarios = ScenariosOf(current.Phase.Zone).ToList();
        var scale = AsuTheme.Scale;
        var gap = SegmentGapPx * scale;
        var height = SegmentHeightPx * scale;
        var width = ImGui.GetContentRegionAvail().X;
        var total = scenarios.Sum(s => s.Duration ?? UnknownDurationSeconds);
        var usable = width - gap * (scenarios.Count - 1);
        var pos = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();
        var x = pos.X;
        foreach (var scenario in scenarios)
        {
            var w = usable * (scenario.Duration ?? UnknownDurationSeconds) / total;
            var color = scenario == current ? AsuTheme.Accent : clearedThisSession.Contains(scenario) ? AsuTheme.Cleared : AsuTheme.Element;
            draw.AddRectFilled(new Vector2(x, pos.Y), new Vector2(x + w, pos.Y + height), AsuTheme.U32(color));
            if (scenario == current && scenario.Duration is { } d && d > 0f)
            {
                var progress = Math.Clamp((float)runElapsed.TotalSeconds / d, 0f, 1f);
                draw.AddRectFilled(new Vector2(x, pos.Y + height), new Vector2(x + w * progress, pos.Y + height + scale), AsuTheme.U32(AsuTheme.Ok));
            }
            x += w + gap;
        }
        ImGui.Dummy(new Vector2(width, height + scale));
    }
}
