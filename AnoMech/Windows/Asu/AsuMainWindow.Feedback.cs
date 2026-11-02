using System.Numerics;
using AnoMech.Multiplayer;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

namespace AnoMech.Windows.Asu;

public sealed partial class AsuMainWindow
{
    private const float FeedbackPopupPx = 320f;

    private FeedbackKind? pendingFeedback;
    private FeedbackIssue? feedbackPreview;

    private void DrawFeedbackButton()
    {
        if (ImGuiComponents.IconButton("feedback", FontAwesomeIcon.CommentAlt)) OpenFeedback(null);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Request a scenario or strat, or report a problem");
    }

    private void DrawRequestLink(FeedbackKind kind)
    {
        using (AsuTheme.PushTextColor(AsuTheme.Accent))
            if (ImGui.SmallButton($"Request one##request-{kind}")) OpenFeedback(kind);
    }

    // Opened from the window root: a request link can sit inside another popup.
    private void OpenFeedback(FeedbackKind? kind)
    {
        pendingFeedback = kind;
        feedbackPreview = kind is { } k ? FeedbackLinks.Build(k, FeedbackContextNow()) : null;
        openFeedbackNextFrame = true;
    }

    private bool openFeedbackNextFrame;

    private FeedbackContext FeedbackContextNow()
    {
        var role = Picker.RoleOverride is { } r ? ScenarioSetupUi.RoleLabels[(int)r + 1] : null;
        var seed = Plugin.Game.LastRun != null ? Plugin.Game.World.Rng.Seed : (int?)null;
        return new FeedbackContext(Selected?.FullName(), Picker.SelectedAi?.Name, role, RunState.InSession, seed, PluginBuildInfo.Version);
    }

    private void DrawFeedbackPopup()
    {
        if (openFeedbackNextFrame)
        {
            ImGui.OpenPopup("feedback-popup");
            openFeedbackNextFrame = false;
        }
        ImGui.SetNextWindowSize(new Vector2(FeedbackPopupPx * ImGuiHelpers.GlobalScale, 0f));
        if (!ImGui.BeginPopup("feedback-popup")) return;
        ImGui.TextWrapped("Opens a pre-filled GitHub issue. You review it before posting.");
        ImGui.Separator();
        var scenario = Selected?.DisplayName();
        Option(FeedbackKind.Scenario, "Request a scenario");
        Option(FeedbackKind.Strat, scenario is null ? "Request a strat" : $"Request a strat for {scenario}");
        Option(FeedbackKind.Bug, scenario is null ? "Report a problem" : $"Report a problem with {scenario}");

        if (feedbackPreview is { } issue)
        {
            ImGui.Separator();
            ImGui.TextUnformatted(issue.Title);
            using (AsuTheme.PushTextColor(AsuTheme.Muted))
                ImGui.TextWrapped(issue.Body);
            bool open;
            using (AsuTheme.PushAccentButton())
                open = ImGui.Button("Open on GitHub ↗");
            if (open) Util.OpenLink(issue.Url);
            ImGui.SameLine();
            if (ImGui.Button("Copy text")) ImGui.SetClipboardText($"{issue.Title}\n\n{issue.Body}");
        }
        ImGui.EndPopup();
    }

    private void Option(FeedbackKind kind, string label)
    {
        if (ImGui.Selectable(label, pendingFeedback == kind))
        {
            pendingFeedback = kind;
            feedbackPreview = FeedbackLinks.Build(kind, FeedbackContextNow());
        }
    }
}
