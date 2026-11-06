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
public sealed record LessonToolbox(string PlanId, Dictionary<string, string> RoleMap, Dictionary<string, int> Steps, int? ExampleSeed = null,
    Dictionary<string, string>? Example = null, float? Residual = null);
