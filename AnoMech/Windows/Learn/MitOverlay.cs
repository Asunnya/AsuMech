using System;
using System.Linq;
using System.Numerics;
using AnoMech.Learn.Mitigation;
using AnoMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Learn;

// The next mitigation items of the player's column, with a countdown to each hit, and the last
// verdicts. Shown while a session runs with Reminder/Strict assist or a Guided/Assisted lesson.
internal sealed class MitOverlay : Window
{
    private const float WidthPx = 360f;
    private const float BottomMarginPx = 180f;
    private const int Upcoming = 3;

    public MitOverlay() : base("##AsuMechMitStrip")
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
        var training = MitTraining.Instance;
        training?.Tick();
        var assist = LearnSession.Current is { Ended: false } learn
            ? learn.Level is AnoMech.Learn.LearnLevel.Observe or AnoMech.Learn.LearnLevel.Exam ? MitAssist.None : MitAssist.Reminder
            : (MitAssist)Plugin.Config.MitPracticeAssist;
        IsOpen = training?.Session != null && assist != MitAssist.None;
    }

    public override void PreDraw()
    {
        var display = ImGui.GetIO().DisplaySize;
        var scale = ImGuiHelpers.GlobalScale;
        Position = new Vector2((display.X - WidthPx * scale) * 0.5f, display.Y - BottomMarginPx * scale);
        PositionCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        if (MitTraining.Instance is not { Session: { } session } training) return;
        var now = Plugin.Game.World.Events.Elapsed;
        var draw = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var width = WidthPx * scale;
        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
        var items = session.Upcoming(now).Take(Upcoming).ToList();
        var feedback = training.Feedback.Where(f => DateTime.UtcNow < f.Until).ToList();
        training.Feedback.RemoveAll(f => DateTime.UtcNow >= f.Until);
        var rows = Math.Max(1, items.Count) + feedback.Count + 1;
        var pos = ImGui.GetCursorScreenPos();
        var padding = new Vector2(12f, 8f) * scale;
        var height = padding.Y * 2 + lineHeight * rows;
        draw.AddRectFilled(pos, pos + new Vector2(width, height), AsuTheme.U32(AsuTheme.Window with { W = 0.9f }), 8f * scale);
        draw.AddRect(pos, pos + new Vector2(width, height), AsuTheme.U32(AsuTheme.Line), 8f * scale);
        var cursor = pos + padding;
        draw.AddText(cursor, AsuTheme.U32(AsuTheme.Muted), $"Mit · {training.Column} ({training.Job}) · {training.Plan?.Plan}");
        cursor.Y += lineHeight;
        if (items.Count == 0) draw.AddText(cursor, AsuTheme.U32(AsuTheme.Muted), "No more items in this phase.");
        foreach (var (item, i) in items.Select((it, i) => (it, i)))
        {
            var names = item.Item.Reminder ?? (item.Item.ExpectStatus is { } st ? $"({st} active)" : string.Join(" / ", item.AcceptIds.Select(id => MitCatalog.Find(id)?.Name ?? id.ToString())));
            if (item.Item.Forbid is { Count: > 0 } f) names = $"don't: {string.Join("/", f)}";
            var left = item.Closes - now;
            var color = i == 0 ? (left <= 0f ? AsuTheme.Bad : now >= item.Opens ? AsuTheme.Ok : AsuTheme.Text) : AsuTheme.Muted;
            var target = item.Target is { } t and not ("self" or "party") ? $" → {t}" : "";
            var text = $"{item.Label}: {names}{target}  {(left >= 0 ? $"{left:0.0}s" : "now")}";
            draw.AddText(cursor, AsuTheme.U32(color), text);
            if (i == 0 && item.Closes > item.Opens)
            {
                var frac = Math.Clamp((now - item.Opens) / (item.Closes - item.Opens), 0f, 1f);
                var barY = cursor.Y + lineHeight - 3f * scale;
                draw.AddRectFilled(new Vector2(cursor.X, barY), new Vector2(cursor.X + (width - padding.X * 2) * frac, barY + 2f * scale), AsuTheme.U32(now >= item.Opens ? AsuTheme.Ok : AsuTheme.Line));
            }
            cursor.Y += lineHeight;
        }
        foreach (var (text, color, _) in feedback)
        {
            draw.AddText(cursor, AsuTheme.U32(color), text);
            cursor.Y += lineHeight;
        }
        ImGui.Dummy(new Vector2(width, height));
    }
}
