using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn.Toolbox;

// XIVPlan (xivplan.netlify.app): the whole scene travels in the link, "#/plan/" + base64url of a
// zlib-compressed JSON; a .xivplan file is the same JSON. Scene units are pixels of a 600-wide
// arena with the origin at the centre and y up; waymarks A-D calibrate when drawn, else the arena's
// half-width is taken for 20 yalms, which is a guess the report says.
public sealed class XivplanProvider : IRaidplanProvider
{
    public RaidplanHost Host => RaidplanHost.Xivplan;

    private const float AssumedHalfWidthYalms = 20f;
    private static readonly string[] Jobs = ["PLD", "WAR", "DRK", "GNB", "WHM", "SCH", "AST", "SGE", "MNK", "DRG", "NIN", "VPR", "RPR", "SAM", "BRD", "MCH", "DNC", "BLM", "SMN", "RDM", "PCT", "BLU"];

    public static string Decode(string linkOrPayload)
    {
        var text = linkOrPayload.Trim();
        var at = text.IndexOf("#/plan/", StringComparison.Ordinal);
        if (at >= 0) text = text[(at + "#/plan/".Length)..];
        else if (text.Contains("?plan=", StringComparison.Ordinal)) text = text[(text.IndexOf("?plan=", StringComparison.Ordinal) + 6)..];
        text = Uri.UnescapeDataString(text).Replace('-', '+').Replace('_', '/');
        text = text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=');
        using var input = new MemoryStream(System.Convert.FromBase64String(text));
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(zlib, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static string Encode(string sceneJson)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true)) zlib.Write(Encoding.UTF8.GetBytes(sceneJson));
        return "https://xivplan.netlify.app/#/plan/" + System.Convert.ToBase64String(output.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool Looks(string text) => text.Contains("xivplan", StringComparison.OrdinalIgnoreCase) && text.Contains("#/plan/", StringComparison.Ordinal);

    public (ToolboxPlan Plan, ImportReport Report) Convert(byte[] raw, string id, WaymarkLayout layout, float? arenaYalms = null)
    {
        var text = Encoding.UTF8.GetString(raw).Trim();
        var json = text.StartsWith('{') ? text : Decode(text);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var arena = root.TryGetProperty("arena", out var a) ? a : default;
        var halfWidth = Num(arena, "width", 600f) / 2f;
        var steps = root.TryGetProperty("steps", out var st) && st.ValueKind == JsonValueKind.Array ? st.EnumerateArray().ToList() : [];
        if (steps.Count == 0) throw new InvalidOperationException("no steps");
        var warnings = new List<string>();

        var objectsOf = steps.Select(s => s.TryGetProperty("objects", out var o) && o.ValueKind == JsonValueKind.Array ? o.EnumerateArray().ToList() : []).ToList();
        var marks = objectsOf.Select(list => list.Where(o => Str(o, "type") == "marker" && WaymarkOf(o) is { }).Select(o => (WaymarkOf(o)!.Value, Scene(list, o))).ToList())
            .FirstOrDefault(m => m.Count(x => x.Item1 <= WaymarkSlot.D) >= 2);
        float scale, residual;
        Vector2 offset;
        if (marks != null) (scale, offset, residual) = ToolboxImport.Calibrate(marks, layout);
        else
        {
            scale = AssumedHalfWidthYalms / halfWidth;
            offset = Vector2.Zero;
            residual = 0f;
            warnings.Add($"no waymarks A-D drawn: the arena's half-width is taken for {AssumedHalfWidthYalms:0} yalms (unverified)");
        }
        if (residual > 0.3f) warnings.Add($"waymark calibration residual {residual:F2} m");

        var roleMap = new Dictionary<string, string>();
        var converted = new List<ToolboxStep>();
        for (var i = 0; i < steps.Count; i++)
        {
            var list = objectsOf[i];
            var free = new Dictionary<string, Queue<string>>
            {
                ["tank"] = new(["tank1", "tank2"]), ["healer"] = new(["healer1", "healer2"]), ["dps"] = new(["dps1", "dps2", "dps3", "dps4"]),
            };
            // Icons named for a seat keep it; job icons take what is left of their group.
            foreach (var o in list.Where(o => Str(o, "type") == "party"))
                if (ExplicitSlot(Path.GetFileNameWithoutExtension(Str(o, "image"))) is { } reserved) Take(free, reserved.Slot);
            var objects = new List<ToolboxObject>();
            var n = 0;
            var enemies = 0;
            // Later entries render underneath; walk them bottom-up so the top icon is added last.
            foreach (var o in Enumerable.Reverse(list))
            {
                if (o.TryGetProperty("hide", out var hide) && hide.ValueKind == JsonValueKind.True) continue;
                var type = Str(o, "type");
                var at = Scene(list, o) * scale + offset;
                var rotation = Num(o, "rotation");
                var radius = Num(o, "radius") * scale;
                var color = Str(o, "color") is { Length: 7 } c ? c : null;
                switch (type)
                {
                    case "party":
                    {
                        var file = Path.GetFileNameWithoutExtension(Str(o, "image"));
                        var (slot, job, category) = PartySlot(file, free);
                        if (slot is null) { objects.Add(new ToolboxObject("text", $"text{n++}", Str(o, "name"), at.X, at.Y, 0f, 0f, 0f, "text")); break; }
                        objects.Add(new ToolboxObject("player", slot, Str(o, "name"), at.X, at.Y, 2f, 2f, 0f, "circle", Job: job));
                        roleMap.TryAdd(slot, RoleOf(slot, category));
                        break;
                    }
                    case "marker":
                    {
                        if (WaymarkOf(o) is not { } wm) { objects.Add(new ToolboxObject("text", $"text{n++}", Str(o, "name"), at.X, at.Y, 0f, 0f, 0f, "text")); break; }
                        var label = wm <= WaymarkSlot.D ? wm.ToString() : ((int)wm - (int)WaymarkSlot.One + 1).ToString();
                        objects.Add(new ToolboxObject("waymark", $"waymark{label}", $"Waymark {label}", at.X, at.Y, 2f, 2f, 0f, wm <= WaymarkSlot.D ? "square" : "circle"));
                        break;
                    }
                    case "enemy":
                    {
                        var kind = enemies++ == 0 ? "boss" : "add";
                        objects.Add(new ToolboxObject(kind, kind == "boss" ? "boss" : $"add{n++}", Str(o, "name"), at.X, at.Y, radius * 2f, radius * 2f, rotation, "circle", color));
                        break;
                    }
                    case "text":
                    case "icon":
                        objects.Add(new ToolboxObject("text", $"text{n++}", type == "text" ? Str(o, "text") : Str(o, "name"), at.X, at.Y, 0f, 0f, rotation, "text", color));
                        break;
                    case "tether":
                    {
                        var start = list.FirstOrDefault(x => Id(x) == Num(o, "startId", -1f));
                        var end = list.FirstOrDefault(x => Id(x) == Num(o, "endId", -1f));
                        if (start.ValueKind != JsonValueKind.Object || end.ValueKind != JsonValueKind.Object) break;
                        var s = Scene(list, start) * scale + offset;
                        var e = Scene(list, end) * scale + offset;
                        var delta = e - s;
                        objects.Add(new ToolboxObject("tether", $"tether{n++}", Str(o, "tether"), (s.X + e.X) / 2f, (s.Y + e.Y) / 2f, 0.5f, delta.Length(), MathF.Atan2(-delta.X, delta.Y) * 180f / MathF.PI, "rect", color));
                        break;
                    }
                    case "circle" or "knockback" or "rotateCW" or "rotateCCW" or "proximity" or "stack" or "eye" or "tower" or "polygon" or "exaflare" or "starburst":
                        objects.Add(new ToolboxObject("zone", $"zone{n++}", type, at.X, at.Y, radius * 2f, radius * 2f, rotation, "circle", color ?? "#fc972b"));
                        break;
                    case "donut":
                        objects.Add(new ToolboxObject("zone", $"zone{n++}", type, at.X, at.Y, radius * 2f, radius * 2f, rotation, "donut", color ?? "#fc972b"));
                        break;
                    case "cone" or "arc":
                        objects.Add(new ToolboxObject("zone", $"zone{n++}", $"{type} {Num(o, "coneAngle", 90f):0}°", at.X, at.Y, radius * 2f, radius * 2f, rotation, "cone", color ?? "#fc972b"));
                        break;
                    case "line" or "lineStack" or "lineKnockAway" or "arrow":
                    {
                        // The base sits at x/y; the box extends `length` along the rotation (0 = up = north).
                        var length = Num(o, "length") * scale;
                        var width = Num(o, "width") * scale;
                        var rad = rotation * MathF.PI / 180f;
                        var centre = at + new Vector2(MathF.Sin(rad), -MathF.Cos(rad)) * (length / 2f);
                        objects.Add(new ToolboxObject(type == "arrow" ? "tether" : "zone", $"{(type == "arrow" ? "tether" : "zone")}{n++}", type, centre.X, centre.Y, width, length, rotation, type == "arrow" ? "rect" : "square", color ?? "#fc972b"));
                        break;
                    }
                    case "rect" or "lineKnockback" or "triangle" or "rightTriangle" or "draw":
                        objects.Add(new ToolboxObject("zone", $"zone{n++}", type, at.X, at.Y, Num(o, "width") * scale, Num(o, "height") * scale, rotation, "square", color ?? "#fc972b"));
                        break;
                }
            }
            converted.Add(new ToolboxStep(i, steps.Count == 1 ? "Plan" : $"Step {i + 1}", "", objects));
        }
        return (new ToolboxPlan(id, "XIVPlan", "", layout.Name, scale, converted), new ImportReport(steps.Count, steps.Count, false, scale, residual, ToolboxImport.Dedupe(roleMap), warnings));
    }

    private static WaymarkSlot? WaymarkOf(JsonElement o)
    {
        var file = Path.GetFileNameWithoutExtension(Str(o, "image"));
        if (!file.StartsWith("waymark_", StringComparison.Ordinal) || file.Length != 9) return null;
        return file[8] switch
        {
            'a' => WaymarkSlot.A, 'b' => WaymarkSlot.B, 'c' => WaymarkSlot.C, 'd' => WaymarkSlot.D,
            '1' => WaymarkSlot.One, '2' => WaymarkSlot.Two, '3' => WaymarkSlot.Three, '4' => WaymarkSlot.Four, _ => null,
        };
    }

    // Scene coordinates (centre origin, y up) as the plan's X east / Z south, following positionParentId chains.
    private static Vector2 Scene(List<JsonElement> list, JsonElement o)
    {
        var at = new Vector2(Num(o, "x"), -Num(o, "y"));
        var guard = 0;
        var current = o;
        while (current.TryGetProperty("positionParentId", out var pid) && pid.ValueKind == JsonValueKind.Number && guard++ < 16)
        {
            var parent = list.FirstOrDefault(x => Id(x) == pid.GetSingle());
            if (parent.ValueKind != JsonValueKind.Object) break;
            at += new Vector2(Num(parent, "x"), -Num(parent, "y"));
            current = parent;
        }
        return at;
    }

    private static (string Slot, string Category)? ExplicitSlot(string file) => file.ToLowerInvariant() switch
    {
        "tank1" => ("tank1", "tank"), "tank2" => ("tank2", "tank"),
        "healer1" or "pure_healer" => ("healer1", "regen"), "healer2" or "barrier_healer" => ("healer2", "shield"),
        "dps1" or "melee1" => ("dps1", "melee"), "dps2" or "melee2" => ("dps2", "melee"),
        "dps3" or "ranged1" or "physical_ranged" => ("dps3", "phys"), "dps4" or "ranged2" or "magic_ranged" => ("dps4", "caster"),
        _ => null,
    };

    private static (string? Slot, string? Job, string Category) PartySlot(string file, Dictionary<string, Queue<string>> free)
    {
        if (ExplicitSlot(file) is { } explicitSlot) return (explicitSlot.Slot, null, explicitSlot.Category);
        var upper = file.ToUpperInvariant();
        if (Jobs.Contains(upper))
        {
            var category = upper switch
            {
                "PLD" or "WAR" or "DRK" or "GNB" => "tank",
                "WHM" or "AST" => "regen", "SCH" or "SGE" => "shield",
                "MNK" or "DRG" or "NIN" or "VPR" or "RPR" or "SAM" => "melee",
                "BRD" or "MCH" or "DNC" => "phys", _ => "caster",
            };
            // A job takes the seat its category usually fills, then whatever its group still has.
            var preferred = category switch { "regen" => "healer1", "shield" => "healer2", "phys" => "dps3", "caster" => "dps4", _ => null };
            var slot = (preferred != null ? Take(free, preferred) : null) ?? Take(free, category is "tank" ? "tank" : category is "regen" or "shield" ? "healer" : "dps");
            return (slot, upper, category);
        }
        var lower = file.ToLowerInvariant();
        return lower switch
        {
            "tank" => (Take(free, "tank"), null, "tank"),
            "healer" => (Take(free, "healer"), null, "regen"),
            "dps" or "melee" or "ranged" => (Take(free, "dps"), null, lower == "ranged" ? "phys" : "melee"),
            _ => (null, null, ""),
        };
    }

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

    private static float Id(JsonElement o) => Num(o, "id", -2f);
    private static string Str(JsonElement o, string name) => o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
    private static float Num(JsonElement o, string name, float fallback = 0f) => o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : fallback;
}
