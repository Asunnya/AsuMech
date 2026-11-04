using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AnoMech.Windows.Asu;

internal static class Pills
{
    private const float KnobSlideSeconds = 0.12f;
    private const float KnobInsetPx = 2f;
    private static readonly Dictionary<uint, float> KnobPositions = new();

    // A sliding on/off switch. Returns true when clicked.
    public static bool Toggle(string label, bool on, bool disabled = false)
    {
        ImGui.BeginDisabled(disabled);
        var clicked = Switch(label, on);
        ImGui.EndDisabled();
        return clicked;
    }

    // Drop-in for ImGui.Checkbox: a sliding switch in the new layout, a plain checkbox in the classic one.
    public static bool Checkbox(string label, ref bool value)
    {
        if (!Plugin.Config.UseNewLayout) return ImGui.Checkbox(label, ref value);
        if (!Switch(label, value)) return false;
        value = !value;
        return true;
    }

    public static float Width(string label)
    {
        var shown = label.Split("##")[0];
        var track = ImGui.GetFrameHeight() * 1.75f;
        return track + (shown.Length > 0 ? ImGui.GetStyle().ItemInnerSpacing.X + ImGui.CalcTextSize(shown).X : 0);
    }

    // The label is part of the hit area, like a checkbox's.
    private static bool Switch(string label, bool on)
    {
        var shown = label.Split("##")[0];
        var height = ImGui.GetFrameHeight();
        var trackSize = new Vector2(height * 1.75f, height * 0.8f);
        var textSize = ImGui.CalcTextSize(shown);
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var pos = ImGui.GetCursorScreenPos();
        var id = ImGui.GetID(label);

        var width = Width(label);
        var clicked = ImGui.InvisibleButton(label, new Vector2(width, height));
        var hovered = ImGui.IsItemHovered();

        var target = on ? 1f : 0f;
        var t = KnobPositions.GetValueOrDefault(id, target);
        var step = ImGui.GetIO().DeltaTime / KnobSlideSeconds;
        t = t < target ? MathF.Min(target, t + step) : MathF.Max(target, t - step);
        KnobPositions[id] = t;

        var trackMin = pos + new Vector2(0, (height - trackSize.Y) / 2);
        var trackMax = trackMin + trackSize;
        var radius = trackSize.Y / 2;
        var track = Vector4.Lerp(hovered ? AsuTheme.ElementHover : AsuTheme.Line,
                                 hovered ? AsuTheme.AccentHover : AsuTheme.Accent, t);
        var knob = Vector4.Lerp(AsuTheme.Text, AsuTheme.OnAccent, t);
        var knobCenter = new Vector2(trackMin.X + radius + (trackSize.X - 2 * radius) * t, trackMin.Y + radius);

        var draw = ImGui.GetWindowDrawList();
        draw.AddRectFilled(trackMin, trackMax, ImGui.GetColorU32(track), radius);
        draw.AddCircleFilled(knobCenter, radius - KnobInsetPx * AsuTheme.Scale, ImGui.GetColorU32(knob));
        if (shown.Length > 0)
            draw.AddText(new Vector2(trackMax.X + spacing, pos.Y + (height - textSize.Y) / 2), ImGui.GetColorU32(ImGuiCol.Text), shown);

        return clicked;
    }
}
