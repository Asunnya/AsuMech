using AsuMech.Core.Game;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;

namespace AsuMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private enum Mode { Practice, Learn }

    private Mode mode = Mode.Practice;
    // Measured after drawing, used next frame: icon buttons are wider than a frame is tall.
    private float titleIconsWidth;
    private float minimizedRightWidth;
    private bool minimized;

    private void DrawTitleRow(Game game)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("AsuMech");
        var (label, color) = RunState.Status(game, Selected);
        ImGui.SameLine();
        using (AsuTheme.PushTextColor(color))
            ImGui.TextUnformatted($"· {label}");
        if (game.LastRun is { Issues.Count: > 0 } run && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            foreach (var issue in run.Issues)
                ImGui.TextUnformatted($"{issue.Time,6:F1}s  {issue.Description}");
            ImGui.EndTooltip();
        }

        // Down to P and L when the full names would push the row onto a second line.
        var room = ImGui.GetWindowContentRegionMax().X - (ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X) - ImGui.GetStyle().ItemSpacing.X;
        var compact = titleIconsWidth > 0f && ModeWidth(false) + titleIconsWidth > room;
        Flow.SameLineRight(titleIconsWidth > 0f ? ModeWidth(compact) + titleIconsWidth : 0f);
        ImGui.BeginGroup();
        var groupStart = ImGui.GetCursorScreenPos().X;
        if (ModeButton(compact ? "P##mode-practice" : "Practice##mode-practice", mode == Mode.Practice, compact)) mode = Mode.Practice;
        ImGui.SameLine(0f, 0f);
        if (ModeButton(compact ? "L##mode-learn" : "Learn##mode-learn", mode == Mode.Learn, compact)) mode = Mode.Learn;
        var modeEnd = ImGui.GetItemRectMax().X;

        ImGui.SameLine();
        if (ImGuiComponents.IconButton("discord", FontAwesomeIcon.Users)) Util.OpenLink(FeedbackLinks.Discord);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("AsuMech Discord");
        ImGui.SameLine();
        DrawFeedbackButton();
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("settings", FontAwesomeIcon.Cog)) plugin.ToggleConfigUi();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Settings");
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("minimize", FontAwesomeIcon.WindowMinimize)) minimized = true;
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Minimize");
        ImGui.SameLine();
        DrawCloseButton();
        ImGui.EndGroup();
        titleIconsWidth = ImGui.GetItemRectSize().X - (modeEnd - groupStart);
        ImGui.Separator();
    }

    private void DrawCloseButton()
    {
        ImGui.BeginDisabled(Plugin.Game.World.Map.IsInInstance);
        if (ImGuiComponents.IconButton("close", FontAwesomeIcon.Times)) IsOpen = false;
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(Plugin.Game.World.Map.IsInInstance ? "Leave the instance first." : "Close");
    }

    // One line: the name, the status and the way back.
    private void DrawMinimized(Game game)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("AsuMech");
        var (label, color) = RunState.Status(game, Selected);
        var status = $"· {label}";
        Flow.SameLineIfFits(Flow.TextWidth(status));
        using (AsuTheme.PushTextColor(color))
            ImGui.TextUnformatted(status);

        Flow.SameLineRight(minimizedRightWidth);
        ImGui.BeginGroup();
        if (ImGuiComponents.IconButton("restore", FontAwesomeIcon.WindowMaximize)) minimized = false;
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Restore");
        ImGui.SameLine();
        DrawCloseButton();
        ImGui.EndGroup();
        minimizedRightWidth = ImGui.GetItemRectSize().X;
    }

    private static float ModeWidth(bool compact)
        => compact ? Flow.ButtonWidth("P") + Flow.ButtonWidth("L") : Flow.ButtonWidth("Practice") + Flow.ButtonWidth("Learn");

    private static bool ModeButton(string label, bool active, bool compact)
    {
        bool clicked;
        using (active ? AsuTheme.PushAccentButton() : null)
            clicked = ImGui.Button(label);
        if (compact && ImGui.IsItemHovered()) ImGui.SetTooltip(label.Contains("practice") ? "Practice" : "Learn");
        return clicked;
    }
}
