using System.Collections.Generic;
using AsuMech.Scenarios;

namespace AsuMech.Windows.Asu;

internal sealed record LearnEntry(string Notes, IReadOnlyList<(string Label, string Url)> Links);

// Per-scenario notes and guide links, keyed by IScenario.FullName(). Empty until written.
internal static class LearnContent
{
    private static readonly Dictionary<string, LearnEntry> Entries = new();

    public static LearnEntry? For(IScenario scenario) => Entries.GetValueOrDefault(scenario.FullName());
}
