using System;
using System.Numerics;
using AsuMech.Scenarios;
using Dalamud.Bindings.ImGui;

namespace AsuMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(6);

    private string? toast;
    private DateTime toastUntil;

    private void OnDrillReached(string scenario, int streak)
    {
        toast = $"Drilled {scenario}: {streak} clean in a row.";
        toastUntil = DateTime.UtcNow + ToastDuration;
    }

    private static string StreakText()
        => $"streak {Plugin.Practice.Streak}{(Plugin.Drill.StreakTarget is { } t ? $"/{t}" : "")}";

    private static string BestStreakText(IScenario scenario)
        => Plugin.Drill.BestOf(scenario.FullName()) is var best and > 0 ? $" · best {best}" : "";

    private static string DrillLabel() => $"Drill: {(Plugin.Drill.StreakTarget is { } t ? t.ToString() : "off")}##drill";

    private float StreakWidth() => Flow.TextWidth(StreakText()) + ImGui.GetStyle().ItemSpacing.X + Flow.ButtonWidth(DrillLabel());

    private void DrawStreak(bool inSession)
    {
        ImGui.AlignTextToFramePadding();
        Muted(StreakText());
        Flow.SameLineIfFits(Flow.ButtonWidth(DrillLabel()));
        ImGui.BeginDisabled(inSession);
        bool clicked;
        using (AsuTheme.PushPill())
        using (Plugin.Drill.StreakTarget != null ? AsuTheme.PushAccentButton() : null)
            clicked = ImGui.Button(DrillLabel());
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(inSession
                ? SessionDisabledReason
                : "Loop until this many clean runs in a row, then stop. Click to cycle 3, 5, 10, off.");
        if (!clicked) return;
        Plugin.Drill.CycleTarget();
        if (Plugin.Drill.StreakTarget != null) Plugin.Practice.AutoRestart = true;
    }

    private void DrawToast()
    {
        if (toast == null) return;
        if (DateTime.UtcNow > toastUntil)
        {
            toast = null;
            return;
        }
        var pos = ImGui.GetCursorScreenPos();
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, AsuTheme.U32(AsuTheme.ToastBackground), AsuTheme.PillRounding * 0.5f);
        ImGui.SetCursorScreenPos(pos + ImGui.GetStyle().FramePadding);
        using (AsuTheme.PushTextColor(AsuTheme.ToastText))
            ImGui.TextUnformatted(toast);
        ImGui.SetCursorScreenPos(pos with { Y = pos.Y + size.Y + ImGui.GetStyle().ItemSpacing.Y });
        ImGui.Dummy(Vector2.Zero);
    }
}
