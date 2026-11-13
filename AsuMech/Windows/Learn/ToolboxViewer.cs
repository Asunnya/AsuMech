using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.Game.Geometry;
using AsuMech.Learn;
using AsuMech.Learn.Toolbox;
using AsuMech.Learn.Tools;
using AsuMech.Network;
using AsuMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AsuMech.Windows.Learn;

// The lesson's toolbox plan, one step at a time, top-down and north-up. Follows the running lesson
// unless the player browses; browsing never touches the simulation.
internal sealed class ToolboxViewer : Window
{
    private const float DefaultSizePx = 340f;
    // Plans are calibrated by waymarks; the wall sits near 15 yalms. Enemies dragged past it are parked
    // off the arena by the author, and the toolbox's own "Main Boss" marker is unused when adds stand in.
    private const float ArenaYalms = 32f;
    private const float ArenaMarginYalms = 2f;
    private const float ParkedYalms = 16.5f;
    private const float PlayerIconPx = 24f;
    private const float EnemyIconPx = 28f;
    private const float OpacitySliderPx = 90f;
    private const float MinOpacity = 0.1f;
    private const float OtherRoleAlpha = 0.3f;
    private const float MinMapPx = 120f;
    private const float MaxMapLabelFraction = 0.4f;

    private ToolboxPlan? plan;
    private LessonToolbox? link;
    private LessonDirector? subscribed;
    private int shown;
    // The plan steps the lesson links, in plan order; a plan covering a whole fight shows only this
    // mechanic's part. Empty means the lesson links none, so every step is shown.
    private List<int> own = [];
    private bool follow = true;
    private PartyRole role;

    public ToolboxViewer() : base("Toolbox###AsuMechToolbox")
    {
        Size = new Vector2(DefaultSizePx, DefaultSizePx + 120f);
        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
    }

    public override void PreOpenCheck()
    {
        var session = LearnSession.Current;
        if (session is { Ended: false, Lesson.Toolbox: { } tb } && session.Level != LearnLevel.Exam)
        {
            if (!ReferenceEquals(subscribed, session.Director))
            {
                if (subscribed != null) subscribed.StepChanged -= OnStepChanged;
                subscribed = session.Director;
                subscribed.StepChanged += OnStepChanged;
                plan = ToolboxPlans.Load(tb.PlanId);
                link = tb;
                own = plan is null ? [] : tb.AllPages.Where(i => i >= 0 && i < plan.Steps.Count).Distinct().Order().ToList();
                role = session.Role;
                shown = own.Count > 0 ? own[0] : 0;
                follow = true;
            }
            IsOpen = plan != null;
        }
        else
        {
            if (subscribed != null) subscribed.StepChanged -= OnStepChanged;
            subscribed = null;
            IsOpen = false;
        }
    }

    private void OnStepChanged(ActiveStep step)
    {
        if (follow) Show(step);
    }

    private void JumpToCurrent()
    {
        if (subscribed?.Current is { } step) Show(step);
    }

    private void Show(ActiveStep step)
    {
        if (link is null || step.Step is null) return;
        var rolls = LearnSession.Current is { } session ? StateSnapshot.Of(session.Scenario) : [];
        if (link.PageFor(MasteryStore.StepId(step.Step.Key), rolls) is { } index) shown = index;
    }

    private IDisposable? theme;

    public override void PreDraw() => theme = AsuTheme.Push();

    public override void PostDraw()
    {
        theme?.Dispose();
        theme = null;
    }

