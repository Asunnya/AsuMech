using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Umad.P5Celestriad;

// First-pass bots for Celestriad: each doppel already "knows" its own permanent element (or
// free-pair role) and, within a doubled element's active pair, which tower is "theirs" (see
// PlaceSet), a strategy call this AI owns, not something UmadP5CelestriadState hands it. Runs
// clockwise through this run's element sectors to that set's matching tower, splitting the
// pair 1y either side of the tower's tangent so
// both fit inside the soak radius. On Catastrophic Choice sets (0 and 2), bots first stack in
// the tower's centre, then after ChoiceReadDelay step to the safe half: Aero (green) -> toward
// the boss, Earth (brown) -> away from the boss. This is simulated recognition time, not a
// read of the ground VFX (which only flashes at resolution, see
// UmadP5CelestriadScenario.SpawnChoiceOmen): bots already know their side from state directly.
public sealed class UmadP5CelestriadAi : IScenarioAi<UmadP5CelestriadState>
{
    public string Name => "Standard";

    private const float MoveSpeed = 6f;
    private const float PairOffset = 1f;
    private const float HalfOffset = 2f;

    public void Run(UmadP5CelestriadState state, SimWorld world)
    {
        var ai = new AiManager(world);
        world.Events.Add(7.6f, () => PlaceSet(ai, world, state, 0, half: 0f, at: 7.6f));
        world.Events.Add(12.18f, () => PlaceSet(ai, world, state, 0, HalfFor(state, 0), at: 12.18f));
        world.Events.Add(15.9f, () => PlaceSet(ai, world, state, 1, half: 0f, at: 15.9f));
        world.Events.Add(22.1f, () => PlaceSet(ai, world, state, 2, half: 0f, at: 22.1f));
        world.Events.Add(24.34f, () => PlaceSet(ai, world, state, 2, HalfFor(state, 2), at: 24.34f));
    }

    private static float HalfFor(UmadP5CelestriadState state, int set) =>
        state.AeroVariant[set] is { } choice
            ? (choice == CatastrophicChoice.Aero ? 1f : -1f)
            : 0f;

    private static void PlaceSet(AiManager ai, SimWorld world, UmadP5CelestriadState state, int set, float half, float at, [CallerLineNumber] int line = 0)
    {
        var party = world.Party;
        var freeRoles = state.PlayerDebuffElement.Where(kv => kv.Value is null).Select(kv => kv.Key).ToArray();

        // Grouped by element to recover the doubled-pair ordering State guarantees (ascending
        // sub-index): within a doubled element's 2 active towers, the first always gets that
        // set's debuffed pair for this element and the second always gets the free pair. This
        // pairing is this AI's own strategy choice, not a fact State hands us.
        foreach (var group in state.SetActiveTowers[set]
                     .Select(towerIndex => state.AllTowers[towerIndex])
                     .GroupBy(tower => tower.Element))
        {
            var debuffedRoles = state.PlayerDebuffElement
                .Where(kv => kv.Value is not null && state.ElementForSet(kv.Key, set) == group.Key)
                .Select(kv => kv.Key)
                .ToArray();

            var towers = group.ToArray();
            for (var i = 0; i < towers.Length; i++)
                PlaceAtTower(ai, party, towers[i], i == 0 ? debuffedRoles : freeRoles, half, at, line);
        }
    }

    private static void PlaceAtTower(AiManager ai, SimParty party, CelestriadTower tower, IReadOnlyList<PartyRole> roles, float half, float at, int line)
    {
        var inward = Vector3.Normalize(-tower.Position);
        var lateral = new Vector3(-inward.Z, 0f, inward.X);

        for (var i = 0; i < roles.Count; i++)
        {
            var bot = party.Get(roles[i]);
            if (bot is null || !bot.IsAlive()) continue;

            var side = i == 0 ? -1f : 1f;
            var dest = tower.Position + lateral * (side * PairOffset) + inward * (half * HalfOffset);
            ai.MoveNow(bot, dest, MoveSpeed, at, line: line);
        }
    }
}
