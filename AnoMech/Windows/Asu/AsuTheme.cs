using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AnoMech.Windows.Asu;

// The only place the new layout names a color or a size.
internal static class AsuTheme
{
    public static readonly Vector4 Window = Rgb(0x17181b);
    public static readonly Vector4 Element = Rgb(0x202227);
    public static readonly Vector4 ElementHover = Rgb(0x292c32);
    public static readonly Vector4 Line = Rgb(0x2a2d33);
    public static readonly Vector4 Text = Rgb(0xe8e6e3);
    public static readonly Vector4 Muted = Rgb(0x8d8a85);
    public static readonly Vector4 Accent = Rgb(0xe8835f);
    public static readonly Vector4 AccentHover = Rgb(0xf09574);
    public static readonly Vector4 OnAccent = Rgb(0x1f120d);
    public static readonly Vector4 Ok = Rgb(0x79c99a);
    public static readonly Vector4 Bad = Rgb(0xff7b72);
    public static readonly Vector4 Cleared = Rgb(0x3c5a48);
    public static readonly Vector4 ToastBackground = Rgb(0x173326);
    public static readonly Vector4 ToastText = Rgb(0xbdf0d2);
    public static readonly Vector4 ArenaMarker = Rgb(0x55585f);

    private const float WindowRoundingPx = 12f;
    private const float FrameRoundingPx = 7f;
    private const float PopupRoundingPx = 8f;
    private const float PillRoundingPx = 12f;
    private const float RowGapPx = 8f;
    private const float UnderlinePx = 2f;
    private static readonly Vector2 WindowPaddingPx = new(14f, 12f);
    private static readonly Vector2 FramePaddingPx = new(11f, 6f);
    private static readonly Vector2 TabPaddingPx = new(10f, 6f);

    private static float Scale => ImGuiHelpers.GlobalScale;
    public static float RowGap => RowGapPx * Scale;
    public static float Underline => UnderlinePx * Scale;
    public static float PillRounding => PillRoundingPx * Scale;
    public static Vector2 TabPadding => TabPaddingPx * Scale;

    public static uint U32(Vector4 color) => ImGui.ColorConvertFloat4ToU32(color);

    // Window-level style; pushed before Begin, so it covers the window frame too.
    public static IDisposable Push()
    {
        var scope = new StyleScope();
        scope.Color(ImGuiCol.WindowBg, Window);
        scope.Color(ImGuiCol.ChildBg, Window);
        scope.Color(ImGuiCol.PopupBg, Element);
        scope.Color(ImGuiCol.Border, Line);
        scope.Color(ImGuiCol.Separator, Line);
        scope.Color(ImGuiCol.Text, Text);
        scope.Color(ImGuiCol.TextDisabled, Muted);
        scope.Color(ImGuiCol.FrameBg, Element);
        scope.Color(ImGuiCol.FrameBgHovered, ElementHover);
        scope.Color(ImGuiCol.FrameBgActive, ElementHover);
        scope.Color(ImGuiCol.Button, Element);
        scope.Color(ImGuiCol.ButtonHovered, ElementHover);
        scope.Color(ImGuiCol.ButtonActive, ElementHover);
        scope.Color(ImGuiCol.Header, ElementHover);
        scope.Color(ImGuiCol.HeaderHovered, ElementHover);
        scope.Color(ImGuiCol.HeaderActive, ElementHover);
        scope.Color(ImGuiCol.CheckMark, Accent);
        scope.Color(ImGuiCol.TitleBg, Window);
        scope.Color(ImGuiCol.TitleBgActive, Window);
        scope.Color(ImGuiCol.TitleBgCollapsed, Window);
        scope.Var(ImGuiStyleVar.WindowRounding, WindowRoundingPx * Scale);
        scope.Var(ImGuiStyleVar.WindowBorderSize, 1f);
        scope.Var(ImGuiStyleVar.ChildBorderSize, 0f);
        scope.Var(ImGuiStyleVar.FrameBorderSize, 0f);
        scope.Var(ImGuiStyleVar.FrameRounding, FrameRoundingPx * Scale);
        scope.Var(ImGuiStyleVar.PopupRounding, PopupRoundingPx * Scale);
        scope.Var(ImGuiStyleVar.WindowPadding, WindowPaddingPx * Scale);
        scope.Var(ImGuiStyleVar.FramePadding, FramePaddingPx * Scale);
        scope.Var(ImGuiStyleVar.ItemSpacing, new Vector2(RowGapPx, RowGapPx) * Scale);
        return scope;
    }

    // The filled accent button (Start, Try it).
    public static IDisposable PushAccentButton()
    {
        var scope = new StyleScope();
        scope.Color(ImGuiCol.Button, Accent);
        scope.Color(ImGuiCol.ButtonHovered, AccentHover);
        scope.Color(ImGuiCol.ButtonActive, AccentHover);
        scope.Color(ImGuiCol.Text, OnAccent);
        return scope;
    }

    public static IDisposable PushPill()
    {
        var scope = new StyleScope();
        scope.Var(ImGuiStyleVar.FrameRounding, PillRounding);
        return scope;
    }

    public static IDisposable PushTextColor(Vector4 color)
    {
        var scope = new StyleScope();
        scope.Color(ImGuiCol.Text, color);
        return scope;
    }

    private static Vector4 Rgb(uint rgb)
        => new(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, 1f);

    // Counts its own pushes so a scope always pops exactly what it pushed.
    private sealed class StyleScope : IDisposable
    {
        private int colors;
        private int vars;
        private bool disposed;

        public void Color(ImGuiCol col, Vector4 value)
        {
            ImGui.PushStyleColor(col, value);
            colors++;
        }

        public void Var(ImGuiStyleVar var, float value)
        {
            ImGui.PushStyleVar(var, value);
            vars++;
        }

        public void Var(ImGuiStyleVar var, Vector2 value)
        {
            ImGui.PushStyleVar(var, value);
            vars++;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (vars > 0) ImGui.PopStyleVar(vars);
            if (colors > 0) ImGui.PopStyleColor(colors);
        }
    }
}
