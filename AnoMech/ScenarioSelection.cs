using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;

namespace AnoMech;

// What the main window has set up for the next Start. Strat is null when the selected region has
// no strat to run.
public sealed record ScenarioSelection(IScenario Scenario, int? Strat, int Waymark, int? Seed, PartyRole? RoleOverride);
