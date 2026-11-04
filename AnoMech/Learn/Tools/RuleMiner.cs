using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Learn.Tools;

// Where a step sends a role, named for a player.
public sealed record MinedSpot(string Label, float X, float Z, List<int> Seeds);

// How a step's spot for one role varies with the scenario's rolls: one spot for every seed, or
// the state fields whose value picks the spot, or nothing the recorded fields explain.
public sealed record MinedRule(string Cls, int Line, float Time, string Role, List<MinedSpot> Spots, List<string> Fields,
    Dictionary<string, string> SpotByValue, int SeedCount, string? NearestField = null, float Purity = 0f, string? Relation = null)
{
    public bool Fixed => Spots.Count == 1;
    public bool Explained => Fixed || Relation != null || Fields.Count > 0;

    // One line a player could follow, or null when the spot is unexplained.
    public string? Describe() => Fixed ? Spots[0].Label
        : Relation ?? (Fields.Count > 0 ? $"by {string.Join(" & ", Fields)}: {string.Join("; ", SpotByValue.Select(kv => $"{kv.Key} → {kv.Value}"))}" : null);
}

// Reads which rolls decide each step's spot. A field explains a step when seeds that share its value
// share the spot; among those, the fewest distinct values win, then a pair of fields. A field that
// names the role (a target list containing it) is also tried as "am I in it".
public static class RuleMiner
{
    public const float SameSpotYalms = 4.5f;
    // With enough seeds, a value seen once proves nothing; it has to recur.
    public const int MinSupportSeeds = 2;
    public const int SeedsForSupport = 6;
    public const int FewValues = 4;

    public static List<MinedRule> Mine(OrderDump dump, WaymarkLayout? waymarks = null)
    {
        var runs = (dump.Runs ?? []).ToDictionary(r => r.Seed, r => r.State);
        var rules = new List<MinedRule>();
        foreach (var step in dump.Orders.GroupBy(o => (o.Cls, o.Line)).OrderBy(g => g.Min(o => o.Time)))
        {
            var first = step.Min(o => o.Time);
            foreach (var role in step.Select(o => o.Role).Distinct())
            {
                var bySeed = step.Where(o => o.Role == role && Math.Abs(o.Time - first) < 0.01f).GroupBy(o => o.Seed)
                    .ToDictionary(g => g.Key, g => new Vector2(g.First().X, g.First().Z));
                if (bySeed.Count == 0) continue;
                var spots = Cluster(bySeed, waymarks);
                var relation = spots.Count == 1 ? null : Relate(bySeed, runs, waymarks);
                var (fields, mapping, nearest, purity) = spots.Count == 1 || relation != null ? ([], new Dictionary<string, string>(), null, 1f) : Explain(spots, bySeed.Keys, runs, role);
                rules.Add(new MinedRule(step.Key.Cls, step.Key.Line, first, role, spots, fields, mapping, bySeed.Count, nearest, purity, relation));
            }
        }
        return rules;
    }

    private static List<MinedSpot> Cluster(Dictionary<int, Vector2> bySeed, WaymarkLayout? waymarks)
    {
        var spots = new List<(Vector2 Centre, List<int> Seeds)>();
        foreach (var (seed, at) in bySeed.OrderBy(kv => kv.Key))
        {
            var near = spots.FindIndex(s => Vector2.Distance(s.Centre, at) <= SameSpotYalms);
            if (near < 0) spots.Add((at, [seed]));
            else spots[near].Seeds.Add(seed);
        }
        // Two clusters the player would call the same place read as one.
        var named = spots.Select(s => new MinedSpot(Describe(s.Centre, waymarks), s.Centre.X, s.Centre.Y, s.Seeds)).ToList();
        return named.GroupBy(s => s.Label).Select(g => g.Count() == 1 ? g.First()
            : new MinedSpot(g.Key, g.Average(s => s.X), g.Average(s => s.Z), g.SelectMany(s => s.Seeds).OrderBy(x => x).ToList())).ToList();
    }