    public override void Draw()
    {
        if (plan is null) return;
        var pages = own.Count > 0 ? own : Enumerable.Range(0, plan.Steps.Count).ToList();
        var page = Math.Max(0, pages.IndexOf(shown));
        shown = pages[page];
        var step = plan.Steps[shown];

        if (ImGuiComponents.IconButton("toolbox-prev", FontAwesomeIcon.ChevronLeft) && page > 0) { shown = pages[page - 1]; follow = false; }
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("toolbox-next", FontAwesomeIcon.ChevronRight) && page < pages.Count - 1) { shown = pages[page + 1]; follow = false; }
        ImGui.SameLine();
        ImGui.TextUnformatted($"{page + 1}/{pages.Count}  {plan.Title(step)}");
        if (LearnSession.Current?.Lesson is { } lesson)
        {
            ImGui.SameLine();
            LessonVoteButtons.Draw(lesson, $"#{shown + 1} {plan.Title(step)}", LessonFeedback.Toolbox);
        }
        if (!follow)
        {
            ImGui.SameLine();
            using (AsuTheme.PushAccentButton())
                if (ImGui.SmallButton("Follow the run")) { follow = true; JumpToCurrent(); }
        }
        var config = Plugin.Config;
        using (AsuTheme.PushPill())
            if (Pills.Toggle("Markers##waymarks", config.ToolboxShowWaymarks)) { config.ToolboxShowWaymarks = !config.ToolboxShowWaymarks; config.Save(); }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(OpacitySliderPx * ImGuiHelpers.GlobalScale);
        var opacity = config.ToolboxOpacity;
        if (ImGui.SliderFloat("Marker opacity", ref opacity, MinOpacity, 1f, "%.0f%%") || ImGui.IsItemDeactivatedAfterEdit())
        {
            config.ToolboxOpacity = Math.Clamp(opacity, MinOpacity, 1f);
            if (ImGui.IsItemDeactivatedAfterEdit()) config.Save();
        }

        // Labels that fit beside an object stay on the map; sentences go under it, where they wrap
        // instead of running past the arena's edge.
        var avail = ImGui.GetContentRegionAvail();
        var texts = step.Objects.Where(o => o.Kind == "text" && o.Label.Trim().Length > 0 && !plan.Footer.Contains(o.Label.Trim())).ToList();
        var captions = texts.Where(o => !FitsOnMap(o.Label, avail.X)).Select(o => o.Label.Trim()).ToList();
        var note = step.Note.Length > 0 ? step.Note.Split('\n')[0] : null;
        var below = captions.Concat(note is null ? Array.Empty<string>() : [note])
            .Sum(t => ImGui.CalcTextSize(t, false, avail.X).Y + ImGui.GetStyle().ItemSpacing.Y);
        var size = Math.Max(MinMapPx * ImGuiHelpers.GlobalScale, Math.Min(avail.X, avail.Y - below));
        var origin = ImGui.GetCursorScreenPos();
        var centre = origin + new Vector2(size * 0.5f);
        var draw = ImGui.GetWindowDrawList();
        draw.PushClipRect(origin, origin + new Vector2(size), true);
        // The run's own fence while one is up, the zone's floor before that; the view spans the
        // floor plus a margin for objects authored past its edge.
        var arena = LearnSession.Current is { Ended: false } live ? live.World.ArenaShape ?? live.Scenario.Phase.Zone.Arena : null;
        var px = size / ArenaYalms;
        switch (arena)
        {
            case SquareArena square:
                px = size / (square.HalfWidth * 2f + ArenaMarginYalms * 2f);
                var half = new Vector2(square.HalfWidth * px);
                draw.AddRectFilled(centre - half, centre + half, AsuTheme.U32(AsuTheme.Element));
                draw.AddRect(centre - half, centre + half, AsuTheme.U32(AsuTheme.Line));
                break;
            case CircleArena circle:
                px = size / (circle.Radius * 2f + ArenaMarginYalms * 2f);
                draw.AddCircleFilled(centre, circle.Radius * px, AsuTheme.U32(AsuTheme.Element));
                draw.AddCircle(centre, circle.Radius * px, AsuTheme.U32(AsuTheme.Line));
                break;
            default:
                draw.AddCircleFilled(centre, size * 0.5f, AsuTheme.U32(AsuTheme.Element));
                draw.AddCircle(centre, size * 0.5f, AsuTheme.U32(AsuTheme.Line));
                break;
        }
        draw.AddText(new Vector2(centre.X - 4f, origin.Y), AsuTheme.U32(AsuTheme.Muted), "N");

