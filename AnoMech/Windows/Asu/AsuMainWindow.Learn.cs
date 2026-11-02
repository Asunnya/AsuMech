using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const float ArenaPreviewPx = 120f;
    private const float ArenaMarginYalms = 4f;
    private const float WaymarkDotPx = 4f;

    private WatchSession? watch;

    private void TickWatch(Game game)
    {
        if (watch == null) return;
        watch.Tick(RunState.Live(game), ImGui.GetIO().DeltaTime);
        if (watch.Ended) watch = null;
    }

    private void EndWatch()
    {
        watch?.Dispose();
        watch = null;
    }

    private void DrawLearn(Game game, IScenario scenario, bool inSession)
    {
        DrawStratPill(scenario);
        if (!inSession)
        {
            ImGui.SameLine();
            DrawRolePill();
        }

        var why = WatchRefusal(scenario, inSession);
        ImGui.BeginDisabled(why != null);
        bool clicked;
        using (AsuTheme.PushAccentButton())
            clicked = ImGui.Button(watch != null ? "◉ Watching…##watch" : "◉ Watch##watch");
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(why ?? $"Bots play your seat at {WatchSession.WatchSpeed}× speed. Watch clears don't count toward streaks.");
        ImGui.SameLine();
        Muted($"Speed {WatchSession.WatchSpeed}×");
        if (clicked) StartWatch();

        DrawArenaPreview(scenario);
        ImGui.SameLine();
        ImGui.BeginGroup();
        if (LearnContent.For(scenario) is { } entry)
        {
            ImGui.TextWrapped(entry.Notes);
            foreach (var (label, url) in entry.Links)
                if (ImGui.SmallButton($"{label} ↗")) Util.OpenLink(url);
        }
        else
        {
            Muted("Notes and guide links: not written yet.");
            DrawRequestLink(FeedbackKind.Strat);
        }
        ImGui.EndGroup();
    }

    private string? WatchRefusal(IScenario scenario, bool inSession)
    {
        if (inSession) return "Watch is solo only: a session runs at 1× with every seat played.";
        if (watch != null) return "Already watching.";
        if (Picker.SelectedAi == null) return "Watch needs a strat for the bots to play.";
        return plugin.Runs.StartRefusal(Picker.Selection, SoloSelected);
    }

    private void StartWatch()
    {
        EndWatch();
        // A loop would restart after the watch hands control back.
        Plugin.Practice.AutoRestart = false;
        watch = new WatchSession(new GameWatchTarget());
        plugin.Runs.Start(Picker.Selection, SoloSelected);
    }

    // The arena and the selected waymark layout, north up.
    private void DrawArenaPreview(IScenario scenario)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = ArenaPreviewPx * scale;
        var pos = ImGui.GetCursorScreenPos();
        var center = pos + new Vector2(size * 0.5f);
        var draw = ImGui.GetWindowDrawList();
        var presets = scenario.Phase.Zone.WaymarkPresets;
        var markers = presets.Count > 0 ? presets[Math.Clamp(Picker.Waymark, 0, presets.Count - 1)].Markers : [];
        var extent = markers.Count > 0 ? markers.Max(m => new Vector2(m.Offset.X, m.Offset.Z).Length()) + ArenaMarginYalms : 20f;
        var yalm = size * 0.5f / extent;

        draw.AddCircleFilled(center, size * 0.5f, AsuTheme.U32(AsuTheme.Element));
        draw.AddCircle(center, size * 0.5f, AsuTheme.U32(AsuTheme.Line));
        draw.AddText(new Vector2(center.X - ImGui.CalcTextSize("N").X * 0.5f, pos.Y), AsuTheme.U32(AsuTheme.Muted), "N");
        foreach (var marker in markers)
        {
            var at = center + new Vector2(marker.Offset.X, marker.Offset.Z) * yalm;
            var color = AsuTheme.U32(WaymarkColor(marker.Slot));
            var label = WaymarkLabel(marker.Slot);
            if (marker.Slot <= WaymarkSlot.D) draw.AddCircleFilled(at, WaymarkDotPx * scale, color);
            else draw.AddRectFilled(at - new Vector2(WaymarkDotPx * scale), at + new Vector2(WaymarkDotPx * scale), color);
            draw.AddText(at + new Vector2(WaymarkDotPx * scale + 1f, -ImGui.GetTextLineHeight() * 0.5f), AsuTheme.U32(AsuTheme.Text), label);
        }
        ImGui.Dummy(new Vector2(size));
        if (ImGui.IsItemHovered() && presets.Count > 0)
            ImGui.SetTooltip($"Waymarks: {presets[Math.Clamp(Picker.Waymark, 0, presets.Count - 1)].Name}");
    }

    private static Vector4 WaymarkColor(WaymarkSlot slot) => slot switch
    {
        WaymarkSlot.A or WaymarkSlot.One => AsuTheme.WaymarkRed,
        WaymarkSlot.B or WaymarkSlot.Two => AsuTheme.WaymarkYellow,
        WaymarkSlot.C or WaymarkSlot.Three => AsuTheme.WaymarkBlue,
        _ => AsuTheme.WaymarkPurple,
    };

    private static string WaymarkLabel(WaymarkSlot slot) => slot switch
    {
        WaymarkSlot.A => "A", WaymarkSlot.B => "B", WaymarkSlot.C => "C", WaymarkSlot.D => "D",
        WaymarkSlot.One => "1", WaymarkSlot.Two => "2", WaymarkSlot.Three => "3", _ => "4",
    };
}
