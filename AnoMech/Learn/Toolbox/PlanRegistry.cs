using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnoMech.Learn.Toolbox;

// Where a raidplan lives: the host and the id its link carries.
public enum RaidplanHost { Toolbox, RaidplanIo, Stgy, Xivplan, Unknown }

public sealed record PlanRef(RaidplanHost Host, string Id, string Url)
{
    // "https://ff14.toolboxgaming.space/?id=122994122467471&preview=1", "https://raidplan.io/plan/abc123",
    // a bare toolbox id, pasted "[stgy:…]" codes (one board per code, in order), or an
    // ffxivstrats.io strategy ("https://ffxivstrats.io/fight/dt-ulti-umad?strategy=883", id "dt-ulti-umad/883").
    public static PlanRef Parse(string text)
    {
        text = text.Trim();
        if (text.Contains("[stgy:", StringComparison.Ordinal))
            return new PlanRef(RaidplanHost.Stgy, "stgy-" + Crc(text), text);
        if (XivplanProvider.Looks(text)) return new PlanRef(RaidplanHost.Xivplan, "xivplan-" + Crc(text), text);
        if (text.All(char.IsDigit) && text.Length >= 6) return new PlanRef(RaidplanHost.Toolbox, text, $"https://ff14.toolboxgaming.space/?id={text}");
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)) return new PlanRef(RaidplanHost.Unknown, text, text);
        if (uri.Host.EndsWith("toolboxgaming.space", StringComparison.OrdinalIgnoreCase))
        {
            var id = uri.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2)).FirstOrDefault(p => p[0] == "id" && p.Length == 2)?[1];
            return id is null ? new PlanRef(RaidplanHost.Unknown, text, text) : new PlanRef(RaidplanHost.Toolbox, id, text);
        }
        if (uri.Host.EndsWith("ffxivstrats.io", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            var strategy = uri.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2)).FirstOrDefault(p => p[0] == "strategy" && p.Length == 2)?[1]
                           ?? (uri.Fragment.Length > 1 ? uri.Fragment[1..] : null);
            if (parts.Length >= 2 && parts[0] == "strategy") return new PlanRef(RaidplanHost.Stgy, $"strategy/{parts[1]}", text);
            if (parts.Length >= 2 && parts[0] == "fight" && strategy != null) return new PlanRef(RaidplanHost.Stgy, $"{parts[1]}/{strategy}", text);
            if (parts.Length >= 2 && parts[0] == "fight") return new PlanRef(RaidplanHost.Stgy, parts[1], text);
        }
        if (uri.Host.EndsWith("raidplan.io", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            if (parts.Length >= 2 && parts[0] == "plan") return new PlanRef(RaidplanHost.RaidplanIo, parts[1], text);
        }
        return new PlanRef(RaidplanHost.Unknown, text, text);
    }

    private static string Crc(string text)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in System.Text.Encoding.UTF8.GetBytes(text))
        {
            crc ^= b;
            for (var i = 0; i < 8; i++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return (~crc).ToString("x8");
    }
}

// One registered plan: the link as the guide gives it, the zone's waymark preset it was drawn on,
// who made it, and which strat it belongs to when the scenario has several.
public sealed record PlanEntry(string Url, string? Layout = null, string? Credits = null, string? Strat = null)
{
    [JsonIgnore] public PlanRef Ref => PlanRef.Parse(Url);
}

// Learn/Toolbox/plans.json: scenario type name -> its raidplans. The authoring chain reads it so a
// family's lessons come out of one command.
public static class PlanRegistry
{
    private const string Resource = "AnoMech.Learn.Toolbox.plans.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<PlanEntry>>> Embedded = new(() =>
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource);
        if (stream is null) return new Dictionary<string, IReadOnlyList<PlanEntry>>();
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    });

    public static IReadOnlyDictionary<string, IReadOnlyList<PlanEntry>> All => Embedded.Value;

    public static IReadOnlyDictionary<string, IReadOnlyList<PlanEntry>> Parse(string json)
        => (JsonSerializer.Deserialize<Dictionary<string, List<PlanEntry>>>(json, Json) ?? new())
            .ToDictionary(kv => kv.Key, kv => (IReadOnlyList<PlanEntry>)kv.Value);

    public static string ToJson(IReadOnlyDictionary<string, IReadOnlyList<PlanEntry>> registry) => JsonSerializer.Serialize(registry, Json);

    public static IReadOnlyList<PlanEntry> For(string scenarioType) => All.TryGetValue(scenarioType, out var list) ? list : [];

    // The entry for a strat: one named after it, else the family-wide one with no strat named.
    public static PlanEntry? For(string scenarioType, string strat) => AllFor(scenarioType, strat).FirstOrDefault();

    // Every entry for a strat (a guide split in parts): the ones named after it, else the unnamed ones.
    public static IReadOnlyList<PlanEntry> AllFor(string scenarioType, string strat)
    {
        var named = For(scenarioType).Where(e => e.Strat == strat).ToList();
        return named.Count > 0 ? named : For(scenarioType).Where(e => e.Strat is null).ToList();
    }
}
