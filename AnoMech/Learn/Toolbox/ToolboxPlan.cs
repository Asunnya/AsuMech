using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnoMech.Learn.Toolbox;

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
}

// The lesson's link to a plan: which slot each role is, and which toolbox step each lesson step shows.
public sealed record LessonToolbox(string PlanId, Dictionary<string, string> RoleMap, Dictionary<string, int> Steps);
