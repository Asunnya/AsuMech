using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AnoMech.Learn.Sources;

// One public hint module (a folder of C# files) read as text: its enums with their trailing
// comments, the action facts those comments carry, the AoE shapes the code builds, and the state
// machine's steps. No compiler: the two sources share a shape of file that regular expressions
// cover, and whatever they don't cover is listed in Unparsed instead of guessed. Each is a second
// opinion for crosscheck and a name for an id the game leaves nameless; never the sim's truth.
public sealed record ModuleEnumEntry(string Name, long Value, string? Comment);

// From an AID comment, "Boss->self, 2.0+4.0s cast, range 60 90-degree cone, tankbuster": the
// shape words are the sheet's, as the module author read them off the game.
public sealed record ModuleActionFact(string Name, uint Id, string? Caster, string? Target, float? CastSeconds, float? CastExtraSeconds, ModuleShape? Shape, string? Note);

// Range is the reach (circle radius, cone length, rect length, donut outer); Angle a cone's full
// angle in degrees; Width a rect's or cross's full width; Inner a donut's hole radius.
public sealed record ModuleShape(string Kind, float? Range, float? Angle = null, float? Width = null, float? Inner = null);

// A shape the code builds, bound to the actions named in the same statement.
public sealed record ModuleCodeShape(IReadOnlyList<string> Actions, ModuleShape Shape, string File, int Line);

// One state machine step: the builder call, the actions it watches, its numbers in order (the
// delay from the previous step first, a cast's length second), its label and hint.
public sealed record ModuleState(string Phase, string Kind, IReadOnlyList<string> Actions, IReadOnlyList<float> Numbers, string? Name, string? Hint, string File, int Line);

public sealed record ModuleSourceBlock(
    string Name, string Repo, string Path, string Commit, string License,
    Dictionary<string, List<ModuleEnumEntry>> Enums,
    List<ModuleActionFact> Actions,
    List<ModuleCodeShape> Shapes,
    List<ModuleState> States,
    List<string> Unparsed)
{
    public IReadOnlyList<ModuleEnumEntry> Enum(string name) => Enums.TryGetValue(name, out var list) ? list : [];

    public ModuleActionFact? Action(uint id) => Actions.FirstOrDefault(a => a.Id == id);

    public string? ActionName(uint id) => Action(id)?.Name;

    // Shapes the code binds to an action, by its enum name.
    public IEnumerable<ModuleShape> ShapesFor(string actionName)
        => Shapes.Where(s => s.Actions.Contains(actionName, StringComparer.Ordinal)).Select(s => s.Shape);

    public string? IconName(long value) => Enum("IconID").FirstOrDefault(e => e.Value == value)?.Name;
    public string? TetherName(long value) => Enum("TetherID").FirstOrDefault(e => e.Value == value)?.Name;
    public string? StatusName(long value) => Enum("SID").FirstOrDefault(e => e.Value == value)?.Name;
}

public sealed class ModuleSource
{
    private const string ResourcePrefix = "AnoMech.Learn.Sources.modules.";
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string Fight { get; init; } = "";
    public List<ModuleSourceBlock> Sources { get; init; } = [];

