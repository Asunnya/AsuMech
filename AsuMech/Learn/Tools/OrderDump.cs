using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AsuMech.Core.Game.Ai;

namespace AsuMech.Learn.Tools;

// One recorded order: what the strat asked of the player's slot in one run, flattened for JSON.
public sealed record DumpedOrder(string Cls, float Time, string Source, string? Method, string Role, int Seed,
    float IssuedAt, float? ArrivalTime, float X, float Z)
{
    public static DumpedOrder Of(GuidanceOrder o, int seed) => new(o.Key.StratClass, o.Key.Time, o.Key.Source, o.Key.Method,
        o.Role.ToString(), seed, o.IssuedAt, o.ArrivalTime, o.Target.X, o.Target.Z);

    public GuidanceKey Key => new(Cls, Time, Source, Method);
    public int Line => int.TryParse(Source[(Source.LastIndexOf(':') + 1)..], out var n) ? n : -1;
}

// One thing the sim sent during a run that a player could see: a cast bar, a resolve, a status, a
// tether, a head marker, a spawn. Actor and Target are party roles when the sim put a seat there.
public sealed record DumpedEvent(float Time, string Kind, string Name, string? Actor, string? Target, float? X, float? Z, string? Detail);

// What one seed rolled (the scenario's *State, stringified) and what it showed, the same for every role.
public sealed record DumpedRun(int Seed, Dictionary<string, string> State, List<DumpedEvent> Events);

public sealed record OrderDump(string Scenario, string Strat, int StratIndex, List<DumpedOrder> Orders, List<DumpedRun>? Runs = null)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public DumpedRun? Run(int seed) => Runs?.Find(r => r.Seed == seed);

    public string ToJson() => JsonSerializer.Serialize(this, Json);
    public static OrderDump Parse(string json) => JsonSerializer.Deserialize<OrderDump>(json, Json)!;
}
