using System;
using System.Numerics;
using AnoMech.Learn;
using AnoMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Learn;

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

    public override void Draw()
    {
        using var theme = AsuTheme.Push();
        if (LearnSession.LastReport is not { } report)
        {
            ImGui.TextUnformatted("No judged lesson run yet.");
            return;
        }
        ImGui.TextUnformatted(report.Title);
        using (AsuTheme.PushTextColor(report.InPlace == report.Results.Count ? AsuTheme.Ok : AsuTheme.Accent))
            ImGui.TextUnformatted(report.Summary);
        if (!ImGui.BeginTable("lesson-report", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp)) return;
        ImGui.TableSetupColumn("Step");
        ImGui.TableSetupColumn("Result");
        ImGui.TableSetupColumn("Arrived");
        ImGui.TableSetupColumn("Off by");
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
            if (step.Step is { Skip: false } lessonStep && ImGui.SmallButton($"Drill##{row}")) StepDrillRunner.Instance?.Start(report, lessonStep);
        }
        ImGui.EndTable();
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
