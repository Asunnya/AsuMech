using System.Collections.Generic;
using AsuMech.Core.Game.Party;

namespace AsuMech;

// The role and strats the player last picked, kept in the config so the next session opens on them.
// Strats are kept by name: indices shift when a scenario gains a strat.
public sealed class PickMemory
{
    public PartyRole? Role { get; set; }

    // Scenario full name → the strat's region and name.
    public Dictionary<string, StratPick> Strats { get; set; } = [];

    // Fight (zone) name → the last strat picked in any of its scenarios, for one not opened yet.
    public Dictionary<string, StratPick> FightStrats { get; set; } = [];
}

public sealed record StratPick(string? Group, string Name);