    // A spot that follows a rolled vector (same place, opposite, a turn away) or sits still in the
    // frame of a rolled bearing reads as one sentence instead of a table of values.
    private static string? Relate(Dictionary<int, Vector2> bySeed, Dictionary<int, Dictionary<string, string>> runs, WaymarkLayout? waymarks)
    {
        var seeds = bySeed.Keys.Where(runs.ContainsKey).ToList();
        if (seeds.Count < 3) return null;
        foreach (var name in runs[seeds[0]].Keys)
        {
            var vectors = seeds.Select(s => ParseVector(runs[s].GetValueOrDefault(name))).ToList();
            if (vectors.All(v => v != null))
            {
                var offsets = seeds.Select((s, i) => bySeed[s] - vectors[i]!.Value).ToList();
                var mean = new Vector2(offsets.Average(o => o.X), offsets.Average(o => o.Y));
                if (offsets.All(o => Vector2.Distance(o, mean) <= RelationSlackYalms))
                    return mean.Length() < 2f ? $"at {name}" : $"{Compass(mean)} of {name}";
                var turns = seeds.Select((s, i) => Turn(vectors[i]!.Value, bySeed[s])).ToList();
                var radii = seeds.Select(s => bySeed[s].Length()).ToList();
                if (turns.All(t => t != null) && Spread(turns.Select(t => t!.Value)) <= RelationSlackDegrees && radii.Max() - radii.Min() <= RelationSlackYalms * 2)
                    return $"{TurnName(turns.Average(t => t!.Value))} {name}, {radii.Average():0}m out";
                continue;
            }
            var bearings = seeds.Select(s => ParseBearing(runs[s].GetValueOrDefault(name))).ToList();
            if (bearings.All(b => b != null) && bearings.Distinct().Count() > 1)
            {
                var inFrame = seeds.Select((s, i) => Rotate(bySeed[s], -bearings[i]!.Value)).ToList();
                var centre = new Vector2(inFrame.Average(p => p.X), inFrame.Average(p => p.Y));
                if (inFrame.All(p => Vector2.Distance(p, centre) <= RelationSlackYalms))
                    return $"with {name} as north: {Compass(centre)}";
            }
        }
        return null;
    }

    private const float RelationSlackYalms = 2f;
    private const float RelationSlackDegrees = 20f;

