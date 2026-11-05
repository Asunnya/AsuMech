using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn.Toolbox;

// raidplan.io: the plan document (userdata.raidplan.io/<code>.json) is a flat list of nodes, each
// tagged with its step; players are "marker" nodes labelled MT/ST/H1/H2/D1-D4, waymarks are
// "waypoint" nodes, AoEs are "ability" nodes drawn as 100-unit SVGs scaled on a 1200x675 canvas.
public sealed class RaidplanIoProvider : IRaidplanProvider
{
    public RaidplanHost Host => RaidplanHost.RaidplanIo;

    private const float AbilityUnits = 100f;
    private const float ImageSpanPx = 675f;
    private const float AssumedArenaYalms = 40f;
    private static readonly Dictionary<string, string> Slots = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MT"] = "tank1", ["ST"] = "tank2", ["OT"] = "tank2", ["H1"] = "healer1", ["H2"] = "healer2",
        ["D1"] = "dps1", ["D2"] = "dps2", ["D3"] = "dps3", ["D4"] = "dps4",
        ["M1"] = "dps1", ["M2"] = "dps2", ["R1"] = "dps3", ["R2"] = "dps4",
    };
    private static readonly Dictionary<string, string> Jobs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pld"] = "PLD", ["war"] = "WAR", ["drk"] = "DRK", ["gnb"] = "GNB",
        ["whm"] = "WHM", ["sch"] = "SCH", ["ast"] = "AST", ["sge"] = "SGE",
        ["mnk"] = "MNK", ["drg"] = "DRG", ["nin"] = "NIN", ["sam"] = "SAM", ["rpr"] = "RPR", ["vpr"] = "VPR",
        ["bard"] = "BRD", ["brd"] = "BRD", ["mch"] = "MCH", ["dnc"] = "DNC",
        ["blm"] = "BLM", ["smn"] = "SMN", ["rmage"] = "RDM", ["rdm"] = "RDM", ["pct"] = "PCT",
    };
    private static readonly Dictionary<string, WaymarkSlot> Waymarks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = WaymarkSlot.A, ["b"] = WaymarkSlot.B, ["c"] = WaymarkSlot.C, ["d"] = WaymarkSlot.D,
        ["1"] = WaymarkSlot.One, ["2"] = WaymarkSlot.Two, ["3"] = WaymarkSlot.Three, ["4"] = WaymarkSlot.Four,
    };
    private static readonly Dictionary<string, string> Shapes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ff-circle"] = "circle", ["ff-stack"] = "circle", ["ff-area-prox"] = "circle", ["ff-knock"] = "circle",
        ["ff-square"] = "square", ["ff-donut"] = "donut", ["ff-wedge"] = "cone", ["ff-quarter"] = "cone",
    };

    public (ToolboxPlan Plan, ImportReport Report) Convert(byte[] raw, string id, WaymarkLayout layout)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var nodes = root.GetProperty("nodes").EnumerateArray().ToList();
        var count = root.TryGetProperty("steps", out var st) && st.ValueKind == JsonValueKind.Number ? st.GetInt32()
            : nodes.Count == 0 ? 0 : nodes.Max(Step) + 1;
        if (count == 0) throw new InvalidOperationException("no steps");
        var warnings = new List<string>();

        // Waymarks keep their place across steps; the first step that draws A-D calibrates.
        var marks = Enumerable.Range(0, count)
            .Select(i => nodes.Where(n => Step(n) == i && Type(n) == "waypoint").Select(n => (Waymarks.GetValueOrDefault(Str(Attr(n), "wayId"), WaymarkSlot.A), Pos(n))).ToList())
            .FirstOrDefault(m => m.Count(x => x.Item1 <= WaymarkSlot.D) >= 2);
        float scale, residual;
        Vector2 offset;
        if (marks != null) (scale, offset, residual) = ToolboxImport.Calibrate(marks, layout);
        else
        {
            // No A-D drawn: the arena image is contain-fit to the 1200x675 canvas, so a square floor
            // spans 675 px; taken for a 40-yalm arena, which the report says is a guess.
            scale = AssumedArenaYalms / ImageSpanPx;
            offset = -new Vector2(600f, 337.5f) * scale;
            residual = 0f;
            warnings.Add($"no waymarks A-D on any step: the arena image is taken for {AssumedArenaYalms:0} yalms across (unverified)");
        }
        if (residual > 0.3f) warnings.Add($"waymark calibration residual {residual:F2} m");

        var notes = root.TryGetProperty("step_notes_raw", out var sn) && sn.ValueKind == JsonValueKind.Array ? sn.EnumerateArray().Select(e => e.GetString() ?? "").ToList() : [];
        var header = Str(root, "header_notes_raw");
        var steps = new List<ToolboxStep>();
        var roleMap = new Dictionary<string, string>();
        for (var i = 0; i < count; i++)
        {
            var objects = new List<ToolboxObject>();
            var seen = new HashSet<string>();
            var n = 0;
            foreach (var node in nodes.Where(x => Step(x) == i))
            {
                var attr = Attr(node);
                var at = Pos(node) * scale + offset;
                var (w, h) = Extent(node);
                switch (Type(node))
                {
                    case "marker":
                        var text = Str(attr, "text");
                        if (!Slots.TryGetValue(text, out var slot)) { if (text.Length > 0) objects.Add(new ToolboxObject("text", $"text{n++}", text, at.X, at.Y, w * scale, h * scale, 0f, "text")); break; }
                        // A job icon and a role icon stacked on one slot: the job one names the job.
                        var job = JobOf(Str(attr, "asset"));
                        if (!seen.Add(slot))
                        {
                            if (job != null) objects[objects.FindIndex(o => o.Key == slot)] = objects.First(o => o.Key == slot) with { Job = job };
                            break;
                        }
                        objects.Add(new ToolboxObject("player", slot, text, at.X, at.Y, w * scale, h * scale, 0f, "circle", Job: job));
                        if (job != null && !roleMap.ContainsKey(slot)) roleMap[slot] = RoleOf(slot);
                        break;
                    case "waypoint":
                        if (!Waymarks.TryGetValue(Str(attr, "wayId"), out var wm)) break;
                        var name = wm.ToString() is { Length: 1 } letter ? letter : ((int)wm - (int)WaymarkSlot.One + 1).ToString();
                        objects.Add(new ToolboxObject("waymark", $"waymark{name}", $"Waymark {name}", at.X, at.Y, w * scale, h * scale, 0f, wm <= WaymarkSlot.D ? "square" : "circle"));
                        break;
                    case "ability":
                        var ability = Str(attr, "abilityId");
                        if (ability == "ff-boss") { objects.Add(new ToolboxObject("boss", "boss", "Boss", at.X, at.Y, w * scale, h * scale, Num(Meta(node), "angle"), "circle")); break; }
                        objects.Add(new ToolboxObject("zone", $"zone{n++}", ability, at.X, at.Y, w * scale, h * scale, Num(Meta(node), "angle"), Shapes.GetValueOrDefault(ability, "square"), Color(Str(attr, "colorA"))));
                        break;
                    case "circle":
                        objects.Add(new ToolboxObject("zone", $"zone{n++}", Str(attr, "text"), at.X, at.Y, w * scale, h * scale, Num(Meta(node), "angle"), "circle", Color(Str(attr, "fill"))));
                        break;
                    case "arrow":
                        objects.Add(new ToolboxObject("tether", $"tether{n++}", Str(attr, "text"), at.X, at.Y, w * scale, h * scale, Num(Meta(node), "angle"), "rect", Color(Str(attr, "fill"))));
                        break;
                    case "itext":
                    case "text":
                        objects.Add(new ToolboxObject("text", $"text{n++}", Str(attr, "text"), at.X, at.Y, w * scale, h * scale, 0f, "text"));
                        break;
                }
            }
            var note = i < notes.Count ? notes[i] : i == 0 ? header : "";
            var label = objects.Where(o => o.Kind == "text").Select(o => o.Label.Split('\n')[0]).FirstOrDefault(l => l.Length > 0) ?? $"Step {i + 1}";
            steps.Add(new ToolboxStep(i, label, note, objects));
        }
        foreach (var slot in Slots.Values.Distinct().Where(s => !roleMap.ContainsKey(s) && steps.Any(st => st.Objects.Any(o => o.Key == s))))
            roleMap[slot] = RoleOf(slot);
        return (new ToolboxPlan(id, Str(root, "name") is { Length: > 0 } nm ? nm : header.Split('\n')[0], Str(root, "author"), layout.Name, scale, steps),
            new ImportReport(count, count, false, scale, residual, roleMap, warnings));
    }

    // The label already says the seat; the opener refines ties within a group later.
    private static string RoleOf(string slot) => slot switch
    {
        "tank1" => nameof(PartyRole.MainTank), "tank2" => nameof(PartyRole.OffTank),
        "healer1" => nameof(PartyRole.RegenHealer), "healer2" => nameof(PartyRole.ShieldHealer),
        "dps1" => nameof(PartyRole.MeleeDpsA), "dps2" => nameof(PartyRole.MeleeDpsB),
        "dps3" => nameof(PartyRole.PhysRangedDps), _ => nameof(PartyRole.CasterDps),
    };

    private static string? JobOf(string asset)
    {
        var file = asset.Split('/').LastOrDefault() ?? "";
        var stem = file.Contains('.') ? file[..file.LastIndexOf('.')] : file;
        return Jobs.GetValueOrDefault(stem);
    }

    private static int Step(JsonElement node) => Meta(node).TryGetProperty("step", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0;
    private static string Type(JsonElement node) => Str(node, "type");
    private static JsonElement Attr(JsonElement node) => node.TryGetProperty("attr", out var a) ? a : default;
    private static JsonElement Meta(JsonElement node) => node.TryGetProperty("meta", out var m) ? m : default;

    private static Vector2 Pos(JsonElement node)
    {
        var meta = Meta(node);
        if (!meta.TryGetProperty("pos", out var p)) return Vector2.Zero;
        return new Vector2(Num(p, "x"), Num(p, "y"));
    }

    // Rendered extent in canvas px: an ability is a 100-unit drawing, everything else its size × scale.
    private static (float W, float H) Extent(JsonElement node)
    {
        var meta = Meta(node);
        var sx = meta.TryGetProperty("scale", out var sc) ? Num(sc, "x", 1f) : 1f;
        var sy = meta.TryGetProperty("scale", out sc) ? Num(sc, "y", 1f) : 1f;
        if (Type(node) == "ability") return (AbilityUnits * sx, AbilityUnits * sy);
        var w = meta.TryGetProperty("size", out var size) ? Num(size, "w") : 0f;
        var h = meta.TryGetProperty("size", out size) ? Num(size, "h") : 0f;
        return (w * sx, h * sy);
    }

    private static string? Color(string hex) => hex is { Length: >= 7 } && hex[0] == '#' ? hex[..7] : null;

    private static string Str(JsonElement o, string name)
        => o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    private static float Num(JsonElement o, string name, float fallback = 0f)
        => o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : fallback;
}
