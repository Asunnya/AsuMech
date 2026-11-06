using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn.Toolbox.Stgy;

// The Strategy Board as a raidplan: one board is one step, so the raw input is a list of codes
// (one per line, or ffxivstrats.io feed JSON with a "code" per item) in step order. Waymarks A-D
// calibrate the board when the author placed them; without them the arena outline of the board's
// background is taken for a 20-yalm arena, which is only a guess.
public sealed class StgyProvider : IRaidplanProvider
{
    public RaidplanHost Host => RaidplanHost.Stgy;

    // The site's background images put the arena ring 174 board px from the centre; how large the
    // game draws it is UNVERIFIED, so this calibration carries a warning.
    private const float ArenaRingPx = 174f;
    private const float AssumedArenaYalms = 20f;
    private static readonly Vector2 Centre = new(StgyCode.BoardWidth / 2f, StgyCode.BoardHeight / 2f);

    private static readonly Dictionary<int, string> RoleSlots = new()
    {
        [48] = "tank1", [49] = "tank2", [51] = "healer1", [52] = "healer2", [54] = "dps1", [55] = "dps2", [56] = "dps3", [57] = "dps4",
    };

    private static string Category(int type) => type switch
    {
        47 or 18 or 20 or 27 or 29 or 38 or 43 => "tank",
        50 or 122 or 23 or 32 or 39 => "regen",
        123 or 35 or 46 => "shield",
        118 or 19 or 21 or 26 or 28 or 30 or 36 or 40 or 45 or 101 => "melee",
        120 or 22 or 31 or 37 or 44 => "phys",
        121 or 24 or 25 or 33 or 34 or 41 or 42 or 102 => "caster",
        53 or 119 => "dps",
        _ => "",
    };

