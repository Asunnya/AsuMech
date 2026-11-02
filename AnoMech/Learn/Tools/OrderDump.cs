using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnoMech.Core.Game.Ai;

namespace AnoMech.Learn.Tools;

// One recorded order: what the strat asked of the player's slot in one run, flattened for JSON.
public sealed record DumpedOrder(string Cls, float Time, string Source, string? Method, string Role, int Seed,
    float IssuedAt, float? ArrivalTime, float X, float Z)
{
    public static DumpedOrder Of(GuidanceOrder o, int seed) => new(o.Key.StratClass, o.Key.Time, o.Key.Source, o.Key.Method,
        o.Role.ToString(), seed, o.IssuedAt, o.ArrivalTime, o.Target.X, o.Target.Z);

    public GuidanceKey Key => new(Cls, Time, Source, Method);
    public int Line => int.TryParse(Source[(Source.LastIndexOf(':') + 1)..], out var n) ? n : -1;
}

public sealed record OrderDump(string Scenario, string Strat, int StratIndex, List<DumpedOrder> Orders)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string ToJson() => JsonSerializer.Serialize(this, Json);
    public static OrderDump Parse(string json) => JsonSerializer.Deserialize<OrderDump>(json, Json)!;
}
