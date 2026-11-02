using AnoMech.Core.Game;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private enum Mode { Practice, Learn }

    private Mode mode = Mode.Practice;

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

        var style = ImGui.GetStyle();
        var iconWidth = ImGui.GetFrameHeight();
        var switchWidth = ImGui.CalcTextSize("Practice").X + ImGui.CalcTextSize("Learn").X + style.FramePadding.X * 4 + style.ItemSpacing.X;
        var rightWidth = switchWidth + (iconWidth + style.ItemSpacing.X) * 3;
        ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - rightWidth);

        if (ModeButton("Practice", mode == Mode.Practice)) mode = Mode.Practice;
        ImGui.SameLine(0f, 0f);
        if (ModeButton("Learn", mode == Mode.Learn)) mode = Mode.Learn;

        ImGui.SameLine();
        DrawFeedbackButton();
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("settings", FontAwesomeIcon.Cog)) plugin.ToggleConfigUi();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Settings");
        ImGui.SameLine();
        ImGui.BeginDisabled(Plugin.Game.World.Map.IsInInstance);
        if (ImGuiComponents.IconButton("close", FontAwesomeIcon.Times)) IsOpen = false;
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(Plugin.Game.World.Map.IsInInstance ? "Leave the instance first." : "Close");
        ImGui.Separator();
    }

    private static bool ModeButton(string label, bool active)
    {
        using var colors = active ? AsuTheme.PushAccentButton() : null;
        return ImGui.Button(label);
    }
}
