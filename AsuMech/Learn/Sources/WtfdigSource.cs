using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using AsuMech.Core.Game.Party;

namespace AsuMech.Learn.Sources;

// One mechanic's text from WTFDIG for one strat variant: what happens, what to do, and what each
// role does, resolved for a strat name and its toggle defaults.
public sealed record WtfdigMechanic(string Phase, string PhaseTag, string Mechanic, string? Description, string? Action, Dictionary<string, string> ByRole);

// The converted data.ts (+ mits.ts) of one WTFDIG fight, embedded under Learn/Sources/wtfdig/<fight>.json.
// The credit (MIT, github.com/mczub/wtfdig) travels with the file.
public sealed class WtfdigSource
{
    private const string ResourcePrefix = "AsuMech.Learn.Sources.wtfdig.";

    public string Fight { get; }
    public string Commit { get; }
    public string Url { get; }
    public JsonArray Strats { get; }
    public JsonObject Config { get; }
    public JsonObject Mits { get; }

    private WtfdigSource(string fight, string commit, string url, JsonArray strats, JsonObject config, JsonObject mits)
    {
        Fight = fight;
        Commit = commit;
        Url = url;
        Strats = strats;
        Config = config;
        Mits = mits;
    }

    public static WtfdigSource Parse(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        return new WtfdigSource(root["fight"]?.GetValue<string>() ?? "", root["commit"]?.GetValue<string>() ?? "", root["source"]?.GetValue<string>() ?? "",
            root["strats"]?.AsArray() ?? [], root["config"]?.AsObject() ?? [], root["mits"]?.AsObject() ?? []);
    }