        Vector2 At(ToolboxObject o) => centre + new Vector2(o.X, o.Z) * px;
        foreach (var o in step.Objects.Where(o => o.Kind == "zone")) DrawZone(draw, o, At(o), px);
        foreach (var o in step.Objects.Where(o => o.Kind == "tether")) DrawTether(draw, o, At(o), px);
        if (config.ToolboxShowWaymarks)
            foreach (var o in step.Objects.Where(o => o.Kind == "waymark")) DrawWaymark(draw, o, At(o), config.ToolboxOpacity);
        var hasAdds = step.Objects.Any(o => o.Kind == "add");
        foreach (var o in step.Objects.Where(o => o.Kind is "boss" or "add"))
        {
            if (new Vector2(o.X, o.Z).Length() > ParkedYalms) continue;
            if (o.Kind == "boss" && hasAdds && o.Label == "Main Boss") continue;
            DrawEnemy(draw, o, At(o), px);
        }
        foreach (var o in step.Objects.Where(o => o.Kind == "player")) DrawPlayer(draw, o, At(o));
        foreach (var o in texts.Where(o => FitsOnMap(o.Label, avail.X)))
        {
            var label = o.Label.Trim();
            var w = ImGui.CalcTextSize(label).X;
            var at = At(o) - new Vector2(w * 0.5f, ImGui.GetTextLineHeight() * 0.5f);
            at.X = Math.Clamp(at.X, origin.X, origin.X + size - w);
            draw.AddText(at, AsuTheme.U32(AsuTheme.Text), label);
        }
        draw.PopClipRect();
        ImGui.Dummy(new Vector2(size));

