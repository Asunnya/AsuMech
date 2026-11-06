using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AsuMech.Network;

namespace AsuMech.Relay;

public sealed record RunReportInfo(string Name, long Bytes, DateTime ModifiedUtc);

// POST /reports lands here, one JSON file each: when, who (the first 8 characters of the relay id)
// and the report as sent. Like lesson feedback, the per-peer hourly cap is the real limit.
internal sealed class RunReportStore(string? dir, Func<DateTime> now)
{
    public const int PerPeerPerHour = 30;

    public enum Outcome { Stored, Invalid, TooMany, Failed }

    private readonly object gate = new();
    private readonly Dictionary<Guid, (DateTime Start, int Count)> recent = [];
    private static readonly System.Text.RegularExpressions.Regex NamePattern = new("^[0-9]{8}-[0-9]{6}_[a-z-]{3,20}_[0-9a-f]{8}(_[0-9]+)?\\.json$");

    public bool Enabled => dir != null;

    public (Outcome Outcome, string? Name) Add(Guid peer, string body)
    {
        if (dir is null) return (Outcome.Failed, null);
        JsonObject? report;
        try { report = JsonNode.Parse(body) as JsonObject; }
        catch (JsonException) { return (Outcome.Invalid, null); }
        if (report is null || !RunReportWire.IsValid(report)) return (Outcome.Invalid, null);
        var at = now();
        var who = peer.ToString("N")[..8];
        lock (gate)
        {
            var (start, count) = recent.TryGetValue(peer, out var r) && at - r.Start < TimeSpan.FromHours(1) ? r : (at, 0);
            if (count >= PerPeerPerHour) return (Outcome.TooMany, null);
            recent[peer] = (start, count + 1);
            try
            {
                Directory.CreateDirectory(dir);
                var stem = $"{at:yyyyMMdd-HHmmss}_{(string)report["kind"]!}_{who}";
                var name = stem + ".json";
                for (var n = 2; File.Exists(Path.Combine(dir, name)); n++) name = $"{stem}_{n}.json";
                var stored = new JsonObject { ["at"] = at, ["peer"] = who, ["report"] = report };
                File.WriteAllText(Path.Combine(dir, name), stored.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                return (Outcome.Stored, name);
            }
            catch (IOException) { return (Outcome.Failed, null); }
            catch (UnauthorizedAccessException) { return (Outcome.Failed, null); }
        }
    }

    public List<RunReportInfo> List()
    {
        if (dir is null || !Directory.Exists(dir)) return [];
        return new DirectoryInfo(dir).EnumerateFiles("*.json").Where(f => NamePattern.IsMatch(f.Name))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => new RunReportInfo(f.Name, f.Length, f.LastWriteTimeUtc)).ToList();
    }

    public string? Read(string name)
        => dir != null && NamePattern.IsMatch(name) && File.Exists(Path.Combine(dir, name)) ? File.ReadAllText(Path.Combine(dir, name)) : null;
}
