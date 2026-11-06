using System;
using System.Linq;
using System.Numerics;
using AnoMech.Learn;
using AnoMech.Learn.Mitigation;
using AnoMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Learn;

// The next casts of the running scenario, top right, with the seconds to each and the lesson's
// steps marked among them. Read-only: it shows the embedded CastTimeline, which the sim follows.
internal sealed class TimelinePanel : Window
{
    private const float WidthPx = 300f;
    private const float TopMarginPx = 90f;
    private const float RightMarginPx = 20f;

    private string? loadedFor;
    private CastTimeline? timeline;

    public TimelinePanel() : base("##AsuMechTimeline")
    {
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.AlwaysAutoResize;
        RespectCloseHotkey = false;
        ShowCloseButton = false;
        DisableWindowSounds = true;
    }

    public override void PreOpenCheck()
    {
        var session = LearnSession.Current;
        if (session is not { Ended: false } || session.Level == LearnLevel.Exam || !Plugin.Config.ShowTimelinePanel)
        {
            IsOpen = false;
            return;
        }
        var name = session.Scenario.GetType().Name;
        if (loadedFor != name)
        {
            loadedFor = name;
            timeline = MitTimelines.Load(name);
        }
        IsOpen = timeline != null;
    }

    public override void PreDraw()
    {
        var display = ImGui.GetIO().DisplaySize;
        var scale = ImGuiHelpers.GlobalScale;
        Position = new Vector2(display.X - (WidthPx + RightMarginPx) * scale, TopMarginPx * scale);
        PositionCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        if (LearnSession.Current is not { Ended: false } session) return;
        var now = session.World.Events.Elapsed;
        var items = UpcomingCasts.Next(timeline, session.Lesson, now);
        var draw = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var width = WidthPx * scale;
        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
        var padding = new Vector2(12f, 8f) * scale;
        var pos = ImGui.GetCursorScreenPos();
        var height = padding.Y * 2 + lineHeight * (Math.Max(1, items.Count) + 1);
        draw.AddRectFilled(pos, pos + new Vector2(width, height), AsuTheme.U32(AsuTheme.Window with { W = 0.9f }), 8f * scale);
        draw.AddRect(pos, pos + new Vector2(width, height), AsuTheme.U32(AsuTheme.Line), 8f * scale);
        var cursor = pos + padding;
        draw.AddText(cursor, AsuTheme.U32(AsuTheme.Muted), $"Timeline · {now:0.0}s");
        cursor.Y += lineHeight;
        if (items.Count == 0) draw.AddText(cursor, AsuTheme.U32(AsuTheme.Muted), "Nothing more on the timeline.");
        foreach (var item in items)
        {
            var left = item.At - now;
            var running = item.Running(now);
            var color = item.IsStep ? AsuTheme.Accent : running ? AsuTheme.Text : left <= 3f ? AsuTheme.Text : AsuTheme.Muted;
            var when = running ? "now" : $"{MathF.Max(0f, left):0.0}s";
            draw.AddText(cursor, AsuTheme.U32(color), $"{when,6}  {(item.IsStep ? "▸ " : "")}{item.Name}");
            if (running && item.Resolve is { } resolve && resolve > item.At)
            {
                var frac = Math.Clamp((now - item.At) / (resolve - item.At), 0f, 1f);
                var barY = cursor.Y + lineHeight - 3f * scale;
                draw.AddRectFilled(new Vector2(cursor.X, barY), new Vector2(cursor.X + (width - padding.X * 2) * frac, barY + 2f * scale), AsuTheme.U32(AsuTheme.Accent));
            }
            cursor.Y += lineHeight;
        }
        ImGui.Dummy(new Vector2(width, height));
    }
}
