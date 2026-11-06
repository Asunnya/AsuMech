using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsuMech.Learn.Mitigation;

public sealed record MitSource(string Title, string Url, List<string> Credits, string ImportedAt);

public sealed record MitAnchor(string Cast, int Occurrence = 1);

// One cell of the sheet. Accept: any of these actions satisfies it. When: beforeSnapshot (default),
// afterCastEnd, afterAction:<name>, afterStatusEnds:<name>. Target: MT, OT, self, party, or a
// mechanic role. Unresolved keeps the text the importer couldn't read; a plan with any doesn't ship.
public sealed record MitItem(
    List<string>? Accept = null,
    string? When = null,
    string? Target = null,
    List<string>? Forbid = null,
    string? ExpectStatus = null,
    string? Reminder = null,
    string? Note = null,
    string? Unresolved = null,
    int MinCount = 1);

public sealed record MitMechanic(string Id, string Label, MitAnchor? Anchor, Dictionary<string, List<MitItem>> Columns, string? Unresolved = null);

public sealed record MitScenario(List<MitMechanic> Mechanics);

// KnownUnresolved: rows the author read and accepted as having no cast to anchor on (a cutscene,
// an order rolled per run, a part the scenario does not simulate); anything else unresolved fails CI.
public sealed record MitPlan(string Plan, MitSource Source, Dictionary<string, MitScenario> Scenarios, List<string>? KnownUnresolved = null)
{
    public IEnumerable<string> UnknownUnresolved() => UnresolvedTexts().Where(u => !(KnownUnresolved ?? []).Contains(u));

    public static readonly string[] Columns = ["MT", "OT", "Scholar", "Sage", "White Mage", "Astrologian", "Melee 1", "Melee 2", "Phys Range", "Caster"];

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public string ToJson() => JsonSerializer.Serialize(this, Json);
    public static MitPlan Parse(string json) => JsonSerializer.Deserialize<MitPlan>(json, Json)!;

    public IEnumerable<string> UnresolvedTexts()
    {
        foreach (var scenario in Scenarios.Values)
        foreach (var mech in scenario.Mechanics)
        {
            if (mech.Unresolved != null) yield return $"{mech.Label}: {mech.Unresolved}";
            foreach (var (column, items) in mech.Columns)
            foreach (var item in items)
                if (item.Unresolved != null) yield return $"{mech.Label} / {column}: {item.Unresolved}";
        }
    }
}
