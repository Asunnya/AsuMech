using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Ai;
using AsuMech.Scenarios;

namespace AsuMech.Core.Game;

// Run to end: a fight played as one run, its scenarios back to back in the fight's order.
public static class FightChain
{
    // The run that opens the fight `p` is in; null when none of its scenarios can be played this way.
    public static RunScenarioParams? First(ScenarioCatalog catalog, RunScenarioParams p)
        => First(catalog.FightOf(p.Scenario.Phase.Zone), p);

    // The run that follows `p` in its fight; null once the fight is over.
    public static RunScenarioParams? Next(ScenarioCatalog catalog, RunScenarioParams p)
        => Next(catalog.FightOf(p.Scenario.Phase.Zone), p);

    // The same over any stretch of the fight, such as one phase's parts.
    public static RunScenarioParams? First(IReadOnlyList<IScenario> chain, RunScenarioParams p)
        => FirstPlayable(chain, p, continued: false);

    public static RunScenarioParams? Next(IReadOnlyList<IScenario> fight, RunScenarioParams p)
    {
        var index = IndexOf(fight, p.Scenario);
        return index < 0 ? null : FirstPlayable(fight.Skip(index + 1), p, continued: true);
    }

    // Strats of different scenarios are different objects: the one carrying on `played` has its
    // name, else its group, else it is the scenario's default.
    public static int? StratFor(IScenario scenario, IScenarioAi? played)
    {
        if (played is null) return null;
        var strats = scenario.AiStrats;
        if (strats.Count == 0) return null;
        for (var i = 0; i < strats.Count; i++)
            if (strats[i].Name == played.Name) return i;
        if (played.Group is { } group)
            for (var i = 0; i < strats.Count; i++)
                if (strats[i].Group == group) return i;
        return 0;
    }

    private static RunScenarioParams? FirstPlayable(IEnumerable<IScenario> scenarios, RunScenarioParams p, bool continued)
    {
        var played = Strat(p);
        foreach (var scenario in scenarios)
        {
            if (played is null)
            {
                if (p.SelectedAi is null && scenario.SupportsSolo)
                    return p with { Scenario = scenario, SelectedAi = null, Continued = continued };
                continue;
            }
            if (StratFor(scenario, played) is { } strat)
                return p with { Scenario = scenario, SelectedAi = strat, Continued = continued };
        }
        return null;
    }

    private static IScenarioAi? Strat(RunScenarioParams p)
        => p.SelectedAi is { } i && i >= 0 && i < p.Scenario.AiStrats.Count ? p.Scenario.AiStrats[i] : null;

    private static int IndexOf(IReadOnlyList<IScenario> fight, IScenario scenario)
    {
        for (var i = 0; i < fight.Count; i++)
            if (ReferenceEquals(fight[i], scenario)) return i;
        return -1;
    }
}
