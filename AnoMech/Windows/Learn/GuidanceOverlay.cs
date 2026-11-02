using System;
using System.Numerics;
using AnoMech.Learn;
using AnoMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Learn;

// Draws the lesson over the game: the target ring and the arrow to it in world space, the step's
// text at the top of the screen, and the result of the last step. Its own look, so nothing here
// reads as a telegraph.
internal sealed class GuidanceOverlay : Window
{
    private const float RingRadiusPx = 14f;
    private const float RingThicknessPx = 3f;
    private const float ArrowThicknessPx = 3f;
    private const float ArrowHeadPx = 10f;
    private const float PanelWidthPx = 440f;
    private const float PanelTopPx = 90f;
    private static readonly TimeSpan ResultDuration = TimeSpan.FromSeconds(2.5);

    private (string Text, Vector4 Color, DateTime Until)? result;
    private LessonDirector? subscribed;

    public GuidanceOverlay() : base("##AsuMechGuidance")
    {
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings
                | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus;
        RespectCloseHotkey = false;
        ShowCloseButton = false;
        DisableWindowSounds = true;
        IsOpen = true;
    }

    public override void PreOpenCheck()
    {
        var session = LearnSession.Current;
        session?.Tick(ImGui.GetIO().DeltaTime);
        if (session is { Ended: false } && session.Level != LearnLevel.Exam)
        {
            Subscribe(session.Director);
            IsOpen = true;
        }
        else
        {
            Subscribe(null);
            IsOpen = false;
        }
    }

    private void Subscribe(LessonDirector? director)
    {
        if (ReferenceEquals(subscribed, director)) return;
        if (subscribed != null) subscribed.StepResolved -= OnResolved;
        subscribed = director;
        if (director != null) director.StepResolved += OnResolved;
    }

    private void OnResolved(ActiveStep step, StepResult r)
    {
        var (text, color) = r switch
        {
            StepResult.OnTime => ("In place", AsuTheme.Ok),
            StepResult.Late => ("Late", AsuTheme.Accent),
            StepResult.WrongPlace => ("Wrong place", AsuTheme.Bad),
            _ => ("Mechanic failed", AsuTheme.Bad),
        };
        result = ($"{step.Title}: {text}", color, DateTime.UtcNow + ResultDuration);
    }

    public override void PreDraw()
    {
        Position = Vector2.Zero;
        Size = ImGui.GetIO().DisplaySize;
        PositionCondition = SizeCondition = ImGuiCond.Always;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw() => ImGui.PopStyleVar();

    public override void Draw()
    {
        if (LearnSession.Current is not { Ended: false } session) return;
        var draw = ImGui.GetWindowDrawList();
        if (session.Director.Current is { } step && step.HintVisible(session.World.Events.Elapsed)) DrawStep(draw, session, step);
        DrawPanel(draw, session);
    }

    private static void DrawStep(ImDrawListPtr draw, LearnSession session, ActiveStep step)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var eval = step.Evaluation;
        var color = eval.Result switch
        {
            StepResult.OnTime => AsuTheme.Ok,
            StepResult.Late => AsuTheme.Accent,
            StepResult.WrongPlace or StepResult.MechanicFailed => AsuTheme.Bad,
            _ => eval.Within ? AsuTheme.Ok : AsuTheme.Accent,
        };
        var u32 = AsuTheme.U32(color);
        var targetWorld = session.World.Coordinates.ToGlobal(eval.Target);
        if (!Plugin.GameGui.WorldToScreen(targetWorld, out var target)) return;

        // The tolerance ring, in world units projected along X so it sits on the ground.
        var edgeWorld = targetWorld + new Vector3(eval.Tolerance, 0f, 0f);
        var ringPx = Plugin.GameGui.WorldToScreen(edgeWorld, out var edge) ? Math.Max(RingRadiusPx * scale, Vector2.Distance(target, edge)) : RingRadiusPx * scale;
        draw.AddCircle(target, ringPx, u32, 48, RingThicknessPx * scale);
        draw.AddCircleFilled(target, RingThicknessPx * scale, u32);

        if (eval.Result == null && eval.Deadline > 0f)
        {
            var now = session.World.Events.Elapsed;
            var span = eval.Deadline - step.Order.IssuedAt;
            var left = span > 0f ? Math.Clamp((eval.Deadline - now) / span, 0f, 1f) : 0f;
            draw.PathArcTo(target, ringPx + RingThicknessPx * 1.5f * scale, -MathF.PI / 2f, -MathF.PI / 2f + MathF.Tau * left, 48);
            draw.PathStroke(AsuTheme.U32(AsuTheme.Text), ImDrawFlags.None, RingThicknessPx * 0.6f * scale);
        }

        if (eval.Within || session.World.Party.Player is not { } player) return;
        var playerWorld = session.World.Coordinates.ToGlobal(player.Position);
        if (!Plugin.GameGui.WorldToScreen(playerWorld, out var feet)) return;
        var dir = target - feet;
        var len = dir.Length();
        if (len < ringPx * 2f) return;
        dir /= len;
        var end = target - dir * (ringPx + ArrowHeadPx * scale);
        draw.AddLine(feet, end, u32, ArrowThicknessPx * scale);
        var normal = new Vector2(-dir.Y, dir.X);
        var tip = target - dir * ringPx;
        draw.AddTriangleFilled(tip, end + normal * ArrowHeadPx * 0.6f * scale, end - normal * ArrowHeadPx * 0.6f * scale, u32);
    }

    private void DrawPanel(ImDrawListPtr draw, LearnSession session)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var width = PanelWidthPx * scale;
        var padding = new Vector2(14f, 10f) * scale;
        var x = (ImGui.GetIO().DisplaySize.X - width) * 0.5f;
        var y = PanelTopPx * scale;
        var step = session.Director.Current;
        var lines = new System.Collections.Generic.List<(string Text, Vector4 Color)>();
        if (step != null)
        {
            lines.Add((step.Title, AsuTheme.Text));
            if (step.Cue is { } cue) lines.Add(($"Watch for: {cue}", AsuTheme.Muted));
            if (step.HintVisible(session.World.Events.Elapsed)) { if (step.Rule is { } rule) lines.Add((rule, AsuTheme.Text)); }
            else lines.Add((step.ShowAt == float.MaxValue ? "Mastered: no hint." : "Hint coming…", AsuTheme.Muted));
        }
        else
            lines.Add((session.Lesson is null ? "No lesson for this strat: watching the strat play." : "Waiting for the next step…", AsuTheme.Muted));
        if (result is { } r && DateTime.UtcNow < r.Until) lines.Add((r.Text, r.Color));

        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
        var height = padding.Y * 2 + lineHeight * lines.Count;
        draw.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height), AsuTheme.U32(AsuTheme.Window with { W = 0.92f }), 8f * scale);
        draw.AddRect(new Vector2(x, y), new Vector2(x + width, y + height), AsuTheme.U32(AsuTheme.Line), 8f * scale);
        var cursor = new Vector2(x, y) + padding;
        foreach (var (text, color) in lines)
        {
            draw.AddText(cursor, AsuTheme.U32(color), text);
            cursor.Y += lineHeight;
        }
    }
}
