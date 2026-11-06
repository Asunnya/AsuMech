using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsuMech.Core.Game;
using AsuMech.Learn.Toolbox;

namespace AsuMech.Learn.Tools;

// Everything an author needs per step, as one Markdown file: the spot of every role and the rolls
// that pick it, the cue candidates, and how the toolbox agrees with the strat.
public static class LessonContext
{
    private static readonly string[] Roles = ["MainTank", "OffTank", "RegenHealer", "ShieldHealer", "MeleeDpsA", "MeleeDpsB", "PhysRangedDps", "CasterDps"];

    public static string Write(OrderDump dump, Lesson skeleton, List<MinedRule> rules, Dictionary<(string Cls, int Line, string Role), List<CueCandidate>> cues,
        OracleReport? oracle = null, ToolboxPlan? plan = null, WaymarkLayout? waymarks = null, IReadOnlyList<Sources.WtfdigMechanic>? texts = null, ExampleExtraction? example = null)
    {
        var sb = new StringBuilder();
        var seeds = dump.Orders.Select(o => o.Seed).Distinct().Count();
        sb.AppendLine($"# {dump.Scenario} / {dump.Strat}");
        sb.AppendLine();
        sb.AppendLine($"{skeleton.Steps.Count} steps, {dump.Orders.Count} orders over {seeds} seeds × 8 roles{(waymarks is null ? "" : $", spots named against the {waymarks.Name} layout")}.");
        if (dump.Runs is { Count: > 0 } runs)
        {
            var fields = runs[0].State.Keys.ToList();
            sb.AppendLine($"State fields seen: {string.Join(", ", fields)}.");
            var unexplained = rules.Count(r => !r.Explained);
            if (unexplained > 0) sb.AppendLine($"{unexplained} role×step spots vary across seeds without a field that explains them: more seeds, or a roll the *State doesn't expose.");
        }
        if (oracle != null)
        {
            sb.AppendLine();
            sb.AppendLine($"## Toolbox oracle: example seed {oracle.ExampleSeed}, mean residual {oracle.TotalResidual / System.Math.Max(1, oracle.Steps.Count):F1} m over {oracle.Steps.Count} aligned steps");
            foreach (var s in oracle.Disagreements)
                sb.AppendLine($"- toolbox {s.ToolboxStep} \"{s.Label}\" vs {s.StepId}: {s.Residual:F1} m ({string.Join(", ", s.RolesOff.Select(kv => $"{kv.Key} {kv.Value:F1} m"))})");
            if (!oracle.Disagreements.Any()) sb.AppendLine("every aligned step agrees within its tolerance");
        }
        if (example != null)
        {
            sb.AppendLine();
            sb.AppendLine($"## Example from the plan: {example.Pins.Count} roll(s) pinned, {example.Skipped.Count} left out");
            foreach (var pin in example.Pins) sb.AppendLine($"- {pin.Property} = {pin.Value} ({pin.How})");
            foreach (var skip in example.Skipped) sb.AppendLine($"- not pinned: {skip}");
        }
        foreach (var step in skeleton.Steps)
        {
            var line = step.Key.Line ?? -1;
            var stepRules = rules.Where(r => r.Cls == step.Key.Cls && r.Line == line).ToList();
            sb.AppendLine();
            sb.AppendLine($"## {MasteryStore.StepId(step.Key)} (line {line}) — {step.Title}");
            var deadline = dump.Orders.Where(o => step.Key.Matches(o.Key) && o.ArrivalTime != null).Select(o => o.ArrivalTime!.Value).DefaultIfEmpty().Max();
            if (deadline > 0f) sb.AppendLine($"deadline (arrival): {deadline:0.0}s");
            if (plan != null && skeleton.Toolbox?.Steps.TryGetValue(MasteryStore.StepId(step.Key), out var tb) == true && tb < plan.Steps.Count)
            {
                var toolbox = plan.Steps[tb];
                sb.AppendLine($"toolbox {tb}: \"{toolbox.Label}\"{(toolbox.Note.Length > 0 ? " — " + toolbox.Note.Split('\n')[0].Trim() : "")}");
            }
            foreach (var role in Roles)
            {
                if (stepRules.FirstOrDefault(r => r.Role == role) is not { } rule) continue;
                if (rule.Fixed)
                    sb.AppendLine($"- {role}: {rule.Spots[0].Label} ({rule.Spots[0].X:0.#}, {rule.Spots[0].Z:0.#})");
                else if (rule.Relation is { } relation)
                    sb.AppendLine($"- {role}: {relation} → {string.Join("; ", rule.Spots.Select(s => s.Label))}");
                else if (rule.Fields.Count > 0)
                    sb.AppendLine($"- {role}: by {string.Join(" & ", rule.Fields)} → {string.Join("; ", rule.SpotByValue.Select(kv => $"{kv.Key}: {kv.Value}"))}");
                else
                    sb.AppendLine($"- {role}: varies, unexplained{(rule.NearestField is { } near && rule.Purity >= 0.6f ? $" (closest: {near}, {rule.Purity:P0})" : "")} → {string.Join("; ", rule.Spots.Select(s => $"{s.Label} (seeds {string.Join(",", s.Seeds)})"))}");
            }
            // Cues read the same from every seat once the seat's own events say "you".
            var merged = new Dictionary<string, (CueCandidate Cue, bool Own)>();
            foreach (var role in Roles)
            {
                if (!cues.TryGetValue((step.Key.Cls, line, role), out var list)) continue;
                foreach (var c in list)
                {
                    var own = c.Target == role;
                    var key = $"{c.Kind} {c.Name} on {(own ? "you" : c.Target ?? "")}";
                    if (!merged.TryGetValue(key, out var prev) || c.Seeds > prev.Cue.Seeds) merged[key] = (c, own);
                }
            }
            foreach (var (key, (c, own)) in merged.OrderByDescending(kv => kv.Value.Own).ThenByDescending(kv => kv.Value.Cue.Stable).ThenBy(kv => kv.Value.Cue.SecondsBefore).Take(8))
            {
                var target = own ? " on you" : c.Target is { } t ? $" on {t}" : "";
                sb.AppendLine($"  cue: {c.Kind} {c.Name}{target} {c.SecondsBefore:0.0}s before{(c.Stable ? "" : $" ({c.Seeds}/{c.OfSeeds} seeds)")}");
            }
        }
        if (texts is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine("## WTFDIG text (by mechanic, the page's strat; match to steps by hand, credit kept in sources)");
            foreach (var phase in texts.GroupBy(t => t.Phase))
            {
                sb.AppendLine();
                sb.AppendLine($"### {phase.Key}");
                foreach (var t in phase)
                {
                    sb.AppendLine($"- **{t.Mechanic}**{(t.Action is { Length: > 0 } ? $": {t.Action.Replace("\n", " / ")}" : "")}");
                    if (t.Description is { Length: > 0 }) sb.AppendLine($"  - {t.Description.Replace("\n", " / ")}");
                    foreach (var (role, text) in t.ByRole) sb.AppendLine($"  - {role}: {text.Replace("\n", " / ")}");
                }
            }
        }
        return sb.ToString();
    }
}
