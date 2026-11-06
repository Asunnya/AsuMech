using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnoMech.Learn.Mitigation;
using AnoMech.Scenarios;

namespace AnoMech.Learn.Tools;

// What a bump of the datamining commit changes under the catalog's feet: every EnemyAction's name,
// cast, shape, reach and recast, every mitigation action's recast, and every mitigation status's
// name and stacks, compared between two commits' Action and Status sheets. The sim reads those
// columns, so a change here is a change in what it does.
public sealed record PatchChange(string Sheet, uint Id, string Name, string Column, string From, string To, string UsedBy);

public static class PatchDiff
{
    // Column -> how it reads; cast and recast are tenths of a second in the sheet.
    private static readonly (string Column, string Label, Func<string, string> Show)[] ActionColumns =
    [
        ("Name", "name", s => s),
        ("Cast100ms", "cast", Tenths),
        ("CastType", "shape", s => int.TryParse(s, out var t) ? $"{t} ({(Core.Native.Interfaces.CastType)t})" : s),
        ("EffectRange", "range", s => s),
        ("XAxisModifier", "width", s => s),
        ("Recast100ms", "recast", Tenths),
        ("Omen", "omen", s => s),
    ];

    private static readonly (string Column, string Label, Func<string, string> Show)[] StatusColumns =
    [
        ("Name", "name", s => s),
        ("MaxStacks", "stacks", s => s),
    ];

    private static string Tenths(string s) => int.TryParse(s, out var v) ? (v / 10f).ToString("0.#", CultureInfo.InvariantCulture) + "s" : s;

    // Every action the catalog's EnemyActions name, by the field that names it; every mitigation action.
    public static Dictionary<uint, string> CatalogActions()
    {
        var result = new Dictionary<uint, string>();
        foreach (var zone in ScenarioCatalog.Create().Zones)
        {
            var family = zone.GetType().Namespace?.Split('.').ElementAtOrDefault(2) ?? "";
            foreach (var (name, action) in ModuleCrosscheck.FamilyActions(family))
                result[action.ActionId] = result.TryGetValue(action.ActionId, out var existing) ? $"{existing}, {family}.{name}" : $"{family}.{name}";
        }
        foreach (var mit in MitCatalog.All)
            result.TryAdd(mit.Id, $"mit {mit.Name}");
        return result;
    }

    public static Dictionary<uint, string> MitigationStatuses()
        => MitStatuses.All.GroupBy(s => (uint)s.StatusId).ToDictionary(g => g.Key, g => $"mit {string.Join("/", g.Select(s => s.Action).Distinct())}");

    public static List<PatchChange> Compare(DataminingSheet actionsFrom, DataminingSheet actionsTo, DataminingSheet statusesFrom, DataminingSheet statusesTo,
        IReadOnlyDictionary<uint, string> actionIds, IReadOnlyDictionary<uint, string> statusIds)
    {
        var changes = new List<PatchChange>();
        changes.AddRange(Compare(actionsFrom, actionsTo, actionIds, ActionColumns));
        changes.AddRange(Compare(statusesFrom, statusesTo, statusIds, StatusColumns));
        return changes;
    }

    private static IEnumerable<PatchChange> Compare(DataminingSheet from, DataminingSheet to, IReadOnlyDictionary<uint, string> ids, (string Column, string Label, Func<string, string> Show)[] columns)
    {
        foreach (var (id, usedBy) in ids.OrderBy(kv => kv.Key))
        {
            var before = from.Get(id);
            var after = to.Get(id);
            if (before is null && after is null) { yield return new PatchChange(from.Name, id, "", "row", "missing", "missing", usedBy); continue; }
            if (before is null) { yield return new PatchChange(from.Name, id, after!.Text("Name"), "row", "missing", "present", usedBy); continue; }
            if (after is null) { yield return new PatchChange(from.Name, id, before.Text("Name"), "row", "present", "missing", usedBy); continue; }
            foreach (var (column, label, show) in columns)
            {
                if (!before.Has(column) || !after.Has(column)) continue;
                var a = before.Text(column);
                var b = after.Text(column);
                if (a != b) yield return new PatchChange(from.Name, id, after.Text("Name"), label, show(a), show(b), usedBy);
            }
        }
    }

    public static string Table(IReadOnlyList<PatchChange> changes)
    {
        if (changes.Count == 0) return "no change in the catalog's actions and mitigation statuses\n";
        var sb = new System.Text.StringBuilder();
        foreach (var c in changes)
            sb.AppendLine($"{c.Sheet,-7} {c.Id,6} {c.Name,-32} {c.Column,-7} {c.From,-18} -> {c.To,-18}  {c.UsedBy}");
        sb.AppendLine($"{changes.Count} change(s)");
        return sb.ToString();
    }
}
