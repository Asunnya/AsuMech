using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Learn.Tools;

namespace AnoMech.Learn.Toolbox;

// One aligned toolbox step checked against the strat: how far the plan's players stand from where
// the strat sends the same roles, on the example seed.
public sealed record OracleStep(int ToolboxStep, string Label, string StepId, float Residual, float Tolerance, Dictionary<string, float> ByRole)
{
    // Toolbox icons are placed by hand; a drawing agrees when every role is within the step's
    // tolerance plus the slack a dragged icon gets.
    public const float SlackYalms = 1.5f;
    public bool Agrees => ByRole.Values.All(d => d <= Tolerance + SlackYalms);
    public IEnumerable<KeyValuePair<string, float>> RolesOff => ByRole.Where(kv => kv.Value > Tolerance + SlackYalms);
}

public sealed record OracleReport(int ExampleSeed, float TotalResidual, List<OracleStep> Steps, Dictionary<int, float> ResidualBySeed)
{
    public IEnumerable<OracleStep> Disagreements => Steps.Where(s => !s.Agrees);
}

// A toolbox draws one roll. The seed whose strat targets sit closest to the plan over every aligned
// step is the example seed; a step still far from the plan there is a strat bug, an alignment slip,
// or a variant the plan doesn't play.
public static class ToolboxOracle
{
    public static OracleReport Check(ToolboxPlan plan, Lesson lesson, LessonToolbox link, OrderDump dump)
    {
        var seeds = dump.Orders.Select(o => o.Seed).Distinct().OrderBy(s => s).ToList();
        // Two calls at one time (different lines) share an id; the first keeps it.
        var stepsById = lesson.Steps.GroupBy(s => MasteryStore.StepId(s.Key)).ToDictionary(g => g.Key, g => g.First());
        var aligned = link.Steps.Where(kv => stepsById.ContainsKey(kv.Key) && kv.Value >= 0 && kv.Value < plan.Steps.Count)
            .Select(kv => (StepId: kv.Key, Step: stepsById[kv.Key], Toolbox: plan.Steps[kv.Value])).ToList();
        var bySeed = seeds.ToDictionary(seed => seed, seed => aligned.Select(a => Residual(a.Toolbox, a.Step, link, dump, seed)).ToList());
        var totals = bySeed.ToDictionary(kv => kv.Key, kv => kv.Value.Sum(r => r.Residual));
        var example = totals.Count == 0 ? seeds.FirstOrDefault() : totals.MinBy(kv => kv.Value).Key;
        var steps = bySeed.TryGetValue(example, out var picked) ? picked : [];
        return new OracleReport(example, totals.GetValueOrDefault(example), steps, totals);
    }

    private static OracleStep Residual(ToolboxStep toolbox, LessonStep step, LessonToolbox link, OrderDump dump, int seed)
    {
        var orders = dump.Orders.Where(o => o.Seed == seed && step.Key.Matches(o.Key)).ToList();
        var first = orders.Count == 0 ? 0f : orders.Min(o => o.Time);
        var targets = orders.Where(o => Math.Abs(o.Time - first) < 0.01f).GroupBy(o => o.Role).ToDictionary(g => g.Key, g => new Vector2(g.First().X, g.First().Z));
        // The lesson's map is role -> toolbox slot.
        var roleBySlot = link.RoleMap.ToDictionary(kv => kv.Value, kv => kv.Key);
        var byRole = new Dictionary<string, float>();
        foreach (var o in toolbox.Objects.Where(o => o.Kind == "player"))
            if (roleBySlot.TryGetValue(o.Key, out var role) && targets.TryGetValue(role, out var target))
                byRole[role] = Vector2.Distance(new Vector2(o.X, o.Z), target);
        var residual = byRole.Count == 0 ? 0f : byRole.Values.Average();
        return new OracleStep(toolbox.Index, toolbox.Label, MasteryStore.StepId(step.Key), residual, step.Tolerance, byRole);
    }
}
