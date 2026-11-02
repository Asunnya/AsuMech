using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnoMech.Learn.Mitigation;

// One sheet tab (CSV) into a MitScenario, anchoring each row on the scenario's cast timeline and
// reading each cell through the alias dictionary. Whatever it can't read stays as `unresolved`.
public static partial class MitSheetImport
{
    public static MitScenario Import(string csv, CastTimeline timeline, MitAliases aliases)
    {
        var rows = ParseCsv(csv);
        var headerIndex = rows.FindIndex(r => r.Any(c => c.Trim() == "MT") && r.Any(c => c.Trim() == "OT"));
        if (headerIndex < 0) throw new InvalidOperationException("no MT/OT header row");
        var header = rows[headerIndex];
        var columnOf = new Dictionary<int, string>();
        for (var i = 0; i < header.Count; i++)
        {
            var name = Canonical(header[i].Trim());
            if (name != null) columnOf[i] = name;
        }
        var labelCol = header.FindIndex(c => c.Trim() == "MT") - 1;

        var mechanics = new List<MitMechanic>();
        var occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Skip(headerIndex + 1))
        {
            if (labelCol < 0 || labelCol >= row.Count) continue;
            var label = Clean(row[labelCol]);
            if (label.Length == 0 || label.StartsWith("Notes", StringComparison.OrdinalIgnoreCase) || label == "Seraph" || label == "xx") continue;
            if (!row.Where((c, i) => i != labelCol && c.Trim().Length > 0).Any()) continue;

            var (anchor, anchorError) = Anchor(label, timeline, occurrences);
            var columns = new Dictionary<string, List<MitItem>>();
            foreach (var (i, column) in columnOf)
            {
                if (i >= row.Count) continue;
                var cell = row[i].Trim();
                if (cell.Length == 0) continue;
                columns[column] = Items(cell, aliases);
            }
            var id = Slug(label) + (anchor is { Occurrence: > 1 } a ? $"-{a.Occurrence}" : "");
            while (mechanics.Any(m => m.Id == id)) id += "-b";
            mechanics.Add(new MitMechanic(id, label, anchor, columns, anchorError));
        }
        return new MitScenario(mechanics);
    }

    // "Downburst & Spiny" -> the cast named Downburst; a label the aliases name directly first; "Friction #2" -> its second occurrence; a
    // repeated label counts up on its own.
    private static (MitAnchor? Anchor, string? Error) Anchor(string label, CastTimeline timeline, Dictionary<string, int> occurrences)
    {
        var numbered = NumberedLabel().Match(label);
        var explicitOccurrence = numbered.Success ? int.Parse(numbered.Groups[1].Value) : (int?)null;
        var baseLabel = NumberedLabel().Replace(label, "").Replace("(x8)", "").Replace("(x6)", "").Replace("(x7)", "").Trim();
        var names = timeline.Names.OrderByDescending(n => n.Length).ToList();
        var aliased = MitAliases.Default.Mechanics.Keys.FirstOrDefault(k => baseLabel.StartsWith(k, StringComparison.OrdinalIgnoreCase));
        if (aliased != null)
        {
            var target = MitAliases.Default.Mechanics[aliased];
            var at = target.LastIndexOf('#');
            var castName = at > 0 ? target[..at].Trim() : target;
            explicitOccurrence ??= at > 0 ? int.Parse(target[(at + 1)..]) : null;
            baseLabel = castName;
        }
        var name = names.FirstOrDefault(n => baseLabel.StartsWith(n, StringComparison.OrdinalIgnoreCase))
                   ?? names.FirstOrDefault(n => baseLabel.Contains(n, StringComparison.OrdinalIgnoreCase));
        if (name is null) return (null, $"no cast named like \"{label}\"");
        var occurrence = explicitOccurrence ?? (occurrences[name] = occurrences.GetValueOrDefault(name) + 1);
        if (explicitOccurrence is { } e) occurrences[name] = Math.Max(occurrences.GetValueOrDefault(name), e);
        return timeline.Find(name, occurrence) is null
            ? (null, $"{name} has no occurrence {occurrence}")
            : (new MitAnchor(name, occurrence), null);
    }

    // A cell holds one entry per line; "A & B" or "A, B" on one line are alternatives.
    public static List<MitItem> Items(string cell, MitAliases aliases)
    {
        var items = new List<MitItem>();
        foreach (var rawLine in MergeContinuations(cell, aliases))
        {
            var line = rawLine;
            if (aliases.Reminders.Any(r => line.Contains(r, StringComparison.OrdinalIgnoreCase)))
            {
                items.Add(new MitItem(Reminder: line));
                continue;
            }
            if (line.StartsWith("DO NOT", StringComparison.OrdinalIgnoreCase))
            {
                var forbidden = line[6..].Split(['/', ','], StringSplitOptions.RemoveEmptyEntries).Select(s => aliases.Resolve(s.Trim())).ToList();
                items.Add(forbidden.All(f => f != null)
                    ? new MitItem(Forbid: forbidden.SelectMany(f => f!).Distinct().ToList())
                    : new MitItem(Unresolved: line));
                continue;
            }
            var state = aliases.States.Keys.FirstOrDefault(k => line.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (state != null && line.Trim().Equals(state, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new MitItem(ExpectStatus: aliases.States[state]));
                continue;
            }
            string? when = null;
            foreach (var (pattern, condition) in aliases.Conditions)
                if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    when = condition;
                    line = line.Replace(pattern, "", StringComparison.OrdinalIgnoreCase).Trim();
                }
            string? note = null;
            var paren = line.IndexOf('(');
            if (paren > 0 && line.EndsWith(')'))
            {
                note = line[(paren + 1)..^1].Trim();
                line = line[..paren].Trim();
            }
            string? target = null;
            foreach (var (pattern, t) in aliases.Targets.OrderByDescending(kv => kv.Key.Length))
                if (line.EndsWith(" " + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    target = t;
                    line = line[..^(pattern.Length + 1)].Trim();
                    break;
                }
            var parts = line.Split([" & ", ", ", " / ", "/"], StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
            var accept = new List<string>();
            var unresolved = new List<string>();
            foreach (var part in parts)
            {
                var resolved = aliases.Resolve(part);
                if (resolved != null) accept.AddRange(resolved);
                else unresolved.Add(part);
            }
            // A later line that names no action is the author's remark on the line above.
            if (unresolved.Count > 0 && accept.Count == 0 && items.Count > 0 && !rawLine.StartsWith('('))
                items[^1] = items[^1] with { Note = items[^1].Note is { } n ? $"{n}; {rawLine}" : rawLine };
            else if (unresolved.Count > 0 && accept.Count == 0) items.Add(new MitItem(Unresolved: rawLine));
            else items.Add(new MitItem(Accept: accept.Distinct().ToList(), When: when, Target: target, Note: note,
                Unresolved: unresolved.Count > 0 ? string.Join(" / ", unresolved) : null));
        }
        return items;
    }

    // A cell wraps its text over several lines: "Asylum\n(After Castbar)", "Feint\n(Once 1st\nfalls off)",
    // "Plant Star\nafter Divination". A line that opens with a bracket or a lowercase word, or follows
    // an unclosed bracket, continues the previous one.
    private static List<string> MergeContinuations(string cell, MitAliases aliases)
    {
        var lines = new List<string>();
        foreach (var raw in cell.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            var continues = lines.Count > 0 && (raw[0] == '(' || char.IsLower(raw[0]) || lines[^1].Count(c => c == '(') > lines[^1].Count(c => c == ')'));
            if (continues) lines[^1] = $"{lines[^1]} {raw}";
            else lines.Add(raw);
        }
        // "Celestial" + "Opposition": a name wrapped mid-word.
        for (var i = 0; i + 1 < lines.Count; i++)
            if (aliases.Resolve(lines[i]) is null && aliases.Resolve($"{lines[i]} {lines[i + 1]}") is not null)
            {
                lines[i] = $"{lines[i]} {lines[i + 1]}";
                lines.RemoveAt(i + 1);
                i--;
            }
        return lines;
    }

    private static string? Canonical(string header) => header switch
    {
        "MT" => "MT", "OT" => "OT", "Scholar" => "Scholar", "Sage" => "Sage", "White Mage" => "White Mage",
        "Astro" or "Astrologian" => "Astrologian", "Melee 1" => "Melee 1", "Melee 2" => "Melee 2",
        "Phys Range" or "Phys Ranged" => "Phys Range", "Caster" => "Caster", "Extras" => "Extras", _ => null,
    };

    private static string Clean(string label) => string.Join(' ', label.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));

    private static string Slug(string label) => Regex.Replace(label.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    [GeneratedRegex(@"#\s*(\d+)")]
    private static partial Regex NumberedLabel();

    public static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else cell.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { row.Add(cell.ToString()); cell.Clear(); }
            else if (c == '\n') { row.Add(cell.ToString()); cell.Clear(); rows.Add(row); row = []; }
            else if (c != '\r') cell.Append(c);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
        return rows;
    }
}
