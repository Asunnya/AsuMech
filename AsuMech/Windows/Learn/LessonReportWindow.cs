using System;
using System.Linq;
using System.Numerics;
using AsuMech.Learn;
using AsuMech.Network;
using AsuMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AsuMech.Windows.Learn;

// Step × result × timing × distance for the last judged run. Opens by itself after an Exam, since
// the Exam shows nothing during the run.
internal sealed class LessonReportWindow : Window, IDisposable
{
    public LessonReportWindow() : base("Lesson report###AsuMechLessonReport")
    {
        Size = new Vector2(560f, 380f);
        SizeCondition = ImGuiCond.FirstUseEver;
        LearnSession.ReportReady += OnReport;
    }

    public void Dispose() => LearnSession.ReportReady -= OnReport;

    private void OnReport(LessonReport report)
    {
        if (report.Level == LearnLevel.Exam) IsOpen = true;
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
        if (LearnSession.LastReport is not { } report)
        {
            ImGui.TextUnformatted("No judged lesson run yet.");
            return;
        }
        ImGui.TextUnformatted(report.Title);
        using (AsuTheme.PushTextColor(report.InPlace == report.Results.Count ? AsuTheme.Ok : AsuTheme.Accent))
            ImGui.TextUnformatted(report.Summary);
        if (!ImGui.BeginTable("lesson-report", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp)) return;
        ImGui.TableSetupColumn("Step");
        ImGui.TableSetupColumn("Result");
        ImGui.TableSetupColumn("Arrived");
        ImGui.TableSetupColumn("Off by");
        ImGui.TableSetupColumn("Text clear?", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();
        var row = 0;
        foreach (var (step, result) in report.Results)
        {
            row++;
            var eval = step.Evaluation;
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted($"{step.Order.Key.Time:0.#}s  {step.Title}");
            ImGui.TableNextColumn();
            using (AsuTheme.PushTextColor(result == StepResult.OnTime ? AsuTheme.Ok : result == StepResult.Late ? AsuTheme.Accent : AsuTheme.Bad))
                ImGui.TextUnformatted(Label(result));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(eval.ArrivedAt is { } at ? $"{at - eval.Deadline:+0.0;-0.0}s" : "—");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(result == StepResult.OnTime || eval.ResolvedDistance is not { } d ? "" : $"{d:0.0} yalms");
            ImGui.TableNextColumn();
            if (step.Step is { } voted) LessonVoteButtons.Draw(report.Lesson, MasteryStore.StepId(voted.Key), LessonFeedback.Text);
            ImGui.TableNextColumn();
            if (step.Step is { Skip: false } lessonStep && ImGui.SmallButton($"Drill##{row}")) StepDrillRunner.Instance?.Start(report, lessonStep);
        }
        ImGui.EndTable();
        DrawHits(report);
        DrawMap(report);
    }

    // Every step with enough tries, worst first: where the player fails, how slowly they react,
    // and whether it is getting better.
    private static void DrawMap(LessonReport report)
    {
        var map = Plugin.StepStats.Map(report.Lesson, report.Role);
        ImGui.Spacing();
        ImGui.TextUnformatted($"Your steps as {report.Role}");
        if (map.Count == 0)
        {
            using (AsuTheme.PushTextColor(AsuTheme.Muted))
                ImGui.TextWrapped($"A step shows up here after {StepStatsStore.MinAttemptsForMap} judged tries.");
        }
        else if (ImGui.BeginTable("lesson-map", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Step");
            ImGui.TableSetupColumn("Fails");
            ImGui.TableSetupColumn("Reaction");
            ImGui.TableSetupColumn("Trend");
            ImGui.TableSetupColumn("Usually");
            ImGui.TableHeadersRow();
            foreach (var t in map)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted($"{t.Step.Key.Time ?? t.Step.Key.From ?? 0f:0.#}s  {t.Step.Title}");
                ImGui.TableNextColumn();
                using (AsuTheme.PushTextColor(t.FailRate >= 0.5f ? AsuTheme.Bad : t.FailRate > 0f ? AsuTheme.Accent : AsuTheme.Ok))
                    ImGui.TextUnformatted($"{t.FailRate:P0} of {t.Attempts}");
                ImGui.TableNextColumn(); ImGui.TextUnformatted(t.MeanReaction is { } r ? $"{r:0.0}s" : "—");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(t.Trend switch { null => "—", < -0.05f => "better", > 0.05f => "worse", _ => "steady" });
                ImGui.TableNextColumn(); ImGui.TextWrapped(t.CommonFailure ?? "");
            }
            ImGui.EndTable();
        }
        if (report.Level != LearnLevel.Exam || StepDrillRunner.Instance is not { } runner) return;
        var worst = StepDrillPlanner.Worst(report.Lesson, report.Role, Plugin.StepStats, report);
        if (worst.Count == 0) return;
        ImGui.Spacing();
        ImGui.TextWrapped($"Drill the weakest: {string.Join(", ", worst.Select(s => s.Title ?? MasteryStore.StepId(s.Key)))}, each until {StepDrillPlanner.AdaptiveStreak} in a row.");
        using (AsuTheme.PushAccentButton())
            if (ImGui.Button("Start the drill##adaptive-drill")) runner.StartAdaptive(report, worst);
    }

    private static void DrawHits(LessonReport report)
    {
        if (report.Hits.Count == 0) return;
        ImGui.Spacing();
        using (AsuTheme.PushTextColor(AsuTheme.Bad))
            ImGui.TextUnformatted($"{report.Hits.Count} hit{(report.Hits.Count == 1 ? "" : "s")} taken");
        if (!ImGui.BeginTable("lesson-hits", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        ImGui.TableSetupColumn("When");
        ImGui.TableSetupColumn("Step");
        ImGui.TableSetupColumn("Why");
        ImGui.TableSetupColumn("From the spot");
        ImGui.TableHeadersRow();
        foreach (var hit in report.Hits)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted($"{hit.Time:0.#}s");
            ImGui.TableNextColumn(); ImGui.TextUnformatted(hit.StepTitle ?? "—");
            ImGui.TableNextColumn();
            using (AsuTheme.PushTextColor(hit.Fatal ? AsuTheme.Bad : AsuTheme.Text))
                ImGui.TextUnformatted(hit.Phrase);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(hit.StepTarget is { } target ? $"{System.Numerics.Vector3.Distance(target, hit.Position):0.0} yalms" : "—");
        }
        ImGui.EndTable();
    }

    private static string Label(StepResult result) => result switch
    {
        StepResult.OnTime => "In place",
        StepResult.Late => "Late",
        StepResult.WrongPlace => "Wrong place",
        _ => "Mechanic failed",
    };
}