    public static ModuleSource Parse(string json) => JsonSerializer.Deserialize<ModuleSource>(json, Json) ?? new ModuleSource();

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static ModuleSource? Load(string fight)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{ResourcePrefix}{fight}.json");
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static IEnumerable<string> Fights()
        => Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && !n.EndsWith(".fights.json", StringComparison.Ordinal))
            .Select(n => n[ResourcePrefix.Length..^".json".Length]);

    // The family's folder name, lower-cased, is the fight key (Scenarios/M11s -> m11s).
    public static ModuleSource? LoadForFamily(string family) => family.Length == 0 ? null : Load(family.ToLowerInvariant());

    // The first source naming the value wins; the others are listed when they disagree.
    public string? IconName(long value) => Sources.Select(s => s.IconName(value)).FirstOrDefault(n => n != null);
    public string? TetherName(long value) => Sources.Select(s => s.TetherName(value)).FirstOrDefault(n => n != null);

    // ── Parsing ───────────────────────────────────────────────────────────────

    public static ModuleSourceBlock ParseFolder(string name, string repo, string path, string commit, string license, IEnumerable<(string File, string Text)> files)
    {
        var block = new ModuleSourceBlock(name, repo, path, commit, license, new(StringComparer.Ordinal), [], [], [], []);
        foreach (var (file, text) in files.OrderBy(f => f.File, StringComparer.Ordinal))
        {
            ParseEnums(block, file, text);
            ParseShapes(block, file, text);
            ParseStates(block, file, text);
        }
        foreach (var entry in block.Enum("AID"))
            block.Actions.Add(ParseActionFact(entry));
        return block;
    }

    private static readonly Regex EnumHeader = new(@"^\s*(?:public\s+)?enum\s+(\w+)\s*(?::\s*\w+)?\s*$", RegexOptions.Multiline);
    private static readonly Regex EnumMember = new(@"^\s*(\w+)\s*=\s*(0x[0-9A-Fa-f]+|\d+)\s*,?\s*(?://\s*(.*?))?\s*$");

    private static void ParseEnums(ModuleSourceBlock block, string file, string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var header = EnumHeader.Match(lines[i]);
            if (!header.Success) continue;
            var list = block.Enums.TryGetValue(header.Groups[1].Value, out var existing) ? existing : block.Enums[header.Groups[1].Value] = [];
            var j = i + 1;
            while (j < lines.Length && !lines[j].Contains('{')) j++;
            for (j++; j < lines.Length && !lines[j].TrimStart().StartsWith('}'); j++)
            {
                var line = lines[j].TrimEnd('\r');
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("//")) continue;
                var m = EnumMember.Match(line);
                if (!m.Success) { block.Unparsed.Add($"{file}:{j + 1}: {line.Trim()}"); continue; }
                var raw = m.Groups[2].Value;
                var value = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? long.Parse(raw[2..], NumberStyles.HexNumber) : long.Parse(raw, CultureInfo.InvariantCulture);
                list.Add(new ModuleEnumEntry(m.Groups[1].Value, value, m.Groups[3].Success && m.Groups[3].Value.Length > 0 ? m.Groups[3].Value : null));
            }
            i = j;
        }
    }

    private static readonly Regex ActionComment = new(@"^(?<caster>[\w?]+)->(?<target>[\w?/]+),\s*(?:(?<nocast>no cast)|(?<cast>[\d.]+)(?:\+(?<extra>[\d.]+))?s cast)\s*(?:,\s*(?<rest>.*))?$");
    private static readonly Regex ShapeWords = new(@"^(?:single-target|range\s+(?<range>[\d.]+)(?:-(?<outer>[\d.]+))?\s+(?:(?<circle>circle)|(?<angle>[\d.?]+)-degree\s+cone|width\s+(?<width>[\d.]+)\s+(?<rect>rect|cross)|(?<donut>donut)))\s*(?<note>.*)$");

    // "Boss->self, 2.0+4.0s cast, range 60 90-degree cone, tankbuster ; note". A comment of another
    // shape gives a fact with the name and id only, and goes to Unparsed.
    public static ModuleActionFact ParseActionFact(ModuleEnumEntry entry)
    {
        var id = (uint)entry.Value;
        if (entry.Comment is null) return new ModuleActionFact(entry.Name, id, null, null, null, null, null, null);
        var m = ActionComment.Match(entry.Comment.Trim());
        if (!m.Success) return new ModuleActionFact(entry.Name, id, null, null, null, null, null, entry.Comment.Trim());
        float? cast = m.Groups["nocast"].Success ? 0f : Num(m.Groups["cast"].Value);
        float? extra = m.Groups["extra"].Success ? Num(m.Groups["extra"].Value) : null;
        var rest = m.Groups["rest"].Value.Trim();
        ModuleShape? shape = null;
        string? note = rest.Length > 0 ? rest : null;
        var s = ShapeWords.Match(rest);
        if (s.Success)
        {
            if (s.Groups["range"].Success)
            {
                var range = Num(s.Groups["range"].Value);
                shape = s.Groups["circle"].Success ? new ModuleShape("circle", range)
                    : s.Groups["angle"].Success ? new ModuleShape("cone", range, Angle: s.Groups["angle"].Value == "?" ? null : Num(s.Groups["angle"].Value))
                    : s.Groups["rect"].Success ? new ModuleShape(s.Groups["rect"].Value, range, Width: Num(s.Groups["width"].Value))
                    : new ModuleShape("donut", s.Groups["outer"].Success ? Num(s.Groups["outer"].Value) : range, Inner: s.Groups["outer"].Success ? range : null);
            }
            else shape = new ModuleShape("single-target", null);
            note = s.Groups["note"].Value.Trim(' ', ',', ';').Trim();
            if (note.Length == 0) note = null;
        }
        return new ModuleActionFact(entry.Name, id, m.Groups["caster"].Value, m.Groups["target"].Value, cast, extra, shape, note);
    }

    private static readonly Regex ShapeNew = new(@"new\s+AOEShape(?<kind>Cone|Circle|Rect|Donut|Cross|DonutSector|Capsule)\s*\((?<args>[^()]*(?:\([^()]*\)[^()]*)*)\)");
    private static readonly Regex AidRef = new(@"\bAID\.(\w+)");
    // `base(module, AID.X, radius, ...)` is a subclass of one of these; its first number is read the same way.
    private static readonly Regex RadiusComponent = new(@"\b(?<comp>base|SimpleAOEs|StandardAOEs|SpreadFromIcon|SpreadFromCastTargets|StackWithCastTargets|StackWithIcon|StackWithCastTargetsHits|SimpleKnockbacks|Knockback|Voidzone|BaitAwayCast|LineStack|CastTowers|GenericTowers|Towers)\s*\((?<args>[^()]*(?:\([^()]*\)[^()]*)*)\)");
    private static readonly Regex Number = new(@"(?<![\w.])(?<num>\d+(?:\.\d+)?)[fd]?(?<deg>\.Degrees\(\))?");

    private static readonly Regex ShapeField = new(@"AOEShape(?<kind>Cone|Circle|Rect|Donut|Cross|DonutSector|Capsule)\s+(?<ident>\w+)\s*=\s*new\s*(?:AOEShape\w+)?\s*\((?<args>[^()]*(?:\([^()]*\)[^()]*)*)\)");

    // Every statement (up to a `;` or the end of a class header line) that names an action and
    // builds a shape, names a field holding one, or passes a radius to a component whose first
    // number is one.
    private static void ParseShapes(ModuleSourceBlock block, string file, string text)
    {
        var fields = new Dictionary<string, ModuleShape>(StringComparer.Ordinal);
        foreach (Match f in ShapeField.Matches(text))
            if (ShapeFromArgs(f.Groups["kind"].Value, f.Groups["args"].Value) is { } fieldShape) fields[f.Groups["ident"].Value] = fieldShape;
        var lines = text.Split('\n');
        var statement = new System.Text.StringBuilder();
        var startLine = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var code = StripComment(line);
            if (statement.Length == 0) startLine = i + 1;
            statement.Append(code).Append(' ');
            var trimmed = code.TrimEnd();
            var ends = trimmed.EndsWith(';') || trimmed.EndsWith('{') || trimmed.EndsWith('}') || trimmed.Length == 0 || i == lines.Length - 1;
            if (!ends) continue;
            var s = statement.ToString();
            statement.Clear();
            if (!s.Contains("AID.", StringComparison.Ordinal)) continue;
            var actions = AidRef.Matches(s).Select(m => m.Groups[1].Value).Distinct().ToList();
            var found = false;
            foreach (Match m in ShapeNew.Matches(s))
            {
                found = true;
                if (ShapeFromArgs(m.Groups["kind"].Value, m.Groups["args"].Value) is { } shape)
                    block.Shapes.Add(new ModuleCodeShape(actions, shape, file, startLine));
                else block.Unparsed.Add($"{file}:{startLine}: {m.Value}");
            }
            foreach (var (ident, fieldShape) in fields)
                if (Regex.IsMatch(s, $@"\b{Regex.Escape(ident)}\b") && !s.Contains($" {ident} =", StringComparison.Ordinal))
                {
                    found = true;
                    block.Shapes.Add(new ModuleCodeShape(actions, fieldShape, file, startLine));
                }
            if (found) continue;
            foreach (Match m in RadiusComponent.Matches(s))
            {
                var args = m.Groups["args"].Value;
                // The first number after the last action reference is the radius; LineStack's first is the activation delay.
                var afterActions = args[(args.LastIndexOf("AID.", StringComparison.Ordinal) is var at and >= 0 ? at : 0)..];
                var numbers = Numbers(afterActions).ToList();
                if (m.Groups["comp"].Value == "LineStack")
                {
                    if (numbers.Count >= 3) block.Shapes.Add(new ModuleCodeShape(actions, new ModuleShape("rect", numbers[1].Value, Width: numbers[2].Value * 2), file, startLine));
                    else block.Unparsed.Add($"{file}:{startLine}: {m.Value}");
                }
                else if (m.Groups["comp"].Value is "SimpleKnockbacks" or "Knockback")
                {
                    if (numbers.Count >= 1) block.Shapes.Add(new ModuleCodeShape(actions, new ModuleShape("knockback", numbers[0].Value), file, startLine));
                }
                else if (numbers.Count >= 1 && !numbers[0].Degrees)
                    block.Shapes.Add(new ModuleCodeShape(actions, new ModuleShape(m.Groups["comp"].Value.Contains("Tower") ? "tower" : "circle", numbers[0].Value), file, startLine));
                else block.Unparsed.Add($"{file}:{startLine}: {m.Value}");
            }
        }
    }

    private static ModuleShape? ShapeFromArgs(string kind, string args)
    {
        var numbers = Numbers(args).ToList();
        var plain = numbers.Where(n => !n.Degrees).Select(n => n.Value).ToList();
        var degrees = numbers.Where(n => n.Degrees).Select(n => n.Value).ToList();
        switch (kind)
        {
            case "Circle": return plain.Count >= 1 ? new ModuleShape("circle", plain[0]) : null;
            // (radius, halfAngle.Degrees())
            case "Cone": return plain.Count >= 1 && degrees.Count >= 1 ? new ModuleShape("cone", plain[0], Angle: degrees[0] * 2) : null;
            // (lengthFront, halfWidth[, lengthBack])
            case "Rect": return plain.Count >= 2 ? new ModuleShape("rect", plain[0] + (plain.Count >= 3 ? plain[2] : 0f), Width: plain[1] * 2) : null;
            // (length, halfWidth)
            case "Cross": return plain.Count >= 2 ? new ModuleShape("cross", plain[0], Width: plain[1] * 2) : null;
            // (inner, outer)
            case "Donut": return plain.Count >= 2 ? new ModuleShape("donut", plain[1], Inner: plain[0]) : null;
            case "DonutSector": return plain.Count >= 2 && degrees.Count >= 1 ? new ModuleShape("donut-sector", plain[1], Angle: degrees[0] * 2, Inner: plain[0]) : null;
            case "Capsule": return plain.Count >= 2 ? new ModuleShape("capsule", plain[0], Width: plain[1] * 2) : null;
            default: return null;
        }
    }

    private static IEnumerable<(float Value, bool Degrees)> Numbers(string text)
        => Number.Matches(text).Select(m => (Num(m.Groups["num"].Value), m.Groups["deg"].Success));

    private static readonly Regex PhaseMethod = new(@"^\s*(?:private\s+|public\s+|internal\s+)?void\s+(\w+)\s*\(\s*uint\s+id\b");
    private static readonly Regex StateCall = new(@"^\s*(?<kind>Cast|CastStart|CastEnd|CastMulti|CastStartMulti|CastEndMulti|ActorCast|ActorCastStart|ActorCastEnd|ActorCastMulti|ComponentCondition<\w+>|Condition|SimpleState|Timeout|Targetable|ActorTargetable|DeathPhase|SimplePhase)\s*\(");
    private static readonly Regex HintCall = new(@"\.SetHint\(\s*StateMachine\.StateHint\.(\w+)");
    private static readonly Regex Quoted = new("\"([^\"]*)\"");

    private static void ParseStates(ModuleSourceBlock block, string file, string text)
    {
        if (!text.Contains("StateMachineBuilder", StringComparison.Ordinal)) return;
        var lines = text.Split('\n');
        var phase = "";
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (PhaseMethod.Match(line) is { Success: true } pm) { phase = pm.Groups[1].Value; continue; }
            var call = StateCall.Match(line);
            if (!call.Success) continue;
            // The call's arguments run to the parenthesis that closes it, however many lines that takes.
            var startLine = i + 1;
            var buffer = new System.Text.StringBuilder();
            var depth = 0;
            var closed = false;
            for (var j = i; j < lines.Length && !closed; j++)
            {
                foreach (var c in StripComment(lines[j]))
                {
                    buffer.Append(c);
                    if (c == '(') depth++;
                    else if (c == ')' && --depth == 0) { closed = true; break; }
                }
                buffer.Append(' ');
                if (!closed) i = j;
            }
            var statement = buffer.ToString();
            var open = statement.IndexOf('(');
            var args = statement[(open + 1)..statement.LastIndexOf(')')];
            // The id expression is the first argument; its hex offsets aren't timings.
            var comma = TopLevelComma(args);
            var rest = comma < 0 ? "" : args[(comma + 1)..];
            var actions = AidRef.Matches(rest).Select(m => m.Groups[1].Value).Distinct().ToList();
            var numbers = Numbers(Quoted.Replace(rest, "\"\"")).Where(n => !n.Degrees).Select(n => n.Value).ToList();
            var name = Quoted.Match(rest) is { Success: true } q ? q.Groups[1].Value : null;
            // The hint rides on the chained calls that follow the step.
            string? hint = null;
            for (var k = i + 1; k < lines.Length && lines[k].TrimStart().StartsWith('.'); k++)
                if (HintCall.Match(lines[k]) is { Success: true } h) { hint = h.Groups[1].Value; break; }
            var kind = call.Groups["kind"].Value;
            if (kind is "DeathPhase" or "SimplePhase") continue;
            block.States.Add(new ModuleState(phase, kind, actions, numbers, name, hint, file, startLine));
        }
    }

    private static int TopLevelComma(string args)
    {
        var depth = 0;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is '(' or '<') depth++;
            else if (args[i] is ')' or '>') depth--;
            else if (args[i] == ',' && depth == 0) return i;
        }
        return -1;
    }

    private static string StripComment(string line)
    {
        var at = line.IndexOf("//", StringComparison.Ordinal);
        return at < 0 ? line : line[..at];
    }

    private static float Num(string text) => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
}

// Learn/Sources/modules/fights.json: which folder of which public repo holds each fight, at the
// commit the embedded JSON was made from. crosscheck reads it to fetch, and rewrites the pins on --update.
public sealed class ModuleFights
{
    public sealed record SourceRepo(string Repo, string License);
    public sealed record FightPath(string Path, string Commit);
    public sealed record Fight(string Family, Dictionary<string, FightPath> Paths);

    public Dictionary<string, SourceRepo> Sources { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, Fight> Fights { get; init; } = new(StringComparer.Ordinal);

    public static ModuleFights Parse(string json) => JsonSerializer.Deserialize<ModuleFights>(json, ModuleSource.Json) ?? new ModuleFights();

    public string ToJson() => JsonSerializer.Serialize(this, ModuleSource.Json);

    public static ModuleFights Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AnoMech.Learn.Sources.modules.fights.json");
        if (stream is null) return new ModuleFights();
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }
}

