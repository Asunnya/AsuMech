using System;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Learn;
using AsuMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AsuMech.Windows.Learn;

// The areas about to resolve, drawn on the ground in world space while a lesson runs: the exact
// outline the hit test uses, red when the player's centre is inside it, with the action's name
// and the seconds left. The lesson's spot (GuidanceOverlay) draws after this and stays on top.
internal sealed class HazardOverlay : Window
{
    private const float LineThicknessPx = 2f;
    private const float InsideFill = 0.18f;
    private const float OutsideFill = 0.08f;
    private const int Segments = 48;

    public HazardOverlay() : base("##AsuMechHazards")
    {
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings
                | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus;
        RespectCloseHotkey = false;
        ShowCloseButton = false;
        DisableWindowSounds = true;
        IsOpen = true;
    }

    public static bool ShownAt(LearnLevel level) => level switch
    {
        LearnLevel.Assisted => Plugin.Config.ShowHazardsAssisted,
        LearnLevel.Guided => Plugin.Config.ShowHazardsGuided,
        LearnLevel.Observe => Plugin.Config.ShowHazardsObserve,
        _ => false,
    };

    public override void PreOpenCheck()
        => IsOpen = LearnSession.Current is { Ended: false } session && ShownAt(session.Level) && session.World.Hazards.Count > 0;

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
        var world = session.World;
        var now = world.Events.Elapsed;
        var player = world.Party.Player;
        var scale = ImGuiHelpers.GlobalScale;
        foreach (var hazard in world.Hazards)
        {
            var inside = player is not null && hazard.SignedDistance(player.Position) is <= 0f;
            var color = inside ? AsuTheme.Bad : AsuTheme.Accent;
            var line = AsuTheme.U32(color);
            var fill = AsuTheme.U32(color with { W = inside ? InsideFill : OutsideFill });
            Vector2? label = null;
            foreach (var loop in hazard.Outline(Segments))
            {
                var points = new Vector2[loop.Count];
                var visible = 0;
                for (var i = 0; i < loop.Count; i++)
                {
                    if (Plugin.GameGui.WorldToScreen(world.Coordinates.ToGlobal(loop[i]), out var p)) visible++;
                    points[i] = p;
                }
                if (visible < 2) continue;
                draw.AddConvexPolyFilled(ref points[0], points.Length, fill);
                draw.AddPolyline(ref points[0], points.Length, line, ImDrawFlags.Closed, LineThicknessPx * scale);
                label ??= points[0];
            }
            if (Plugin.GameGui.WorldToScreen(world.Coordinates.ToGlobal(hazard.Origin.Position), out var origin)) label = origin;
            if (label is { } at)
                draw.AddText(at + new Vector2(6f, -14f) * scale, line, $"{hazard.Name} {MathF.Max(0f, hazard.ResolveAt - now):0.0}s");
        }
    }
}
