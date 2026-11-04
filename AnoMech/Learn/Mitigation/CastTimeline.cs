using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnoMech.Learn.Mitigation;

// One boss action in a scenario's timeline: when its cast bar starts and when it resolves (equal
// for an instant). Occurrence counts same-named actions in time order.
public sealed record CastEvent(uint ActionId, string Name, float Start, float Resolve, int Occurrence);

// The deterministic cast timeline of a scenario, recorded headless and embedded under
// Learn/Mitigation/Timelines/<ScenarioType>.json. Targets vary per roll; cast times don't.
public sealed record CastTimeline(string Scenario, List<CastEvent> Casts)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public CastEvent? Find(string name, int occurrence)
        => Casts.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) && c.Occurrence == occurrence);

    public IEnumerable<string> Names => Casts.Select(c => c.Name).Distinct();

    public string ToJson() => JsonSerializer.Serialize(this, Json);
    public static CastTimeline Parse(string json) => JsonSerializer.Deserialize<CastTimeline>(json, Json)!;

    // From the test harness's packet timeline CSV (SimTimeline): `cast` rows start a bar that
    // resolves after its length; `ability` rows are instants unless they are a cast's own resolve.
    public static CastTimeline FromTimelineCsv(string scenario, string csv)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0].TrimEnd('\r').Split(',');
        int Col(string name) => Array.IndexOf(header, name);
        var (tCol, kindCol, idCol, nameCol, detailCol) = (Col("t"), Col("kind"), Col("id"), Col("name"), Col("detail"));
        var raw = new List<(uint Id, string Name, float Start, float Resolve)>();
        foreach (var line in lines.Skip(1))
        {
            var cells = SplitCsv(line.TrimEnd('\r'));
            if (cells.Count <= detailCol) continue;
            var kind = cells[kindCol];
            if (kind is not ("cast" or "ability")) continue;
            if (!uint.TryParse(cells[idCol], out var id) || !float.TryParse(cells[tCol], NumberStyles.Float, CultureInfo.InvariantCulture, out var t)) continue;
            var name = cells[nameCol];
            if (kind == "cast")
            {
                var m = System.Text.RegularExpressions.Regex.Match(cells[detailCol], @"cast=([0-9.]+)s");
                var length = m.Success ? float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0f;
                raw.Add((id, name, t, t + length));
            }
            else raw.Add((id, name, t, t));
        }
        // Several casters casting together (feathers, sisters) count once; a cast's own resolve isn't a second event.
        var events = new List<(uint Id, string Name, float Start, float Resolve)>();
        foreach (var e in raw.OrderBy(e => e.Start))
        {
            if (events.Any(x => x.Id == e.Id && Math.Abs(x.Start - e.Start) < 0.5f)) continue;
            if (e.Start == e.Resolve && events.Any(x => x.Id == e.Id && x.Resolve != x.Start && Math.Abs(x.Resolve - e.Resolve) < 0.6f)) continue;
            events.Add(e);
        }
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var casts = events.Select(e =>
        {
            seen[e.Name] = seen.GetValueOrDefault(e.Name) + 1;
            return new CastEvent(e.Id, e.Name, e.Start, e.Resolve, seen[e.Name]);
        }).ToList();
        return new CastTimeline(scenario, casts);
    }

    internal static List<string> SplitCsv(string line)
    {
        var cells = new List<string>();
        var sb = new System.Text.StringBuilder();
        var quoted = false;
        foreach (var c in line)
        {
            if (c == '"') quoted = !quoted;
            else if (c == ',' && !quoted) { cells.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        cells.Add(sb.ToString());
        return cells;
    }
}
