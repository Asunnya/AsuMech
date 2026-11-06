using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Learn.Tools;

namespace AnoMech.Learn.Toolbox;

public sealed record ImportReport(int StepsInFile, int StepsRecovered, bool Truncated, float Scale, float Residual,
    Dictionary<string, string> RoleMap, List<string> Warnings);

// Turns the raw toolbox JSON into a ToolboxPlan: tolerant of a truncated file, calibrated by the
// waymarks, with slots mapped to party roles by their job labels.
public static class ToolboxImport
{
    private static readonly string[] Slots = ["tank1", "tank2", "healer1", "healer2", "dps1", "dps2", "dps3", "dps4"];
    private static readonly Dictionary<string, WaymarkSlot> WaymarkKeys = new()
    {
        ["waymarkA"] = WaymarkSlot.A, ["waymarkB"] = WaymarkSlot.B, ["waymarkC"] = WaymarkSlot.C, ["waymarkD"] = WaymarkSlot.D,
        ["waymark1"] = WaymarkSlot.One, ["waymark2"] = WaymarkSlot.Two, ["waymark3"] = WaymarkSlot.Three, ["waymark4"] = WaymarkSlot.Four,
    };
    private static readonly Dictionary<string, string> JobCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PLD"] = "tank", ["WAR"] = "tank", ["DRK"] = "tank", ["GNB"] = "tank",
        ["WHM"] = "regen", ["AST"] = "regen", ["SCH"] = "shield", ["SGE"] = "shield",
        ["MNK"] = "melee", ["DRG"] = "melee", ["NIN"] = "melee", ["SAM"] = "melee", ["RPR"] = "melee", ["VPR"] = "melee",
        ["BRD"] = "phys", ["MCH"] = "phys", ["DNC"] = "phys",
        ["BLM"] = "caster", ["SMN"] = "caster", ["RDM"] = "caster", ["PCT"] = "caster",
    };

    // A plan cut short still has whole steps before the cut; NULs pad the broken ones on the server.
    public static (JsonDocument Document, bool Truncated) ParseTolerant(byte[] raw)
    {
        var end = raw.Length;
        while (end > 0 && raw[end - 1] == 0) end--;
        var text = Encoding.UTF8.GetString(raw, 0, end);
        try { return (JsonDocument.Parse(text), end != raw.Length); }
        catch (JsonException) { }

        var stepsMatch = System.Text.RegularExpressions.Regex.Match(text, "\"steps\"\\s*:\\s*\\[");
        if (!stepsMatch.Success) throw new InvalidOperationException("not a toolbox plan");
        var sb = new StringBuilder("{\"steps\":[");
        var i = stepsMatch.Index + stepsMatch.Length;
        var first = true;
        while (i < text.Length)
        {
            while (i < text.Length && (text[i] == ',' || char.IsWhiteSpace(text[i]))) i++;
            if (i >= text.Length || text[i] != '{') break;
            var close = MatchBrace(text, i);
            if (close < 0) break;
            var step = text[i..(close + 1)];
            try { JsonDocument.Parse(step).Dispose(); } catch (JsonException) { break; }
            if (!first) sb.Append(',');
            sb.Append(step);
            first = false;
            i = close + 1;
        }
        sb.Append("]}");
        return (JsonDocument.Parse(sb.ToString()), true);
    }

    private static int MatchBrace(string text, int open)
    {
        var depth = 0;
        var inString = false;
        for (var i = open; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
                continue;
            }
            if (c == '"') inString = true;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return i;
        }
        return -1;
    }

    // Past this, the plan's A-D are not the layout's marks at all (another tier's, or the author's
    // own), and the arena object sizes the plan instead.
    public const float BadFitResidual = 2f;

    public static (ToolboxPlan Plan, ImportReport Report) Convert(byte[] raw, string id, WaymarkLayout layout, float? arenaYalms = null)
    {
        var (doc, truncated) = ParseTolerant(raw);
        var root = doc.RootElement;
        var steps = root.GetProperty("steps");
        var count = steps.GetArrayLength();
        var warnings = new List<string>();
        if (count == 0) throw new InvalidOperationException("no steps");

        var instance = steps[0].GetProperty("instance");
        var (scale, offset, residual) = Calibrate(instance, layout);
        if (residual > BadFitResidual && arenaYalms is { } yalms && instance.TryGetProperty("arena", out var arena) && Size(arena).W > 0f)
        {
            // The arena object is the floor: its width is the zone's, its centre the origin.
            var (w, h) = Size(arena);
            var p = arena.GetProperty("position");
            scale = yalms / w;
            offset = -new Vector2(Num(p, "x") + w / 2f, Num(p, "y") + h / 2f) * scale;
            warnings.Add($"the plan's A-D fit no layout (residual {residual:F2} m): scaled by its arena object, {w:0.#} units for {yalms:0.#} yalms");
            residual = 0f;
        }
        else if (residual > 0.3f) warnings.Add($"waymark calibration residual {residual:F2} m");
        var roleMap = RoleMap(steps[0].GetProperty("instance"), warnings);

        var plan = new ToolboxPlan(id, Str(root, "label"), Str(root, "author"), layout.Name, scale,
            steps.EnumerateArray().Select((s, i) => ConvertStep(s, i, scale, offset)).ToList());
        var reportedCount = root.TryGetProperty("dataSize", out _) ? count : count;
        return (plan, new ImportReport(reportedCount, count, truncated, scale, residual, roleMap, warnings));
    }

    // Uniform scale and translation (no rotation) fitting the toolbox's A-D onto the layout. The
    // numbered marks are left out: plans often place them by their own convention.
    public static (float Scale, Vector2 Offset, float Residual) Calibrate(JsonElement instance, WaymarkLayout layout)
        => Calibrate(WaymarkKeys.Where(kv => instance.TryGetProperty(kv.Key, out _)).Select(kv => (kv.Value, Centre(instance.GetProperty(kv.Key)))), layout);

    // The same fit for any provider's waymark positions in its own canvas units.
    public static (float Scale, Vector2 Offset, float Residual) Calibrate(IEnumerable<(WaymarkSlot Slot, Vector2 At)> marks, WaymarkLayout layout)
    {
        var pairs = new List<(Vector2 Tb, Vector2 Game)>();
        foreach (var (slot, at) in marks)
        {
            if (slot > WaymarkSlot.D) continue;
            var game = layout.Markers.FirstOrDefault(m => m.Slot == slot);
            if (game is null) continue;
            pairs.Add((at, new Vector2(game.Offset.X, game.Offset.Z)));
        }
        if (pairs.Count < 2) throw new InvalidOperationException("fewer than two waymarks to calibrate with");
        var tbMean = pairs.Aggregate(Vector2.Zero, (a, p) => a + p.Tb) / pairs.Count;
        var gameMean = pairs.Aggregate(Vector2.Zero, (a, p) => a + p.Game) / pairs.Count;
        var num = pairs.Sum(p => Vector2.Dot(p.Tb - tbMean, p.Game - gameMean));
        var den = pairs.Sum(p => (p.Tb - tbMean).LengthSquared());
        var scale = num / den;
        var offset = gameMean - tbMean * scale;
        var residual = MathF.Sqrt(pairs.Sum(p => ((p.Tb * scale + offset) - p.Game).LengthSquared()) / pairs.Count);
        return (scale, offset, residual);
    }

    // Slot -> PartyRole by icon job: tanks by slot order, healers by regen/shield, melee in order,
    // phys ranged and caster by job, leftovers into the free DPS seats.
    public static Dictionary<string, string> RoleMap(JsonElement instance, List<string>? warnings = null)
    {
        var map = new Dictionary<string, string>();
        var melee = new Queue<string>([nameof(PartyRole.MeleeDpsA), nameof(PartyRole.MeleeDpsB)]);
        foreach (var slot in Slots)
        {
            if (!instance.TryGetProperty(slot, out var obj)) continue;
            var job = Str(obj, "label");
            var kind = JobCategory.GetValueOrDefault(job);
            string? role = slot switch
            {
                "tank1" => nameof(PartyRole.MainTank),
                "tank2" => nameof(PartyRole.OffTank),
                _ when kind == "regen" || (kind == null && slot == "healer1") => nameof(PartyRole.RegenHealer),
                _ when kind == "shield" || (kind == null && slot == "healer2") => nameof(PartyRole.ShieldHealer),
                _ when kind == "melee" && melee.Count > 0 => melee.Dequeue(),
                _ when kind == "phys" && !map.ContainsValue(nameof(PartyRole.PhysRangedDps)) => nameof(PartyRole.PhysRangedDps),
                _ when kind == "caster" && !map.ContainsValue(nameof(PartyRole.CasterDps)) => nameof(PartyRole.CasterDps),
                _ => null,
            };
            if (role != null) map[slot] = role;
            else if (slot.StartsWith("dps")) warnings?.Add($"{slot} ({job}) has no free seat of its category");
        }
        var free = new Queue<string>(new[] { nameof(PartyRole.MeleeDpsA), nameof(PartyRole.MeleeDpsB), nameof(PartyRole.PhysRangedDps), nameof(PartyRole.CasterDps) }.Where(r => !map.ContainsValue(r)));
        foreach (var slot in Slots.Skip(4))
            if (instance.TryGetProperty(slot, out _) && !map.ContainsKey(slot) && free.Count > 0)
                map[slot] = free.Dequeue();
        return map;
    }

    // Two slots naming one seat (two generic healer icons both read as the regen healer) take the
    // group's seats in slot order instead, so the lesson's role map can be inverted.
    public static Dictionary<string, string> Dedupe(Dictionary<string, string> roleMap)
    {
        string[][] groups = [[nameof(PartyRole.MainTank), nameof(PartyRole.OffTank)], [nameof(PartyRole.RegenHealer), nameof(PartyRole.ShieldHealer)],
            [nameof(PartyRole.MeleeDpsA), nameof(PartyRole.MeleeDpsB), nameof(PartyRole.PhysRangedDps), nameof(PartyRole.CasterDps)]];
        var result = new Dictionary<string, string>(roleMap);
        foreach (var group in groups)
        {
            var slots = result.Where(kv => group.Contains(kv.Value)).Select(kv => kv.Key).OrderBy(k => k).ToList();
            if (slots.Select(sl => result[sl]).Distinct().Count() == slots.Count) continue;
            var taken = new HashSet<string>();
            foreach (var slot in slots)
            {
                var role = taken.Add(result[slot]) ? result[slot] : group.First(r => !taken.Contains(r));
                taken.Add(role);
                result[slot] = role;
            }
        }
        return result;
    }

    private static ToolboxStep ConvertStep(JsonElement step, int index, float scale, Vector2 offset)
    {
        var inst = step.GetProperty("instance");
        var objects = new List<ToolboxObject>();
        foreach (var slot in Slots)
            if (inst.TryGetProperty(slot, out var p)) objects.Add(Obj("player", slot, p, scale, offset, job: Str(p, "label"), buffs: Buffs(p)));
        if (inst.TryGetProperty("boss", out var boss)) objects.Add(Obj("boss", "boss", boss, scale, offset));
        foreach (var (key, _) in WaymarkKeys)
            if (inst.TryGetProperty(key, out var wm)) objects.Add(Obj("waymark", key, wm, scale, offset));
        Manager(inst, "AddManager", "add", objects, scale, offset);
        Manager(inst, "ZoneManager", "zone", objects, scale, offset);
        Manager(inst, "TetherManager", "tether", objects, scale, offset);
        Manager(inst, "TextManager", "text", objects, scale, offset);
        return new ToolboxStep(index, Str(step, "label"), Str(step, "note"), objects);
    }

    private static void Manager(JsonElement inst, string manager, string kind, List<ToolboxObject> into, float scale, Vector2 offset)
    {
        if (!inst.TryGetProperty(manager, out var m) || !m.TryGetProperty("instances", out var list)) return;
        var i = 0;
        foreach (var o in list.EnumerateArray())
            into.Add(Obj(kind, $"{kind}{i++}", o, scale, offset));
    }

    private static ToolboxObject Obj(string kind, string key, JsonElement o, float scale, Vector2 offset, string? job = null, List<string>? buffs = null)
    {
        var c = Centre(o) * scale + offset;
        var (w, h) = Size(o);
        var shape = Str(o, "shape") is { Length: > 0 } s ? s : "circle";
        var color = Str(o, "backgroundColor");
        return new ToolboxObject(kind, key, Str(o, "label"), c.X, c.Y, w * scale, h * scale, Num(o, "rotation"), shape,
            color is "transparent" or "" ? null : color, job, buffs);
    }

    private static List<string>? Buffs(JsonElement p)
    {
        var list = new List<string>();
        for (var i = 1; i <= 4; i++)
            if (p.TryGetProperty($"buffIcon{i}Url", out var b) && b.ValueKind == JsonValueKind.String && b.GetString() is { Length: > 0 } url)
                list.Add(url);
        return list.Count == 0 ? null : list;
    }

    // Toolbox positions are the top-left corner in arena units.
    private static Vector2 Centre(JsonElement o)
    {
        var p = o.GetProperty("position");
        var (w, h) = Size(o);
        return new Vector2(Num(p, "x") + w / 2f, Num(p, "y") + h / 2f);
    }

    private static (float W, float H) Size(JsonElement o)
    {
        var size = Num(o, "size", 1f);
        var w = o.TryGetProperty("width", out var wv) && wv.ValueKind == JsonValueKind.Number ? wv.GetSingle() : size;
        var h = o.TryGetProperty("height", out var hv) && hv.ValueKind == JsonValueKind.Number ? hv.GetSingle() : size;
        return (w, h);
    }

    private static string Str(JsonElement o, string name)
        => o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    private static float Num(JsonElement o, string name, float fallback = 0f)
        => o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : fallback;

    // The job labels say which category a slot is; which seat inside it comes from the opener:
    // tanks swap among tanks, healers among healers, DPS among the four DPS seats, whichever
    // permutation puts the toolbox's first step closest to the strat's first order.
    public static Dictionary<string, string> RefineRoleMap(ToolboxPlan plan, OrderDump dump, Dictionary<string, string> byJob, out float residual)
    {
        residual = 0f;
        if (plan.Steps.Count == 0 || dump.Orders.Count == 0) return byJob;
        var seed = dump.Orders.Min(o => o.Seed);
        var firstTime = dump.Orders.Where(o => o.Seed == seed).Min(o => o.Time);
        var opener = dump.Orders.Where(o => o.Seed == seed && Math.Abs(o.Time - firstTime) < 0.01f).GroupBy(o => o.Role)
            .ToDictionary(g => g.Key, g => new Vector2(g.First().X, g.First().Z));
        var tb = plan.Steps[0].Objects.Where(o => o.Kind == "player").ToDictionary(o => o.Key, o => new Vector2(o.X, o.Z));
        var result = new Dictionary<string, string>(byJob);
        string[][] groups = [["tank1", "tank2"], ["healer1", "healer2"], ["dps1", "dps2", "dps3", "dps4"]];
        var total = 0f;
        foreach (var group in groups)
        {
            var slots = group.Where(tb.ContainsKey).ToList();
            var seats = group.Where(byJob.ContainsKey).Select(s => byJob[s]).ToList();
            if (slots.Count == 0 || seats.Count != slots.Count || !seats.All(opener.ContainsKey)) continue;
            var best = (Cost: float.MaxValue, Perm: (List<string>?)null);
            foreach (var perm in Permutations(seats))
            {
                var cost = 0f;
                for (var i = 0; i < slots.Count; i++) cost += Vector2.Distance(tb[slots[i]], opener[perm[i]]);
                if (cost < best.Cost) best = (cost, perm);
            }
            for (var i = 0; i < slots.Count; i++) result[slots[i]] = best.Perm![i];
            total += best.Cost;
        }
        residual = total / tb.Count;
        return result;
    }

    private static IEnumerable<List<string>> Permutations(List<string> items)
    {
        if (items.Count <= 1) { yield return items; yield break; }
        for (var i = 0; i < items.Count; i++)
        {
            var rest = items.Where((_, j) => j != i).ToList();
            foreach (var p in Permutations(rest)) yield return [items[i], .. p];
        }
    }

    // Which lesson step each toolbox step shows: a monotone alignment (toolbox order never goes
    // back in the lesson) minimising the mapped roles' mean distance, where leaving a toolbox
    // step unmatched costs `maxDistance`. Roll-dependent steps often stay unmatched; the author
    // fills those by hand.
    private const float LabelBonus = 2f;
    private static readonly string[] StopWords = ["the", "and", "with", "move", "position", "positions", "appear", "here", "again", "down"];

    private static bool LabelMatches(string label, LessonStep step)
    {
        var text = $"{step.Title} {step.Cue}".ToLowerInvariant();
        foreach (var token in label.ToLowerInvariant().Split([' ', '-', '&', '(', ')', ',', '/', ':', '+'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length < 4 || StopWords.Contains(token)) continue;
            var stem = token.TrimEnd('s');
            if (text.Contains(stem, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    public static List<(int ToolboxStep, string? StepId, float Distance)> Align(ToolboxPlan plan, Lesson lesson, OrderDump dump,
        Dictionary<string, string> roleMap, float maxDistance = 4f)
    {
        var seed = dump.Orders.Count == 0 ? 0 : dump.Orders.Min(o => o.Seed);
        var lessonSteps = lesson.Steps.Where(s => !s.Skip).ToList();
        var targets = lessonSteps.Select(s =>
        {
            var orders = dump.Orders.Where(o => o.Seed == seed && s.Key.Matches(o.Key)).ToList();
            var first = orders.Count == 0 ? 0f : orders.Min(o => o.Time);
            return orders.Where(o => Math.Abs(o.Time - first) < 0.01f).GroupBy(o => o.Role).ToDictionary(g => g.Key, g => new Vector2(g.First().X, g.First().Z));
        }).ToList();

        var n = plan.Steps.Count;
        var m = lessonSteps.Count;
        var cost = new float[n, m];
        for (var i = 0; i < n; i++)
        {
            var players = plan.Steps[i].Objects.Where(o => o.Kind == "player" && roleMap.ContainsKey(o.Key)).ToDictionary(o => roleMap[o.Key], o => new Vector2(o.X, o.Z));
            for (var j = 0; j < m; j++)
            {
                var shared = players.Keys.Where(targets[j].ContainsKey).ToList();
                cost[i, j] = shared.Count == 0 ? float.MaxValue : shared.Average(r => Vector2.Distance(players[r], targets[j][r]));
                // A toolbox label naming the mechanic the step is about beats a few metres of drift.
                if (cost[i, j] < float.MaxValue && LabelMatches(plan.Steps[i].Label, lessonSteps[j])) cost[i, j] = Math.Max(0f, cost[i, j] - LabelBonus);
            }
        }

        // dp[i, j]: best total for toolbox steps < i with the last match at lesson step <= j (j = m means "none yet" sits at index 0 via -1 offset).
        var dp = new float[n + 1, m + 1];
        var choice = new int[n + 1, m + 1];
        for (var i = 1; i <= n; i++)
        for (var j = 0; j <= m; j++)
        {
            // j here is "last matched lesson index + 1"; 0 = nothing matched yet.
            var skip = dp[i - 1, j] + maxDistance;
            var best = skip;
            var pick = -1;
            if (j > 0)
            {
                var c = cost[i - 1, j - 1];
                if (c < float.MaxValue)
                {
                    var prev = float.MaxValue;
                    for (var k = 0; k <= j; k++) prev = Math.Min(prev, dp[i - 1, k]);
                    if (prev + c < best) { best = prev + c; pick = j - 1; }
                }
            }
            dp[i, j] = best;
            choice[i, j] = pick;
        }

        var result = new (int, string?, float)[n];
        var jEnd = 0;
        for (var j = 0; j <= m; j++) if (dp[n, j] < dp[n, jEnd]) jEnd = j;
        var jj = jEnd;
        for (var i = n; i >= 1; i--)
        {
            var pick = choice[i, jj];
            if (pick < 0)
                result[i - 1] = (plan.Steps[i - 1].Index, null, Enumerable.Range(0, m).Select(j => cost[i - 1, j]).DefaultIfEmpty(float.MaxValue).Min());
            else
            {
                result[i - 1] = (plan.Steps[i - 1].Index, MasteryStore.StepId(lessonSteps[pick].Key), cost[i - 1, pick]);
                var bestK = 0;
                for (var k = 0; k <= jj; k++) if (dp[i - 1, k] < dp[i - 1, bestK]) bestK = k;
                jj = bestK;
            }
        }
        return result.ToList();
    }
}
