using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnoMech.Core.Game.Ai;
using AnoMech.Learn.Tools;
using AnoMech.Scenarios;

namespace AnoMech.Learn;

// One finished run: the values its pinnable rolls took and whether it was clean.
public sealed class RollRun
{
    public Dictionary<string, string> Rolls { get; set; } = [];
    public bool Clean { get; set; }
    public DateTime When { get; set; }
}

public sealed record RollVariant(string Field, string Value, int Runs, int Fails)
{
    public float FailRate => Runs == 0 ? 0f : Fails / (float)Runs;
}

// Which values of a scenario's rolls the player fails on, per scenario and strat: every roll the
// scenario's *StateOverrides can pin, recorded at the end of each run. The worst one can be pinned
// for practice while the others stay random.
public sealed class RollHistory(IDictionary<string, List<RollRun>> runs, Action save)
{
    public const int Kept = 200;
    public const int MinRunsForWorst = 3;

    public static string Key(IScenario scenario, IScenarioAi? strat) => $"{scenario.GetType().Name}|{strat?.Name ?? "-"}";

    public void Record(string key, Dictionary<string, string> rolls, bool clean)
    {
        if (rolls.Count == 0) return;
        if (!runs.TryGetValue(key, out var list)) runs[key] = list = [];
        list.Add(new RollRun { Rolls = rolls, Clean = clean, When = DateTime.UtcNow });
        if (list.Count > Kept) list.RemoveRange(0, list.Count - Kept);
        save();
    }

    public IReadOnlyList<RollRun> Runs(string key) => runs.TryGetValue(key, out var list) ? list : [];

    public List<RollVariant> Variants(string key)
        => Runs(key).SelectMany(r => r.Rolls.Select(kv => (kv.Key, kv.Value, r.Clean)))
            .GroupBy(x => (x.Key, x.Value))
            .Select(g => new RollVariant(g.Key.Key, g.Key.Value, g.Count(), g.Count(x => !x.Clean)))
            .OrderBy(v => v.Field, StringComparer.Ordinal).ThenByDescending(v => v.FailRate).ThenBy(v => v.Value, StringComparer.Ordinal)
            .ToList();

    // The value failed most often, among those seen often enough to say; a field that never
    // varied (one value only) has nothing to practise against and is left out.
    public RollVariant? Worst(string key, int minRuns = MinRunsForWorst)
    {
        var variants = Variants(key);
        var varying = variants.GroupBy(v => v.Field).Where(g => g.Count() > 1).SelectMany(g => g);
        return varying.Where(v => v.Runs >= minRuns && v.Fails > 0)
            .OrderByDescending(v => v.FailRate).ThenByDescending(v => v.Runs).FirstOrDefault();
    }

    // The scenario's rolls its overrides can pin, by the State's names and spelling.
    public static Dictionary<string, string> Pinnable(IScenario scenario)
    {
        if (scenario.SettingsOverrides is not { } overrides) return [];
        var names = overrides.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return StateSnapshot.Of(scenario).Where(kv => names.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
