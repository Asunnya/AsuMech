using System.Numerics;
using AsuMech.Multiplayer;
using AsuMech.Network;
using AsuMech.Scenarios;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

namespace AsuMech.Windows.Asu;

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

    private string feedbackText = "";
    private string? feedbackStatus;
    private string? feedbackSaved;
    private bool feedbackSending;
    private readonly System.Collections.Concurrent.ConcurrentQueue<StudyRelayPost.Result> feedbackResults = new();

    private static string WireKind(FeedbackKind kind) => kind switch
    {
        FeedbackKind.Scenario => RunReportWire.ScenarioRequest,
        FeedbackKind.Strat => RunReportWire.StratRequest,
        _ => RunReportWire.Bug,
    };

    private void DrawFeedbackPopup()
    {
        while (feedbackResults.TryDequeue(out var result))
        {
            feedbackSending = false;
            feedbackStatus = result.Ok ? "Sent, thank you." : $"Not sent: {result.Message}";
            if (result.Ok) feedbackText = "";
        }
        if (openFeedbackNextFrame)
        {
            ImGui.OpenPopup("feedback-popup");
            openFeedbackNextFrame = false;
            feedbackStatus = null;
            feedbackSaved = null;
        }
        ImGui.SetNextWindowSize(new Vector2(FeedbackPopupPx * AsuTheme.Scale, 0f));
        if (!ImGui.BeginPopup("feedback-popup")) return;
        var scenario = (Plugin.Game.LastRun?.Scenario ?? Selected)?.DisplayName();
        Option(FeedbackKind.Scenario, "Request a scenario");
        Option(FeedbackKind.Strat, scenario is null ? "Request a strat" : $"Request a strat for {scenario}");
        Option(FeedbackKind.Bug, scenario is null ? "Report a problem" : $"Report a problem with {scenario}");

        if (pendingFeedback is { } kind && feedbackPreview is { } issue)
        {
            ImGui.Separator();
            using (AsuTheme.PushTextColor(AsuTheme.Muted))
                ImGui.TextWrapped(kind switch
                {
                    FeedbackKind.Scenario => "Which fight and mechanic, and why it's worth practicing?",
                    FeedbackKind.Strat => "Which strat? A raidplan, toolbox or guide link helps most.",
                    _ => "What happened, and what did you expect? The run's details (time, mechanic, rolls, positions) are added for you. No names are sent.",
                });
            ImGui.InputTextMultiline("##feedback-text", ref feedbackText, RunReportWire.MaxTextChars, new Vector2(-1f, ImGui.GetTextLineHeight() * 5f));
            bool send;
            ImGui.BeginDisabled(feedbackSending || feedbackText.Trim().Length == 0);
            using (AsuTheme.PushAccentButton())
                send = ImGui.Button(feedbackSending ? "Sending…" : "Send");
            ImGui.EndDisabled();
            if (send) SendFeedback(kind);
            ImGui.SameLine();
            if (ImGui.Button("Open on GitHub ↗")) Util.OpenLink(issue.Url);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("A pre-filled GitHub issue instead; needs a GitHub account.");
            if (feedbackStatus is { } status)
                using (AsuTheme.PushTextColor(status.StartsWith("Sent", System.StringComparison.Ordinal) ? AsuTheme.Ok : AsuTheme.Bad))
                    ImGui.TextWrapped(status);
            if (feedbackSaved is { } saved)
            {
                using (AsuTheme.PushTextColor(AsuTheme.Muted))
                    ImGui.TextWrapped($"A copy is saved as {System.IO.Path.GetFileName(saved)}.");
                if (ImGui.SmallButton("Open folder")) Util.OpenLink(System.IO.Path.GetDirectoryName(saved)!);
            }
        }
        ImGui.EndPopup();
    }

    private void SendFeedback(FeedbackKind kind)
    {
        var report = RunReport.Build(WireKind(kind), feedbackText.Trim(), Selected, Picker.SelectedAi?.Name);
        try { feedbackSaved = RunReport.Save(report); }
        catch (System.Exception e) { feedbackSaved = null; Core.DiagnosticLog.Warn($"[Feedback] saving the report failed: {e.Message}"); }
        var json = report.ToJsonString();
        var (url, token, credential) = StudyRelayPost.Destination();
        feedbackSending = true;
        feedbackStatus = null;
        System.Threading.Tasks.Task.Run(async () => feedbackResults.Enqueue(await StudyRelayPost.SendAsync("reports", json, url, token, credential)));
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
