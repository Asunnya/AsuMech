using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AsuMech.Learn;

// A family's *State members and their values in a player's words, from
// Learn/Lessons/<Family>/glossary.json: "fields" maps a member name to a phrase, "values" maps a
// member name (or "*" for any) to value → phrase. The draft reads it; nothing in game does.
public sealed class Glossary
{
    private const string ResourcePrefix = "AsuMech.Learn.Lessons.";
    public const string FileName = "glossary.json";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    private sealed record File(Dictionary<string, string>? Fields, Dictionary<string, Dictionary<string, string>>? Values);

    public IReadOnlyDictionary<string, string> Fields { get; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Values { get; }

    public static readonly Glossary Empty = new(new(), new());

    private Glossary(Dictionary<string, string> fields, Dictionary<string, IReadOnlyDictionary<string, string>> values)
    {
        Fields = fields;
        Values = values;
    }

    public static Glossary Parse(string json)
    {
        var file = JsonSerializer.Deserialize<File>(json, Json) ?? new File(null, null);
        return new Glossary(file.Fields ?? new(), (file.Values ?? new()).ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<string, string>)kv.Value));
    }

    // The family is the scenario namespace's third segment (AsuMech.Scenarios.Uwu).
    public static Glossary Load(string family)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"{ResourcePrefix}{family}.{FileName}");
        if (stream is null) return Empty;
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static string FamilyOf(Type scenarioType) => scenarioType.Namespace?.Split('.').ElementAtOrDefault(2) ?? "";

    public string Field(string name) => Fields.TryGetValue(name, out var words) ? words : name;

    public string Value(string field, string value)
        => Values.TryGetValue(field, out var byValue) && byValue.TryGetValue(value, out var words) ? words
            : Values.TryGetValue("*", out var any) && any.TryGetValue(value, out var common) ? common : value;

    // Every known member name in a sentence replaced by its phrase, longest names first so a
    // member whose name contains another's is not half-translated.
    public string Apply(string text)
    {
        foreach (var name in Fields.Keys.OrderByDescending(n => n.Length))
            text = Regex.Replace(text, $@"\b{Regex.Escape(name)}\b", Fields[name]);
        return text;
    }
}
