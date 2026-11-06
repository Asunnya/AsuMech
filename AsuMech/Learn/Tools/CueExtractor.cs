using System;
using System.Collections.Generic;
using System.Linq;

namespace AsuMech.Learn.Tools;

// A visible event that preceded a step's order in the recorded runs: how long before, how often
// across seeds, and whether it singled out the role (a marker, tether or status on them).
public sealed record CueCandidate(string Kind, string Name, string? Target, float SecondsBefore, int Seeds, int OfSeeds)
{
    public bool Stable => Seeds == OfSeeds;
}

// Per step and role, what the player could have seen in the window before the order: the last
// stable event is the cue; anything aimed at the role itself is what the text should name.
public static class CueExtractor
{
    public const float WindowSeconds = 8f;

    public static Dictionary<(string Cls, int Line, string Role), List<CueCandidate>> Extract(OrderDump dump, float window = WindowSeconds)
    {
        var result = new Dictionary<(string, int, string), List<CueCandidate>>();
        var runs = (dump.Runs ?? []).ToDictionary(r => r.Seed, r => r.Events);
        if (runs.Count == 0) return result;
        foreach (var step in dump.Orders.GroupBy(o => (o.Cls, o.Line)))
        {
            foreach (var role in step.Select(o => o.Role).Distinct())
            {
                var seen = new Dictionary<(string Kind, string Name, string? Target), (float Before, int Seeds)>();
                var seeds = 0;
                foreach (var seed in step.Where(o => o.Role == role).Select(o => o.Seed).Distinct().Where(runs.ContainsKey))
                {
                    seeds++;
                    var at = step.Where(o => o.Role == role && o.Seed == seed).Min(o => o.Time);
                    // One count per seed, at the latest occurrence; events on other seats blur into
                    // "another player" while the role's own stay named.
                    var inSeed = new Dictionary<(string, string, string?), float>();
                    foreach (var e in runs[seed].Where(e => at - window <= e.Time && e.Time <= at + 0.05f))
                    {
                        var target = e.Target is null ? null : e.Target == role ? role : IsRole(e.Target) ? "another player" : e.Target;
                        var key = (e.Kind, e.Name, target);
                        var before = at - e.Time;
                        if (!inSeed.TryGetValue(key, out var best) || before < best) inSeed[key] = before;
                    }
                    foreach (var (key, before) in inSeed)
                        seen[key] = seen.TryGetValue(key, out var prev) ? (prev.Before + before, prev.Seeds + 1) : (before, 1);
                }
                var list = seen.Select(kv => new CueCandidate(kv.Key.Kind, kv.Key.Name, kv.Key.Target, kv.Value.Before / kv.Value.Seeds, kv.Value.Seeds, seeds))
                    .OrderByDescending(c => c.Target == role).ThenByDescending(c => c.Stable).ThenBy(c => c.SecondsBefore).ToList();
                result[(step.Key.Cls, step.Key.Line, role)] = list;
            }
        }
        return result;
    }

    private static bool IsRole(string name) => Enum.TryParse<Core.Game.Party.PartyRole>(name, out _);
}
