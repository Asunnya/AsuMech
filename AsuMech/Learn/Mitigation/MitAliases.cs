using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AsuMech.Learn.Mitigation;

// Community names for actions, conditions and states, as the sheets write them. Grows with every
// sheet imported.
public sealed class MitAliases
{
    public Dictionary<string, List<string>> Actions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Reminders { get; init; } = [];
    public Dictionary<string, string> Conditions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> States { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Targets { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    // Row label -> cast name, optionally "#n" for the occurrence.
    public Dictionary<string, string> Mechanics { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<MitAliases> Embedded = new(() =>
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("AsuMech.Learn.Mitigation.Aliases.json")!;
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    });

    public static MitAliases Default => Embedded.Value;

    public static MitAliases Parse(string json)
    {
        var raw = JsonSerializer.Deserialize<MitAliases>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return new MitAliases
        {
            Actions = new Dictionary<string, List<string>>(raw.Actions, StringComparer.OrdinalIgnoreCase),
            Reminders = raw.Reminders,
            Conditions = new Dictionary<string, string>(raw.Conditions, StringComparer.OrdinalIgnoreCase),
            States = new Dictionary<string, string>(raw.States, StringComparer.OrdinalIgnoreCase),
            Targets = new Dictionary<string, string>(raw.Targets, StringComparer.OrdinalIgnoreCase),
            Mechanics = new Dictionary<string, string>(raw.Mechanics, StringComparer.OrdinalIgnoreCase),
        };
    }

    // Catalog names for a community name, longest alias first; the catalog's own names count too.
    public List<string>? Resolve(string text)
    {
        var trimmed = text.Trim();
        if (Actions.TryGetValue(trimmed, out var direct)) return direct;
        if (MitCatalog.Find(trimmed) is { } exact) return [exact.Name];
        foreach (var alias in Actions.Keys.OrderByDescending(k => k.Length))
            if (trimmed.StartsWith(alias, StringComparison.OrdinalIgnoreCase)) return Actions[alias];
        foreach (var action in MitCatalog.All.OrderByDescending(a => a.Name.Length))
            if (trimmed.StartsWith(action.Name, StringComparison.OrdinalIgnoreCase)) return [action.Name];
        return null;
    }
}