    private static Vector2? ParseVector(string? text)
    {
        if (text is not { Length: > 4 } || text[0] != '(' || text[^1] != ')') return null;
        var parts = text[1..^1].Split(',');
        return parts.Length == 2 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)
                                 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z) ? new Vector2(x, z) : null;
    }

    // Compass degrees on a 45° grid, as the scenarios roll bearings.
    private static float? ParseBearing(string? text)
        => float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var b) && b >= 0f && b < 360f && MathF.Abs(b % 45f) < 0.5f ? b : null;

    private static float? Turn(Vector2 from, Vector2 to)
    {
        if (from.Length() < 2f || to.Length() < 2f) return null;
        var a = MathF.Atan2(to.X, -to.Y) - MathF.Atan2(from.X, -from.Y);
        return ((a * 180f / MathF.PI) % 360f + 540f) % 360f - 180f;
    }

    private static float Spread(IEnumerable<float> degrees)
    {
        var list = degrees.ToList();
        var mean = list.Average();
        return list.Max(d => MathF.Abs(d - mean));
    }

    private static string TurnName(float degrees) => degrees switch
    {
        > 160f or < -160f => "opposite",
        > 70f and < 110f => "a quarter turn clockwise of",
        < -70f and > -110f => "a quarter turn counterclockwise of",
        > 25f and < 65f => "an eighth turn clockwise of",
        < -25f and > -65f => "an eighth turn counterclockwise of",
        _ => $"{degrees:0}° from",
    };

    private static Vector2 Rotate(Vector2 p, float degrees)
    {
        var (sin, cos) = MathF.SinCos(degrees * MathF.PI / 180f);
        return new Vector2(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
    }

    // Features: every field as is, "<field> has <role>" for fields listing roles, and
    // "<field> has <item>" for every item a list field ever holds (a plume spot, a target).
    private static (List<string> Fields, Dictionary<string, string> Mapping, string? Nearest, float Purity) Explain(List<MinedSpot> spots, IEnumerable<int> seeds, Dictionary<int, Dictionary<string, string>> runs, string role)
    {
        var seedList = seeds.Where(runs.ContainsKey).ToList();
        if (seedList.Count < 2) return ([], new(), null, 0f);
        var spotOf = seedList.ToDictionary(s => s, s => spots.FindIndex(sp => sp.Seeds.Contains(s)));
        var features = new Dictionary<string, Func<int, string?>>();
        foreach (var name in runs[seedList[0]].Keys)
        {
            features[name] = seed => runs[seed].GetValueOrDefault(name);
            if (runs.Values.Any(r => r.GetValueOrDefault(name)?.Contains(role, StringComparison.Ordinal) == true))
                features[$"{name} has {role}"] = seed => runs[seed].GetValueOrDefault(name)?.Contains(role, StringComparison.Ordinal) == true ? "yes" : "no";
            var items = runs.Values.Select(r => r.GetValueOrDefault(name)).Where(v => v is { Length: > 1 } && v[0] == '[').SelectMany(Items).Distinct().ToList();
            if (items.Count > 1 && items.Count <= 16)
                foreach (var item in items)
                    features[$"{name} has {item}"] = seed => Items(runs[seed].GetValueOrDefault(name) ?? "").Contains(item) ? "yes" : "no";
        }
        var minSupport = seedList.Count >= SeedsForSupport ? MinSupportSeeds : 1;
        bool Explains(IEnumerable<string> names, out Dictionary<string, int> map)
        {
            map = new Dictionary<string, int>();
            foreach (var seed in seedList)
            {
                var key = string.Join(" & ", names.Select(n => features[n](seed) ?? "?"));
                if (map.TryGetValue(key, out var spot) && spot != spotOf[seed]) return false;
                map[key] = spotOf[seed];
            }
            // A feature as varied as the seeds themselves explains nothing; one with few values (a
            // bearing, a side) may, even when a value showed up once; otherwise every value must recur.
            if (map.Count > seedList.Count / 2) return false;
            var support = new Dictionary<string, int>();
            foreach (var seed in seedList)
            {
                var key = string.Join(" & ", names.Select(n => features[n](seed) ?? "?"));
                support[key] = support.GetValueOrDefault(key) + 1;
            }
            return map.Count <= FewValues || support.Values.All(n => n >= minSupport);
        }
        // Fewest values first; on a tie, the feature about the role itself, then the shortest name.
        var single = features.Keys.Where(n => Explains([n], out _)).OrderBy(n => Values(n)).ThenByDescending(n => n.EndsWith(" has " + role, StringComparison.Ordinal)).ThenBy(n => n.Length).ToList();
        if (single.Count > 0 && Explains([single[0]], out var one))
            return ([single[0]], Mapping(one, spots), null, 1f);
        var names = features.Keys.ToList();
        var best = (Names: (List<string>?)null, Values: int.MaxValue, Map: (Dictionary<string, int>?)null);
        for (var i = 0; i < names.Count; i++)
        for (var j = i + 1; j < names.Count; j++)
            if (Explains([names[i], names[j]], out var map) && map.Count < best.Values)
                best = ([names[i], names[j]], map.Count, map);
        if (best.Names != null) return (best.Names, Mapping(best.Map!, spots), null, 1f);
        // Nothing decides the spot outright; name the field that comes closest, as a lead for the
        // author (a list of hazard spots usually means a geometric rule).
        var nearest = names.Where(n => Values(n) < seedList.Count).Select(n => (Name: n, Purity: Purity(n))).OrderByDescending(x => x.Purity).ThenBy(x => Values(x.Name)).FirstOrDefault();
        return ([], new(), nearest.Name, nearest.Purity);

        int Values(string name) => seedList.Select(s => features[name](s)).Distinct().Count();
        float Purity(string name) => seedList.GroupBy(s => features[name](s)).Sum(g => g.GroupBy(s => spotOf[s]).Max(c => c.Count())) / (float)seedList.Count;
    }

    // "[(a),(b),(c)]" or "[X,Y]" -> its items; vectors keep their own commas.
    private static List<string> Items(string list)
    {
        var inner = list.Length >= 2 && list[0] == '[' ? list[1..^1] : list;
        var items = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < inner.Length; i++)
        {
            if (inner[i] is '(' or '{') depth++;
            else if (inner[i] is ')' or '}') depth--;
            else if (inner[i] == ',' && depth == 0) { items.Add(inner[start..i]); start = i + 1; }
        }
        if (inner.Length > 0) items.Add(inner[start..]);
        return items;
    }

    private static Dictionary<string, string> Mapping(Dictionary<string, int> map, List<MinedSpot> spots)
        => map.ToDictionary(kv => kv.Key, kv => spots[kv.Value].Label);

    // "centre", "N wall", "SE 12m", "at B", "near 3"; the arena centre is the strat's origin.
    public static string Describe(Vector2 at, WaymarkLayout? waymarks)
    {
        if (waymarks != null)
        {
            var mark = waymarks.Markers.Select(m => (m.Slot, Distance: Vector2.Distance(at, new Vector2(m.Offset.X, m.Offset.Z)))).OrderBy(m => m.Distance).FirstOrDefault();
            if (mark.Distance <= 2f) return $"at {Label(mark.Slot)}";
            if (mark.Distance <= 4f) return $"near {Label(mark.Slot)} ({Compass(at)})";
        }
        return Compass(at);
    }

    private static string Label(WaymarkSlot slot) => slot switch
    {
        WaymarkSlot.One => "1", WaymarkSlot.Two => "2", WaymarkSlot.Three => "3", WaymarkSlot.Four => "4", _ => slot.ToString(),
    };

    public static string Compass(Vector2 at)
    {
        var r = at.Length();
        if (r < 2f) return "centre";
        var bearing = (MathF.Atan2(at.X, -at.Y) * 180f / MathF.PI + 360f) % 360f;
        string[] names = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        var dir = names[(int)MathF.Round(bearing / 45f) % 8];
        return r >= 15f ? $"{dir} wall" : $"{dir} {r:0}m";
    }
}
