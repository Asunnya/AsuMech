using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AnoMech.Scenarios;

namespace AnoMech.Windows.Asu;

internal sealed record FightGroup(string Label, IReadOnlyList<IZone> Fights);

// The fight tabs in scope, grouped and ordered for the new main window.
internal static partial class CatalogView
{
    public const string Ultimate = "Ultimate";
    public const string Savage = "Savage";

    public static readonly IReadOnlyList<string> ExpansionOrder = ["Dawntrail", "Endwalker", "Shadowbringers", "Stormblood"];
    private static readonly IReadOnlyList<string> GroupOrder = [Ultimate, "Heavyweight", "Cruiserweight", "Light-heavyweight"];

    // Null expansion or type means all of them.
    public static IReadOnlyList<FightGroup> Groups(IEnumerable<IZone> zones, string? expansion, string? type)
        => zones
            .Where(z => expansion == null || z.Expansion == expansion)
            .Where(z => type == null || z.Category == type)
            .GroupBy(GroupOf)
            .OrderBy(g => Rank(g.Key)).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new FightGroup(g.Key, g.OrderBy(z => z.ShortName, NaturalComparer.Instance).ToList()))
            .ToList();

    public static IReadOnlyList<string> Expansions(IEnumerable<IZone> zones)
        => zones.Select(z => z.Expansion).OfType<string>().Distinct()
            .OrderBy(e => ExpansionOrder.IndexOf(e) is var i and >= 0 ? i : int.MaxValue)
            .ThenBy(e => e, StringComparer.Ordinal)
            .ToList();

    public static string GroupOf(IZone zone) => zone.Category == Ultimate ? Ultimate : zone.Tier ?? zone.Category;

    private static int Rank(string group) => GroupOrder.IndexOf(group) is var i and >= 0 ? i : GroupOrder.Count;

    private static int IndexOf(this IReadOnlyList<string> list, string value)
    {
        for (var i = 0; i < list.Count; i++)
            if (list[i] == value) return i;
        return -1;
    }

    // "M9S" before "M10S".
    internal sealed partial class NaturalComparer : IComparer<string>
    {
        public static readonly NaturalComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            var a = Chunks().Matches(x ?? "");
            var b = Chunks().Matches(y ?? "");
            for (var i = 0; i < Math.Min(a.Count, b.Count); i++)
            {
                var (ca, cb) = (a[i].Value, b[i].Value);
                var c = char.IsDigit(ca[0]) && char.IsDigit(cb[0])
                    ? long.Parse(ca).CompareTo(long.Parse(cb))
                    : string.Compare(ca, cb, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
            }
            return a.Count.CompareTo(b.Count);
        }

        [GeneratedRegex(@"\d+|\D+")]
        private static partial Regex Chunks();
    }
}