    public static WtfdigSource? Load(string fight)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{ResourcePrefix}{fight}.json");
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static IEnumerable<string> Fights()
        => Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)).Select(n => n[ResourcePrefix.Length..^".json".Length]);

    // From the two TypeScript modules: the exported *Strats array, the *FightConfig object (its
    // poster layout dropped) and every *MitPlan the mits module exports.
    public static string Convert(string fight, string dataTs, string? mitsTs, string commit, string sourceUrl)
    {
        var mits = mitsTs is null ? new TsModule() : TsLiteral.Parse(mitsTs);
        var data = TsLiteral.Parse(dataTs, mits.Symbols);
        var strats = data.Exports.Select(n => data[n]).OfType<JsonArray>().FirstOrDefault(a => a.Count > 0 && a[0] is JsonObject o && o.ContainsKey("stratName"))
                     ?? throw new InvalidDataException("no exported Strat[] (an array of objects with stratName)");
        var config = data.Exports.Select(n => data[n]).OfType<JsonObject>().FirstOrDefault(o => o.ContainsKey("fightKey") || o.ContainsKey("strats"))
                     ?? throw new InvalidDataException("no exported FightConfig");
        config.Remove("posterLayout");
        config.Remove("mitPlans");
        var plans = new JsonObject();
        foreach (var name in mits.Exports)
            if (mits[name] is JsonObject plan && plan["planName"] is { } pn) plans[pn.GetValue<string>()] = plan.DeepClone();
        var root = new JsonObject
        {
            ["fight"] = fight,
            ["source"] = sourceUrl,
            ["commit"] = commit,
            ["license"] = "MIT (github.com/mczub/wtfdig)",
            ["strats"] = strats.DeepClone(),
            ["config"] = config.DeepClone(),
            ["mits"] = plans,
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    public IEnumerable<string> StratNames => Strats.OfType<JsonObject>().Select(s => s["stratName"]?.GetValue<string>() ?? "");

    public string DefaultStrat => Config["defaultStratName"]?.GetValue<string>() ?? StratNames.FirstOrDefault() ?? "";

    // A strat's toggle values: its own defaults, then each toggle's default.
    public Dictionary<string, string> Toggles(string strat)
    {
        var values = new Dictionary<string, string>();
        if (Config["toggles"] is JsonArray toggles)
            foreach (var t in toggles.OfType<JsonObject>())
                if (t["key"]?.GetValue<string>() is { } key && t["defaultValue"] is JsonValue dv) values[key] = dv.ToString();
        if (Config["strats"]?[strat]?["defaults"] is JsonObject defaults)
            foreach (var (k, v) in defaults) if (v is JsonValue jv) values[k] = jv.ToString();
        return values;
    }

    // The role labels the fight uses for (role, party): roleOptions when the fight has them, else the
    // usual MT/OT, H1/H2, M1/M2, R1/R2.
    public string RoleLabel(string role, int party)
    {
        if (Config["roleOptions"] is JsonArray options)
            foreach (var o in options.OfType<JsonObject>())
                if (o["role"]?.GetValue<string>() == role && (int)(o["party"]?.GetValue<double>() ?? 0) == party) return o["label"]?.GetValue<string>() ?? $"{role} {party}";
        return role switch
        {
            "Tank" => party == 1 ? "MT" : "OT",
            "Healer" => $"H{party}",
            "Melee" => $"M{party}",
            "Ranged" => $"R{party}",
            _ => $"{role} {party}",
        };
    }

    public static PartyRole? SeatOf(string role, int party) => (role, party) switch
    {
        ("Tank", 1) => PartyRole.MainTank, ("Tank", 2) => PartyRole.OffTank,
        ("Healer", 1) => PartyRole.RegenHealer, ("Healer", 2) => PartyRole.ShieldHealer,
        ("Melee", 1) => PartyRole.MeleeDpsA, ("Melee", 2) => PartyRole.MeleeDpsB,
        ("Ranged", 1) => PartyRole.PhysRangedDps, ("Ranged", 2) => PartyRole.CasterDps,
        _ => null,
    };

    // Every mechanic a strat shows, in page order, with its texts resolved for that strat.
    public List<WtfdigMechanic> Mechanics(string? strat = null)
    {
        strat ??= DefaultStrat;
        var toggles = Toggles(strat);
        var result = new List<WtfdigMechanic>();
        var entry = Strats.OfType<JsonObject>().FirstOrDefault(s => s["stratName"]?.GetValue<string>() == strat) ?? Strats.OfType<JsonObject>().FirstOrDefault();
        if (entry?["strats"] is not JsonArray phases) return result;
        foreach (var phase in phases.OfType<JsonObject>())
        {
            var tag = phase["tag"]?.GetValue<string>() ?? "";
            var phaseName = phase["phaseName"]?.GetValue<string>() ?? tag;
            var mechs = phase["mechs"] switch
            {
                JsonArray list => list,
                JsonObject byToggle => toggles.TryGetValue(tag, out var value) && byToggle[value] is JsonArray chosen ? chosen : byToggle.FirstOrDefault().Value as JsonArray,
                _ => null,
            };
            if (mechs is null) continue;
            foreach (var mech in mechs.OfType<JsonObject>())
            {
                var byRole = new Dictionary<string, string>();
                if (mech["strats"] is JsonArray roleStrats)
                    foreach (var rs in roleStrats.OfType<JsonObject>())
                    {
                        var role = rs["role"]?.GetValue<string>() ?? "";
                        var party = (int)(rs["party"]?.GetValue<double>() ?? 1);
                        if (Text(rs["description"], strat) is { Length: > 0 } text && SeatOf(role, party) is { } seat) byRole[seat.ToString()] = text;
                    }
                result.Add(new WtfdigMechanic(phaseName, tag, mech["mechanic"]?.GetValue<string>() ?? "", Text(mech["description"], strat), Text(mech["action"], strat), byRole));
            }
        }
        return result;
    }

    // A string, or a record keyed by strat name.
    private static string? Text(JsonNode? node, string strat) => node switch
    {
        JsonValue v => v.ToString(),
        JsonObject o => o[strat]?.GetValue<string>() ?? o.FirstOrDefault().Value?.GetValue<string>(),
        _ => null,
    };

    // A mit plan as the sheet importer's grid: one row per mechanic (phase-relative seconds in the
    // label's wake), the ten role columns of MitPlan.Columns, healers by job.
    public List<List<string>> MitGrid(string planName, string phaseTag)
    {
        var columns = Mitigation.MitPlan.Columns;
        var rows = new List<List<string>> { new[] { "", "" }.Concat(columns).ToList() };
        if (Mits[planName]?["mechs"] is not JsonArray mechs) return rows;
        foreach (var mech in mechs.OfType<JsonObject>())
        {
            if (mech["phase"]?.GetValue<string>() != phaseTag) continue;
            var cells = columns.ToDictionary(c => c, _ => new List<string>());
            if (mech["mits"] is JsonArray mits)
                foreach (var m in mits.OfType<JsonObject>())
                {
                    var text = m["mitigation"]?.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var label = m["label"]?.GetValue<string>();
                    var line = label is { Length: > 0 } ? $"{text} ({label})" : text;
                    foreach (var column in ColumnsOf(m)) cells[column].Add(line);
                }
            var row = new List<string> { "", mech["mechanic"]?.GetValue<string>() ?? "" };
            row.AddRange(columns.Select(c => string.Join("\n", cells[c])));
            rows.Add(row);
        }
        return rows;
    }

    private static readonly Dictionary<string, string> HealerColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SCH"] = "Scholar", ["SGE"] = "Sage", ["WHM"] = "White Mage", ["AST"] = "Astrologian",
    };

    private static IEnumerable<string> ColumnsOf(JsonObject mit)
    {
        var role = mit["role"]?.GetValue<string>() ?? "";
        var party = mit["party"] is JsonValue p ? (int)p.GetValue<double>() : 0;
        var jobs = mit["jobs"] is JsonArray j ? j.Select(x => x?.GetValue<string>() ?? "").ToList() : [];
        switch (role)
        {
            case "Tank":
                if (party is 1 or 0) yield return "MT";
                if (party is 2 or 0) yield return "OT";
                break;
            case "Healer":
                var healerColumns = jobs.Select(j => HealerColumns.GetValueOrDefault(j)).OfType<string>().ToList();
                foreach (var c in healerColumns.Count > 0 ? healerColumns : HealerColumns.Values.ToList()) yield return c;
                break;
            case "Melee":
                if (party is 1 or 0) yield return "Melee 1";
                if (party is 2 or 0) yield return "Melee 2";
                break;
            case "Ranged":
                if (party is 1 or 0) yield return "Phys Range";
                if (party is 2 or 0) yield return "Caster";
                break;
        }
    }

    public static string GridToCsv(List<List<string>> rows)
        => string.Join("\n", rows.Select(r => string.Join(",", r.Select(c => "\"" + c.Replace("\"", "\"\"") + "\""))));
}