    public static List<(string Name, string Code)> Codes(byte[] raw)
    {
        var text = System.Text.Encoding.UTF8.GetString(raw).Trim();
        var list = new List<(string, string)>();
        if (text.StartsWith('{') || (text.StartsWith('[') && !text.StartsWith("[stgy:", StringComparison.Ordinal)))
        {
            using var doc = JsonDocument.Parse(text);
            var items = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray().ToList() : [doc.RootElement];
            foreach (var item in items)
                if (item.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
                    list.Add((item.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString()! : "", code.GetString()!));
            return list;
        }
        foreach (System.Text.RegularExpressions.Match m in StgyCode.Pattern.Matches(text)) list.Add(("", m.Value));
        return list;
    }

    public (ToolboxPlan Plan, ImportReport Report) Convert(byte[] raw, string id, WaymarkLayout layout, float? arenaYalms = null)
    {
        var codes = Codes(raw);
        if (codes.Count == 0) throw new InvalidOperationException("no [stgy:] code in the input");
        var docs = codes.Select(c => (c.Name, Doc: StgyCode.Decode(c.Code))).ToList();
        var warnings = new List<string>();

        var marks = docs.Select(d => d.Doc.Objects.Where(o => StgyTypes.IsWaymark(o.TypeId)).Select(o => ((WaymarkSlot)(o.TypeId - 79), new Vector2(o.X, o.Y))).ToList())
            .FirstOrDefault(m => m.Count(x => x.Item1 <= WaymarkSlot.D) >= 2);
        float scale, residual;
        Vector2 offset;
        if (marks != null) (scale, offset, residual) = ToolboxImport.Calibrate(marks, layout);
        else
        {
            scale = AssumedArenaYalms / ArenaRingPx;
            offset = -Centre * scale;
            residual = 0f;
            warnings.Add($"no waymarks A-D on any board: the board's arena ring is taken for {AssumedArenaYalms:0} yalms (unverified)");
        }
        if (residual > 0.3f) warnings.Add($"waymark calibration residual {residual:F2} m");

        var roleMap = new Dictionary<string, string>();
        var steps = new List<ToolboxStep>();
        for (var i = 0; i < docs.Count; i++)
        {
            var (name, doc) = docs[i];
            var objects = new List<ToolboxObject>();
            var free = new Dictionary<string, Queue<string>>
            {
                ["tank"] = new(["tank1", "tank2"]), ["healer"] = new(["healer1", "healer2"]), ["dps"] = new(["dps1", "dps2", "dps3", "dps4"]),
            };
            foreach (var o in doc.Objects.Where(o => RoleSlots.ContainsKey(o.TypeId))) Take(free, RoleSlots[o.TypeId]);
            var n = 0;
            var enemies = 0;
            foreach (var o in doc.Objects)
            {
                if (!o.Visible) continue;
                var at = new Vector2(o.X, o.Y) * scale + offset;
                var t = o.TypeId;
                var size = o.Size / 100f;
                if (RoleSlots.TryGetValue(t, out var slot) || (Category(t) is { Length: > 0 } category && (slot = Take(free, Group(category))) != null))
                {
                    var job = StgyTypes.Jobs.GetValueOrDefault(t);
                    objects.Add(new ToolboxObject("player", slot, job ?? o.TypeName, at.X, at.Y, 2f, 2f, 0f, "circle", Job: job));
                    roleMap.TryAdd(slot, RoleOf(slot, job is null ? Category(t) : Category(t)));
                    continue;
                }
                if (StgyTypes.IsWaymark(t))
                {
                    var wm = (WaymarkSlot)(t - 79);
                    var label = wm <= WaymarkSlot.D ? wm.ToString() : ((int)wm - (int)WaymarkSlot.One + 1).ToString();
                    objects.Add(new ToolboxObject("waymark", $"waymark{label}", $"Waymark {label}", at.X, at.Y, 2f, 2f, 0f, wm <= WaymarkSlot.D ? "square" : "circle"));
                    continue;
                }
                if (StgyTypes.IsEnemy(t))
                {
                    var kind = enemies++ == 0 && t != 60 ? "boss" : "add";
                    var d = (t == 64 ? 20f : t == 62 ? 12f : 6f) * size * scale;
                    objects.Add(new ToolboxObject(kind, kind == "boss" ? "boss" : $"add{n++}", o.TypeName, at.X, at.Y, d, d, o.Angle, "circle"));
                    continue;
                }
                if (t == 12)
                {
                    // A line runs from (x,y) to the params' end; stored like a tether: centre, length, bearing.
                    var end = new Vector2(o.A / 10f, o.B / 10f) * scale + offset;
                    var mid = (at + end) / 2f;
                    var delta = end - at;
                    var bearing = MathF.Atan2(-delta.X, delta.Y) * 180f / MathF.PI;
                    objects.Add(new ToolboxObject("tether", $"tether{n++}", "", mid.X, mid.Y, Math.Max(o.C, 1) * scale, delta.Length(), bearing, "rect", Hex(o)));
                    continue;
                }
                if (t == 100)
                {
                    objects.Add(new ToolboxObject("text", $"text{n++}", o.Text ?? "", at.X, at.Y, 0f, 0f, o.Angle, "text", Hex(o)));
                    continue;
                }
                if (StgyTypes.IsAoe(t))
                {
                    objects.Add(Aoe(o, at, scale, n++));
                    continue;
                }
                // Target markers, lock-ons, status icons, shapes: drawn as labels where they sit.
                objects.Add(new ToolboxObject("text", $"text{n++}", o.TypeName.Replace('_', ' '), at.X, at.Y, 0f, 0f, 0f, "text"));
            }
            var stepName = name.Length > 0 ? name : doc.Name.Length > 0 ? doc.Name : $"Board {i + 1}";
            steps.Add(new ToolboxStep(i, stepName, "", objects));
        }
        var title = docs[0].Doc.Name.Length > 0 && docs.Count == 1 ? docs[0].Doc.Name : docs[0].Name;
        return (new ToolboxPlan(id, title, "", layout.Name, scale, steps), new ImportReport(docs.Count, docs.Count, false, scale, residual, ToolboxImport.Dedupe(roleMap), warnings));
    }

    // AoE sizes on the board depend on the site's icon images, which the format does not carry;
    // the donut is the one shape drawn procedurally (outer radius = size × 2.5 px). Others take a
    // nominal 50 px diameter at size 100 (UNVERIFIED).
    private static ToolboxObject Aoe(StgyObject o, Vector2 at, float scale, int n)
    {
        var size = o.Size / 100f;
        var key = $"zone{n}";
        var name = o.TypeName;
        switch (o.TypeId)
        {
            case 17:
                var outer = size * 2.5f * 2f * scale;
                return new ToolboxObject("zone", key, name, at.X, at.Y, outer, outer, o.Angle, "donut", "#ffa131");
            case 10:
                var fan = size * 50f * scale;
                return new ToolboxObject("zone", key, $"{name} {(o.A > 0 ? o.A : 90)}°", at.X, at.Y, fan, fan, o.Angle, "cone", Hex(o));
            case 11:
                return new ToolboxObject("zone", key, name, at.X, at.Y, (o.A > 0 ? o.A : 100) * scale, (o.B > 0 ? o.B : 20) * scale, o.Angle, "square", Hex(o));
            case 15 or 110 or 8 or 125:
                var side = size * 50f * scale;
                return new ToolboxObject("zone", key, name, at.X, at.Y, side, side, o.Angle, "square", Hex(o));
            default:
                var d = size * 50f * scale;
                return new ToolboxObject("zone", key, name, at.X, at.Y, d, d, o.Angle, "circle", Hex(o));
        }
    }

    private static string? Hex(StgyObject o) => o.TypeId is 11 or 12 or 100 ? $"#{o.Color.R:x2}{o.Color.G:x2}{o.Color.B:x2}" : null;

    private static string Group(string category) => category is "tank" ? "tank" : category is "regen" or "shield" ? "healer" : "dps";

    private static string? Take(Dictionary<string, Queue<string>> free, string groupOrSlot)
    {
        if (free.TryGetValue(groupOrSlot, out var queue)) return queue.Count > 0 ? queue.Dequeue() : null;
        foreach (var q in free.Values)
        {
            if (!q.Contains(groupOrSlot)) continue;
            var kept = q.Where(s => s != groupOrSlot).ToList();
            q.Clear();
            foreach (var s in kept) q.Enqueue(s);
            return groupOrSlot;
        }
        return null;
    }

    private static string RoleOf(string slot, string category) => slot switch
    {
        "tank1" => nameof(PartyRole.MainTank), "tank2" => nameof(PartyRole.OffTank),
        "healer1" => category == "shield" ? nameof(PartyRole.ShieldHealer) : nameof(PartyRole.RegenHealer),
        "healer2" => category == "regen" ? nameof(PartyRole.RegenHealer) : nameof(PartyRole.ShieldHealer),
        "dps1" => nameof(PartyRole.MeleeDpsA), "dps2" => nameof(PartyRole.MeleeDpsB),
        "dps3" => category == "caster" ? nameof(PartyRole.CasterDps) : nameof(PartyRole.PhysRangedDps),
        _ => category == "phys" ? nameof(PartyRole.PhysRangedDps) : nameof(PartyRole.CasterDps),
    };
}
