using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;

namespace AsuMech;

// The scenario set up for the next Start, shared by every main window so switching layouts keeps
// the pick.
public sealed class ScenarioPicker
{
    private readonly Action<IScenario>? remember;
    private readonly PickMemory picks;
    private readonly Action? savePicks;
    private readonly Dictionary<IZone, int> waymarkMemory = new();
    private bool roleLocked;
    private bool guest;

    public ScenarioPicker(Action<IScenario>? remember = null, PickMemory? picks = null, Action? savePicks = null)
    {
        this.remember = remember;
        this.picks = picks ?? new PickMemory();
        this.savePicks = savePicks;
        role = this.picks.Role;
    }

    public IScenario? Scenario { get; private set; }

    // Absolute index into Scenario.AiStrats, which is what a run consumes. -1 when a grouped
    // scenario's region has no strats.
    public int Strat { get; private set; }

    // The selected region of a scenario whose strats declare IScenarioAi.Group; null otherwise.
    public string? StratGroup { get; private set; }

    public int Waymark { get; private set; }
    // Null in a session, where the party seat decides; the player's pick comes back after it.
    public PartyRole? RoleOverride => roleLocked ? null : role;
    private PartyRole? role;
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
        Waymark = waymarkMemory.GetValueOrDefault(scenario.Phase.Zone);
        if (Waymark < 0 || Waymark >= scenario.Phase.Zone.WaymarkPresets.Count)
            Waymark = 0;
        RecallStrat();

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
        roleLocked = inSession;
        if (guest)
        {
            Strat = 0;
            StratGroup = null;
        }
        else if (this.guest) RecallStrat();
        this.guest = guest;
        ReconcileStrat();
    }

    public void ChooseRole(PartyRole? chosen)
    {
        if (roleLocked || role == chosen) return;
        role = chosen;
        picks.Role = chosen;
        savePicks?.Invoke();
    }

    public void SelectStrat(int index)
    {
        if (Scenario is null || index < 0 || index >= Scenario.AiStrats.Count) return;
        Strat = index;
        StratGroup = Scenario.AiStrats[index].Group;
        ReconcileStrat();
        RememberStrat();
    }

    public void SelectGroup(string group)
    {
        if (Scenario is null) return;
        StratGroup = group;
        ReconcileStrat();
        RememberStrat();
    }

    // This scenario's own last pick first, then the fight's last pick when this scenario has a
    // strat of that name, else the first.
    private void RecallStrat()
    {
        if (Scenario is not { } scenario) return;
        Strat = 0;
        StratGroup = null;
        var pick = picks.Strats.GetValueOrDefault(scenario.FullName()) is { } own && IndexOf(scenario, own) >= 0 ? own
            : picks.FightStrats.GetValueOrDefault(scenario.Phase.Zone.Name);
        if (pick != null && IndexOf(scenario, pick) is var index and >= 0) Strat = index;
        else if (pick?.Group is { } group && StratGroups(scenario).Contains(group)) StratGroup = group;
        if (Strat >= 0 && Strat < scenario.AiStrats.Count && StratGroup == null) StratGroup = scenario.AiStrats[Strat].Group;
        ReconcileStrat();
    }

    private static int IndexOf(IScenario scenario, StratPick pick)
    {
        var strats = scenario.AiStrats;
        for (var i = 0; i < strats.Count; i++)
            if (strats[i].Name == pick.Name && strats[i].Group == pick.Group) return i;
        return -1;
    }

    private void RememberStrat()
    {
        if (Scenario is not { } scenario || guest) return;
        StratPick? pick = HasStartableStrat && SelectedAi is { } ai ? new StratPick(ai.Group, ai.Name)
            : StratGroup is { } group ? new StratPick(group, "") : null;
        if (pick == null) return;
        picks.Strats[scenario.FullName()] = pick;
        if (pick.Name.Length > 0) picks.FightStrats[scenario.Phase.Zone.Name] = pick;
        savePicks?.Invoke();
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
