using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;

namespace AnoMech;

// The scenario set up for the next Start, shared by every main window so switching layouts keeps
// the pick.
public sealed class ScenarioPicker
{
    private readonly Action<IScenario>? remember;
    private readonly Dictionary<IZone, int> waymarkMemory = new();
    private readonly Dictionary<IScenario, string> stratGroupMemory = new();

    public ScenarioPicker(Action<IScenario>? remember = null) => this.remember = remember;

    public IScenario? Scenario { get; private set; }

    // Absolute index into Scenario.AiStrats, which is what a run consumes. -1 when a grouped
    // scenario's region has no strats.
    public int Strat { get; private set; }

    // The selected region of a scenario whose strats declare IScenarioAi.Group; null otherwise.
    public string? StratGroup { get; private set; }

    public int Waymark { get; private set; }
    public PartyRole? RoleOverride { get; set; }
    public bool Solo { get; set; }

    // Empty rolls a fresh seed each time; a number replays it every time.
    public string SeedText { get; set; } = "";
    public int? Seed => int.TryParse(SeedText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed) ? seed : null;

    public ScenarioSelection? Selection => Scenario is { } scenario
        ? new ScenarioSelection(scenario, HasStartableStrat ? Strat : null, Waymark, Seed, RoleOverride)
        : null;

    public void Select(IScenario scenario, bool persist = true)
    {
        if (Scenario is { } previous)
            waymarkMemory[previous.Phase.Zone] = Waymark;

        Scenario = scenario;
        Solo = false;
        Strat = 0;
        Waymark = waymarkMemory.GetValueOrDefault(scenario.Phase.Zone);
        if (Waymark < 0 || Waymark >= scenario.Phase.Zone.WaymarkPresets.Count)
            Waymark = 0;
        StratGroup = stratGroupMemory.GetValueOrDefault(scenario);
        ReconcileStrat();

        if (persist) remember?.Invoke(scenario);
    }

    public bool Restore(string fullName, IEnumerable<IScenario> catalog)
    {
        if (string.IsNullOrEmpty(fullName)) return false;
        var scenario = catalog.FirstOrDefault(s => string.Equals(s.FullName(), fullName, StringComparison.Ordinal));
        if (scenario is null) return false;
        Select(scenario, persist: false);
        return true;
    }

    // A guest runs whatever the host picked.
    public void FollowHost(IScenario? hostScenario)
    {
        if (hostScenario is not null && Scenario != hostScenario) Select(hostScenario, persist: false);
    }

    // Every frame. In a session the role comes from the party seat and only the host's
    // region/strat runs; each is reset, not just disabled, so a stale pick can't apply.
    public void Reconcile(bool inSession, bool guest)
    {
        if (inSession) RoleOverride = null;
        if (guest)
        {
            Strat = 0;
            StratGroup = null;
        }
        ReconcileStrat();
    }

    public void SelectStrat(int index)
    {
        if (Scenario is null || index < 0 || index >= Scenario.AiStrats.Count) return;
        Strat = index;
        if (Scenario.AiStrats[index].Group is { } group) SelectGroup(group);
    }

    public void SelectGroup(string group)
    {
        if (Scenario is null) return;
        StratGroup = group;
        stratGroupMemory[Scenario] = group;
        ReconcileStrat();
    }

    public void SelectWaymark(int index)
    {
        if (Scenario is null || index < 0 || index >= Scenario.Phase.Zone.WaymarkPresets.Count) return;
        Waymark = index;
        waymarkMemory[Scenario.Phase.Zone] = index;
    }

    public static IReadOnlyList<string> StratGroups(IScenario scenario)
    {
        var groups = new List<string>();
        foreach (var ai in scenario.AiStrats)
            if (ai.Group is { } g && !groups.Contains(g)) groups.Add(g);
        return groups;
    }

    public static bool HasStratChoice(IScenario scenario) =>
        StratGroups(scenario).Count > 0 || scenario.AiStrats.Count > 1;

    public static bool HasWaymarkChoice(IScenario scenario) =>
        scenario.Phase.Zone.WaymarkPresets.Count > 1;

    // Indices into AiStrats of the selected region.
    public List<int> RegionStrats()
    {
        var inRegion = new List<int>();
        if (Scenario is null) return inRegion;
        var strats = Scenario.AiStrats;
        for (var i = 0; i < strats.Count; i++)
            if (strats[i].Group == StratGroup) inRegion.Add(i);
        return inRegion;
    }

    // Grouped scenarios need a real strat in the active region.
    public bool HasStartableStrat
    {
        get
        {
            if (Scenario is not { } scenario) return false;
            if (StratGroups(scenario).Count == 0) return true;
            var strats = scenario.AiStrats;
            return Strat >= 0 && Strat < strats.Count && strats[Strat].Group == StratGroup;
        }
    }

    public IScenarioAi? SelectedAi => Scenario is { } s && Strat >= 0 && Strat < s.AiStrats.Count ? s.AiStrats[Strat] : null;

    private void ReconcileStrat()
    {
        if (Scenario is not { } scenario) return;
        var strats = scenario.AiStrats;
        var groups = StratGroups(scenario);
        if (groups.Count == 0)
        {
            if (strats.Count > 1) Strat = Math.Clamp(Strat, 0, strats.Count - 1);
            return;
        }
        if (StratGroup is null || !groups.Contains(StratGroup)) StratGroup = groups[0];
        var inRegion = RegionStrats();
        if (inRegion.Count == 0) Strat = -1;
        else if (!inRegion.Contains(Strat)) Strat = inRegion[0];
    }
}
