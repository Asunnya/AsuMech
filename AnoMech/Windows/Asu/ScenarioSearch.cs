using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Scenarios;

namespace AnoMech.Windows.Asu;

// The selector popup's list: recent picks first when there's no query, else every match.
internal static class ScenarioSearch
{
    public const int RecentScenarioLimit = 3;
    public const int MaxResults = 40;

    public static IReadOnlyList<IScenario> Matches(IEnumerable<IScenario> scenarios, string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return scenarios
            .Where(s => terms.All(t => Haystack(s).Contains(t, StringComparison.OrdinalIgnoreCase)))
            .Take(MaxResults)
            .ToList();
    }

    public static IReadOnlyList<IScenario> Recent(IEnumerable<IScenario> scenarios, IReadOnlyList<string> recentFullNames)
    {
        var byName = scenarios.GroupBy(s => s.FullName()).ToDictionary(g => g.Key, g => g.First());
        return recentFullNames.Select(n => byName.GetValueOrDefault(n)).OfType<IScenario>().ToList();
    }

    // Most recent first, no duplicates, at most RecentScenarioLimit.
    public static List<string> PushRecent(IReadOnlyList<string> recent, string fullName)
        => recent.Where(n => n != fullName).Prepend(fullName).Take(RecentScenarioLimit).ToList();

    private static string Haystack(IScenario s) => $"{s.Phase.Zone.ShortName} {s.Phase.Zone.Name} {s.DisplayName()}";
}