        using (AsuTheme.PushTextColor(AsuTheme.Muted))
            foreach (var text in note is null ? captions : [.. captions, note])
                ImGui.TextWrapped(text);
    }

    private static bool FitsOnMap(string label, float width)
        => !label.Contains('\n') && ImGui.CalcTextSize(label.Trim()).X <= width * MaxMapLabelFraction;

    private static void DrawEnemy(ImDrawListPtr draw, ToolboxObject o, Vector2 at, float px)
    {
        var hitbox = Math.Max(4f, o.Width * 0.5f * px);
        draw.AddCircle(at, hitbox, AsuTheme.U32(AsuTheme.Bad with { W = 0.6f }), 32, 1.5f);
        var r = EnemyIconPx * 0.5f * ImGuiHelpers.GlobalScale;
        if (JobIcons.Enemy(o.Kind == "boss") is { } icon)
            draw.AddImage(icon.Handle, at - new Vector2(r), at + new Vector2(r));
        else
            draw.AddCircleFilled(at, r * 0.5f, AsuTheme.U32(AsuTheme.Bad));
        draw.AddText(at + new Vector2(-ImGui.CalcTextSize(o.Label).X * 0.5f, r + 2f), AsuTheme.U32(AsuTheme.Muted), o.Label);
    }

    private void DrawPlayer(ImDrawListPtr draw, ToolboxObject o, Vector2 at)
    {
        var mine = link != null && link.RoleMap.TryGetValue(role.ToString(), out var slot) && slot == o.Key;
        var alpha = mine ? 1f : OtherRoleAlpha;
        var r = PlayerIconPx * 0.5f * ImGuiHelpers.GlobalScale;
        if (JobIcons.Get(o.Job) is { } icon)
            draw.AddImage(icon.Handle, at - new Vector2(r), at + new Vector2(r), Vector2.Zero, Vector2.One, AsuTheme.U32(Vector4.One with { W = alpha }));
        else
        {
            draw.AddCircleFilled(at, r, AsuTheme.U32((mine ? AsuTheme.Accent : AsuTheme.Text) with { W = alpha }));
            if (mine)
                draw.AddText(at + new Vector2(r + 2f, -ImGui.GetTextLineHeight() * 0.5f), AsuTheme.U32(AsuTheme.Text), o.Job is { Length: > 0 } ? o.Job : o.Key);
        }
        if (mine) draw.AddCircle(at, r + 2f, AsuTheme.U32(AsuTheme.Accent), 24, 2f);
        if (o.Buffs is { Count: > 0 })
            draw.AddCircle(at, r + 5f, AsuTheme.U32(AsuTheme.WaymarkYellow with { W = alpha }), 24, 1.5f);
    }

    private static void DrawZone(ImDrawListPtr draw, ToolboxObject o, Vector2 at, float px)
    {
        var fill = AsuTheme.U32(ParseColor(o.Color, AsuTheme.Bad) with { W = 0.22f });
        var edge = AsuTheme.U32(ParseColor(o.Color, AsuTheme.Bad) with { W = 0.7f });
        if (o.Shape == "circle")
        {
            var r = o.Width * 0.5f * px;
            draw.AddCircleFilled(at, r, fill, 48);
            draw.AddCircle(at, r, edge, 48, 1.5f);
            return;
        }
        var half = new Vector2(o.Width, o.Height) * 0.5f * px;
        var rot = o.Rotation * MathF.PI / 180f;
        if (o.Shape == "donut")
        {
            // raidplan.io's donut: inner radius is half the outer.
            draw.AddCircle(at, half.X, edge, 48, half.X * 0.5f);
            draw.AddCircle(at, half.X, fill, 48, half.X * 0.5f);
            return;
        }
        if (o.Shape == "cone")
        {
            // A wedge whose apex sits at the top of its unrotated box and whose base is the bottom edge.
            Vector2 P(float sx, float sy)
            {
                var p = new Vector2(half.X * sx, half.Y * sy);
                return at + new Vector2(p.X * MathF.Cos(rot) - p.Y * MathF.Sin(rot), p.X * MathF.Sin(rot) + p.Y * MathF.Cos(rot));
            }
            draw.AddTriangleFilled(P(0, -1), P(1, 1), P(-1, 1), fill);
            draw.AddTriangle(P(0, -1), P(1, 1), P(-1, 1), edge, 1.5f);
            return;
        }
        Vector2 Corner(float sx, float sy)
        {
            var p = new Vector2(half.X * sx, half.Y * sy);
            return at + new Vector2(p.X * MathF.Cos(rot) - p.Y * MathF.Sin(rot), p.X * MathF.Sin(rot) + p.Y * MathF.Cos(rot));
        }
        draw.AddQuadFilled(Corner(-1, -1), Corner(1, -1), Corner(1, 1), Corner(-1, 1), fill);
        draw.AddQuad(Corner(-1, -1), Corner(1, -1), Corner(1, 1), Corner(-1, 1), edge, 1.5f);
    }

    // A toolbox tether is a thin rotated rectangle; its long axis is the line.
    private static void DrawTether(ImDrawListPtr draw, ToolboxObject o, Vector2 at, float px)
    {
        var rot = o.Rotation * MathF.PI / 180f;
        var axis = new Vector2(-MathF.Sin(rot), MathF.Cos(rot)) * o.Height * 0.5f * px;
        draw.AddLine(at - axis, at + axis, AsuTheme.U32(ParseColor(o.Color, AsuTheme.Bad)), 2f);
    }

    private static void DrawWaymark(ImDrawListPtr draw, ToolboxObject o, Vector2 at, float opacity)
    {
        var label = o.Label.Replace("Waymark ", "");
        var color = AsuTheme.U32((label switch
        {
            "A" or "1" => AsuTheme.WaymarkRed, "B" or "2" => AsuTheme.WaymarkYellow,
            "C" or "3" => AsuTheme.WaymarkBlue, _ => AsuTheme.WaymarkPurple,
        }) with { W = opacity });
        if (o.Shape == "circle") draw.AddCircle(at, 5f, color, 16, 1.5f);
        else draw.AddRect(at - new Vector2(5f), at + new Vector2(5f), color, 0f, ImDrawFlags.None, 1.5f);
        draw.AddText(at + new Vector2(6f, -7f), color, label);
    }

    private static Vector4 ParseColor(string? hex, Vector4 fallback)
    {
        if (hex is not { Length: 7 } || hex[0] != '#') return fallback;
        try
        {
            return new Vector4(Convert.ToInt32(hex[1..3], 16) / 255f, Convert.ToInt32(hex[3..5], 16) / 255f, Convert.ToInt32(hex[5..7], 16) / 255f, 1f);
        }
        catch (FormatException) { return fallback; }
    }
}
