using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsuMech.Learn.Toolbox;

// A toolbox object with its centre in scenario-local yalms (X east, Z south), sized in yalms.
public sealed record ToolboxObject(
    string Kind, string Key, string Label, float X, float Z, float Width, float Height, float Rotation,
    string Shape, string? Color = null, string? Job = null, List<string>? Buffs = null);

public sealed record ToolboxStep(int Index, string Label, string Note, List<ToolboxObject> Objects);

// A ff14.toolboxgaming.space plan, reduced to what the viewer draws and already calibrated to the
// zone's waymark layout.
public sealed record ToolboxPlan(string Id, string Label, string Author, string Layout, float Scale, List<ToolboxStep> Steps)
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    // Texts the author put on most steps (a "Created by" credit, a legend) are a footer, not what
    // a step says: they neither name it nor go under its picture.
    [JsonIgnore]
    public IReadOnlySet<string> Footer => footer ??= Steps.Count < 3 ? new HashSet<string>() : Steps
        .SelectMany(st => st.Objects.Where(o => o.Kind == "text").Select(o => o.Label.Trim()).Distinct())
        .GroupBy(t => t).Where(g => g.Count() * 2 > Steps.Count).Select(g => g.Key).ToHashSet();
    private HashSet<string>? footer;

    // The step's first one-line text that isn't the footer, else its imported label.
    public string Title(ToolboxStep step)
        => step.Objects.Where(o => o.Kind == "text" && !Footer.Contains(o.Label.Trim()))
               .Select(o => o.Label.Trim().Split('\n')[0].Trim()).FirstOrDefault(l => l.Length is > 0 and <= MaxTitleChars)
           ?? step.Label;

    private const int MaxTitleChars = 48;

    public string ToJson() => JsonSerializer.Serialize(this, Json);
    public static ToolboxPlan Parse(string json) => JsonSerializer.Deserialize<ToolboxPlan>(json, Json)!;

    // Several plans for one phase (a guide split in parts) read as one, steps in the order given;
    // the id joins theirs with '+'.
    public static ToolboxPlan Merge(IReadOnlyList<ToolboxPlan> plans)
    {
        if (plans.Count == 1) return plans[0];
        var steps = new List<ToolboxStep>();
        foreach (var plan in plans)
            foreach (var step in plan.Steps) steps.Add(step with { Index = steps.Count });
        return new ToolboxPlan(string.Join("+", plans.Select(p => p.Id)), string.Join(" / ", plans.Select(p => p.Label).Where(l => l.Length > 0)),
            string.Join(" / ", plans.Select(p => p.Author).Where(a => a.Length > 0).Distinct()), plans[0].Layout, plans[0].Scale, steps);
    }
}

// The lesson's link to a plan: which slot each role is, and which toolbox step each lesson step shows.
// Example: the rolls the plan draws, as the scenario's *StateOverrides property names and values
// ("SuparnaSongBearing": "270", "SatinPlumesFirst": "[(16,-4),(-4,16),(-12,-12),(-16,4)]"), pinned
// for a replay on top of ExampleSeed.
// Residual: the oracle's mean distance (m) between the plan and the strat over the aligned steps when
// the lesson was authored; CI refuses a lesson that drifts past it by more than a metre.
// A Steps key may carry rolls after '|' ("Ai@15.74|AxeTrophyFirst=false"): that page wins when the
// run rolled them. Pages: more of the plan to page through, though no step jumps to them.
public sealed record LessonToolbox(string PlanId, Dictionary<string, string> RoleMap, Dictionary<string, int> Steps, int? ExampleSeed = null,
    Dictionary<string, string>? Example = null, float? Residual = null, List<int>? Pages = null)
{
    public IEnumerable<int> AllPages => Steps.Values.Concat(Pages ?? []);

    public int? PageFor(string stepId, IReadOnlyDictionary<string, string> rolls)
    {
        int? plain = null;
        foreach (var (key, page) in Steps)
        {
            var bar = key.IndexOf('|');
            if (bar < 0) { if (key == stepId) plain = page; continue; }
            if (key[..bar] != stepId) continue;
            var conditions = key[(bar + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries);
            if (conditions.All(c => c.Split('=', 2) is [var name, var value] && rolls.TryGetValue(name.Trim(), out var rolled)
                    && string.Equals(rolled, value.Trim(), StringComparison.OrdinalIgnoreCase)))
                return page;
        }
        return plain;
    }
}
