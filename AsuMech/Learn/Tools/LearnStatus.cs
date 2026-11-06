using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsuMech.Learn.Mitigation;
using AsuMech.Learn.Toolbox;
using AsuMech.Scenarios;

namespace AsuMech.Learn.Tools;

// One strat's Learn coverage: what exists, embedded, for it.
public sealed record StatusRow(string Family, string Scenario, string Strat, bool Lesson, bool Draft, int Steps, bool Registered, bool Toolbox,
    bool PlanEmbedded, bool ExampleSeed, bool Example, bool Glossary, bool MitTimeline, bool MitPlan)
{
    public string Id => $"{Scenario} / {Strat}";
}

// The scenario × strat matrix the plan tracks; a test prints it and a lesson may never drop out.
public static class LearnStatus
{
    public static List<StatusRow> Rows(ScenarioCatalog catalog)
    {
        var rows = new List<StatusRow>();
        foreach (var scenario in catalog.Scenarios)
        {
            var type = scenario.GetType();
            var family = Glossary.FamilyOf(type);
            var glossary = Glossary.Load(family).Fields.Count > 0;
            var timeline = MitTimelines.Load(type.Name) != null;
            var mitPlan = MitTimelines.PlansFor(type.Name).Any();
            foreach (var strat in scenario.AiStrats)
            {
                var lesson = LessonLoader.For(scenario, strat);
                var link = lesson?.Toolbox;
                rows.Add(new StatusRow(family, type.Name, strat.Name,
                    Lesson: lesson != null, Draft: lesson?.Draft ?? false, Steps: lesson?.Steps.Count ?? 0,
                    Registered: PlanRegistry.For(type.Name, strat.Name) != null,
                    Toolbox: link != null, PlanEmbedded: link != null && ToolboxPlans.Load(link.PlanId) != null,
                    ExampleSeed: link?.ExampleSeed != null, Example: link?.Example is { Count: > 0 },
                    Glossary: glossary, MitTimeline: timeline, MitPlan: mitPlan));
            }
        }
        return rows;
    }

    public static string Table(IEnumerable<StatusRow> rows)
    {
        var list = rows.ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"{"scenario / strat",-62} lesson draft steps plans toolbox embed seed example gloss timeline mit");
        foreach (var r in list)
            sb.AppendLine($"{r.Id,-62} {M(r.Lesson),-6} {M(r.Draft),-5} {r.Steps,5} {M(r.Registered),-5} {M(r.Toolbox),-7} {M(r.PlanEmbedded),-5} {M(r.ExampleSeed),-4} {M(r.Example),-7} {M(r.Glossary),-5} {M(r.MitTimeline),-8} {M(r.MitPlan)}");
        sb.AppendLine($"{list.Count} strats, {list.Count(r => r.Lesson)} lessons ({list.Count(r => r.Lesson && !r.Draft)} reviewed), {list.Count(r => r.Toolbox)} with a toolbox, {list.Count(r => r.MitPlan)} with a mit plan");
        foreach (var family in list.GroupBy(r => r.Family))
            sb.AppendLine($"  {family.Key,-6} {family.Count(r => r.Lesson)}/{family.Count()} lessons, {family.Count(r => r.Registered)} registered plans, {family.Count(r => r.MitTimeline)} timelines");
        return sb.ToString();
    }

    // Per family: how much of the drafted text still needs a human (tables, geometric steps), how
    // many steps carry a cue, how far the plans sit from the strats, and how much is reviewed.
    public static string Metrics(IEnumerable<Lesson> lessons)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{"family",-6} lessons reviewed  steps  sentences  cues  mean residual (plans)");
        foreach (var family in lessons.GroupBy(l => FamilyFromScenario(l.Scenario)).OrderBy(g => g.Key))
        {
            var steps = family.SelectMany(l => l.Steps.Where(s => !s.Skip)).ToList();
            var rules = steps.SelectMany(s => new[] { s.Rule }.Concat(s.Roles?.Values.Select(r => r.Rule) ?? [])).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            var sentences = rules.Count == 0 ? 0f : 100f * rules.Count(r => !Lesson.IsTable(r)) / rules.Count;
            var cues = steps.Count == 0 ? 0f : 100f * steps.Count(s => !string.IsNullOrWhiteSpace(s.Cue) || (s.Roles?.Values.Any(r => !string.IsNullOrWhiteSpace(r.Cue)) ?? false)) / steps.Count;
            var residuals = family.Select(l => l.Toolbox?.Residual).OfType<float>().ToList();
            sb.AppendLine($"{family.Key,-6} {family.Count(),7} {family.Count(l => !l.Draft),8}  {steps.Count,5}  {sentences,8:0}%  {cues,3:0}%  {(residuals.Count == 0 ? "-" : $"{residuals.Average():0.0} m ({residuals.Count})")}");
        }
        return sb.ToString();
    }

    // "UwuP1GarudaScenario" -> "Uwu", "UltimatePredationScenario" -> "Uwu" by its known prefixes.
    private static string FamilyFromScenario(string scenario)
    {
        foreach (var family in new[] { "Umad", "Fru", "Top", "Uwu", "Ucob", "Dsr", "M9s", "M10s", "M11s" })
            if (scenario.StartsWith(family, System.StringComparison.Ordinal)) return family;
        return scenario.StartsWith("Ultimate", System.StringComparison.Ordinal) || scenario.StartsWith("PrimalRoulette", System.StringComparison.Ordinal) ? "Uwu" : "?";
    }

    private static string M(bool b) => b ? "yes" : "-";
}
