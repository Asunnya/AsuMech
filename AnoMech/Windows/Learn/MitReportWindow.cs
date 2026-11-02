using System.Linq;
using System.Numerics;
using AnoMech.Learn.Mitigation;
using AnoMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows.Learn;

// Mechanic × expected × used × deviation for the last run, the player's column only.
internal sealed class MitReportWindow : Window
{
    public MitReportWindow() : base("Mitigation report###AsuMechMitReport")
    {
        Size = new Vector2(620f, 380f);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        using var theme = AsuTheme.Push();
        if (MitTraining.Instance is not { LastReport: { } report } training)
        {
            ImGui.TextUnformatted("No run with mitigation training yet.");
            return;
        }
        ImGui.TextUnformatted(training.LastReportTitle ?? "");
        var errors = report.Count(v => v.IsError);
        var judged = report.Count(v => v.Result != MitResult.Reminder);
        using (AsuTheme.PushTextColor(errors == 0 ? AsuTheme.Ok : AsuTheme.Accent))
            ImGui.TextUnformatted($"{judged - errors}/{judged} on time");
        if (!ImGui.BeginTable("mit-report", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp)) return;
        ImGui.TableSetupColumn("Mechanic");
        ImGui.TableSetupColumn("Expected");
        ImGui.TableSetupColumn("Used");
        ImGui.TableSetupColumn("Result");
        ImGui.TableSetupColumn("Deviation");
        ImGui.TableHeadersRow();
        foreach (var v in report)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted(v.Expectation.Label);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(v.Expectation.Item.Reminder ?? v.Expectation.Item.ExpectStatus ?? (v.Expectation.Item.Forbid is { Count: > 0 } f ? $"not {string.Join("/", f)}" : string.Join("/", v.Expectation.AcceptIds.Select(id => MitCatalog.Find(id)?.Name ?? id.ToString()))));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(v.Use is { } u ? $"{MitCatalog.Find(u.ActionId)?.Name ?? u.ActionId.ToString()} @ {u.Time:0.0}s" : "—");
            ImGui.TableNextColumn();
            using (AsuTheme.PushTextColor(v.Result == MitResult.OnTime ? AsuTheme.Ok : v.IsError ? AsuTheme.Bad : AsuTheme.Muted))
                ImGui.TextUnformatted(v.Result.ToString());
            ImGui.TableNextColumn(); ImGui.TextUnformatted(v.Use is { } use ? $"{use.Time - v.Expectation.Closes:+0.0;-0.0}s" : v.Cause ?? "");
        }
        ImGui.EndTable();
        if (training.LastOffPlan is { Count: > 0 } off)
            using (AsuTheme.PushTextColor(AsuTheme.Muted))
                ImGui.TextWrapped($"Off plan: {string.Join(", ", off.Select(u => $"{MitCatalog.Find(u.ActionId)?.Name} @ {u.Time:0}s"))}");
    }
}
