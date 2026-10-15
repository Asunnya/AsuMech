using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Scenarios.Umad.P2Forsaken.Ai;

// Movement choreography for the JP "Yan" strat — a fork of the NA "Kroxy-Rinon 341"
// helper (UmadP2ForsakenRinonAiHelper), free to diverge without touching it. Yan's own
// tower spots, and the last Past's/Future's End is baited at the A marker. See the
// original helper for the framework design notes.
public sealed class UmadP2ForsakenYanAiHelper
{
    private readonly Vector2?[] oddCoords;
    private readonly Vector2?[] evenCoords;
    private readonly Action<UmadP2ForsakenYanAiHelper, int, IList<PartyRole>> reorderActive;

    private UmadP2ForsakenState state = null!;

    private IReadOnlyList<PartyRole> alphaInitial = [];
    private IReadOnlyList<PartyRole> betaInitial = [];
    private List<PartyRole> alpha = [];
    private List<PartyRole> beta = [];

    public UmadP2ForsakenYanAiHelper(
        Vector2?[] oddCoords,
        Vector2?[] evenCoords,
        Action<UmadP2ForsakenYanAiHelper, int, IList<PartyRole>> reorderActive)
    {
        this.oddCoords = oddCoords;
        this.evenCoords = evenCoords;
        this.reorderActive = reorderActive;
    }

    public void Run(UmadP2ForsakenState s, SimWorld world)
    {
        state = s;
        var ai = new AiManager(world);
        Init();

        ai.Move(1f, InitialLineup);
        ai.Move(10.16f, TowerPositions(0), jitter: .0f, arrivalTime: 22.16f);
        ai.Move(25.17f, TowerPositions(1), jitter: .0f, arrivalTime: 32.16f);
        ai.Move(33.3f, AllThingsEndsBait(0, 2), arrivalTime: 37f);
        ai.Move(39.21f, TowerPositions(2), jitter: .0f, arrivalTime: 43.21f);
        ai.Move(47.22f, TowerPositions(3), jitter: .0f, arrivalTime: 53.22f);
        ai.Move(54.35f, AllThingsEndsBait(1, 4), arrivalTime: 57f);
        ai.Move(59.26f, TowerPositions(4), jitter: .0f, arrivalTime: 63.86f);
        ai.Move(65.4f, TowerPositions(5), jitter: .0f, arrivalTime: 73.27f);
        ai.Move(75.4f, AllThingsEndsBait(2, 6), arrivalTime: 78f);
        ai.Move(81.31f, TowerPositions(6), jitter: .0f, arrivalTime: 85.8f);
        ai.Move(90.32f, TowerPositions(7), jitter: .0f, arrivalTime: 94.32f);
        // Occurrence 3 is the real two-step: gather on the A marker side, then relocate once the
        // castbar starts. The boss's facing locks at its Face() call, so moving during the cast is
        // what makes Future's End safe; Past's End's second move is a same-spot no-op.
        ai.Move(96.45f, GatherAtA(), arrivalTime: 98f);
        ai.Move(101.16f, LastEndBaitAtA(), arrivalTime: 105f);
    }

    // Future's End sits opposite the upcoming towers, Past's End between them, both on the
    // NewNorthAt(2*i+2) bisector the tower pair straddles. Future's following tower transition
    // is infeasible at any distance, so its spot is pushed out for range as well as angle (the
    // cone tracks party.Player, who is in this stack); Past keeps the tighter margin to help its
    // own tight transition. Both casters sit at the origin.
    private const float PastMeleeFromCenter = 9.7f;
    private const float FutureMeleeFromCenter = 11f;

    private Func<IAiMove> AllThingsEndsBait(int i, int northIndex)
    {
        var distance = state.EndAttacks[i] == EndAttack.PastsEnd ? -PastMeleeFromCenter : FutureMeleeFromCenter;
        return () => AiMove.All(new(0, distance))
                           .ApplyPositions(state.NewNorthAt(northIndex).Apply);
    }

    // A sits due north in every Umad waymark preset. 
    private Func<IAiMove> GatherAtA()
    {
        return () => AiMove.All(new(0, -PastMeleeFromCenter))
                           .ApplyPositions(Direction.N.Apply);
    }

    private Func<IAiMove> LastEndBaitAtA()
    {
        var distance = state.EndAttacks[3] == EndAttack.PastsEnd ? -PastMeleeFromCenter : FutureMeleeFromCenter;
        return () => AiMove.All(new(0, distance))
                           .ApplyPositions(Direction.N.Apply);
    }

    private void Init()
    {
        var stacks = state.Lockons.Where(pair => pair.Value == LockonId.ForsakenStack).Select(pair => pair.Key)
                          .ToList();
        var supportPair = (PartyRole)(((int)stacks[0] + 2) % 4);
        var dpsPair = (PartyRole)((((int)stacks[1] - 2) % 4) + 4);
        var list = new List<PartyRole>([stacks[0], stacks[1], supportPair, dpsPair]);
        list.Sort();
        (list[0], list[1]) = (list[1], list[0]); // swap tank and healer
        alpha = list;
        alphaInitial = new List<PartyRole>(alpha);
        list = Enum.GetValues<PartyRole>()
                   .Where(role => !list.Contains(role))
                   .ToList();
        (list[0], list[1]) = (list[1], list[0]); // swap tank and healer
        beta = list;
        betaInitial = new List<PartyRole>(beta);
    }


    public PartyRole ActiveRole(uint mechanic, int towerId, int order)
    {
        var array = towerId is < 3 or 7 ? alpha : beta;
        try
        {
            return array
                   .Where(role => state.Lockons[role] == mechanic)
                   .Skip(order)
                   .First();
        }
        catch (InvalidOperationException)
        {
            DiagnosticLog.Warn($"Lockons {string.Join(",", state.Lockons)}");
            DiagnosticLog.Warn($"Can't find {mechanic}.{order}, for {towerId}, for {string.Join(",", array)}");
            throw;
        }
    }

    private PartyRole PassiveRole(int towerId, int order)
    {
        var array = towerId is < 3 or 7 ? betaInitial : alphaInitial;
        return array[order];
    }

    private IAiMove InitialLineup()
    {
        return AiMove.Create(
            new(-2.5f, -2.5f),
            new(-2.5f, 2.5f),
            new(-7.5f, -2.5f),
            new(-7.5f, 2.5f),
            new(2.5f, -2.5f),
            new(2.5f, 2.5f),
            new(7.5f, -2.5f),
            new(7.5f, 2.5f)
        );
    }

    private Func<IAiMove> TowerPositions(int i)
    {
        return () =>
        {
            var move = i % 2 == 1 ? EvenTower(i) : OddTower(i); // odd/even flipped because 0 indexing teehee
            reorderActive(this, i, i is < 3 or 7 ? alpha : beta); // plug point: same active-group rule as ActiveRole
            return move;
        };
    }

    private IAiMove OddTower(int i)
    {
        return AiMove.Create(oddCoords)
                     .Assignments([
                         ActiveRole(LockonId.ForsakenStack, i, 0),
                         ActiveRole(LockonId.ForsakenCone, i, 0),
                         ActiveRole(LockonId.ForsakenStack, i, 1),
                         ActiveRole(LockonId.ForsakenChariot, i, 0),
                         PassiveRole(i, 0),
                         PassiveRole(i, 1),
                         PassiveRole(i, 2),
                         PassiveRole(i, 3)
                     ])
                     .ApplyPositions(state.NewNorthAt(i).Apply);
    }

    private IAiMove EvenTower(int i)
    {
        return AiMove.Create(evenCoords)
                     .Assignments([
                         ActiveRole(LockonId.ForsakenCone, i, 0),
                         ActiveRole(LockonId.ForsakenChariot, i, 0),
                         ActiveRole(LockonId.ForsakenCone, i, 1),
                         ActiveRole(LockonId.ForsakenChariot, i, 1),
                         PassiveRole(i, 0),
                         PassiveRole(i, 1),
                         PassiveRole(i, 2),
                         PassiveRole(i, 3),
                     ])
                     .ApplyPositions(state.NewNorthAt(i).Apply);
    }
    
    // --- Coordinate sets ---
    // 16 scenario-local XZ coords per set: 8 for odd-index towers, 8 for even-index
    // towers (active 4 + passive 4 each). Passed into the constructor by the strats.
    // AiMove copies these on Create, so its per-move rotation never mutates the set.

    // JP "Yan" layout — hand-placed spots (authoring frame, north up).

    public static readonly Vector2?[] YanOdd =
    [
        // active group
        new(5.66f, -4.66f),   // stack
        new(5.66f, -8.66f),   // cone
        new(-5.4f, -2.4f),    // stack
        new(-5.66f, -9.16f),  // chariot
        // passive group
        new(5.66f, -10.66f),  // outer cone bait
        new(2.5f, -2.5f),     // in the cone tower's stack, outside the tower
        new(-3.5f, -1.9f),    // in stack
        new(-3.5f, -1.9f)     // in stack
    ];

    public static readonly Vector2?[] YanEven =
    [
        // active group
        new(5.66f, -2.16f),   // cone1
        new(5.05f, -9.11f),   // chariot1
        new(-5.66f, -2.16f),  // cone2
        new(-5.05f, -9.11f),  // chariot2
        // passive group
        new(10.66f, -5.66f),  // cone bait1
        new(3.5f, 4.9f),      // clone bait1
        new(-3.5f, 4.9f),     // clone bait2
        new(-10.66f, -5.66f)  // cone bait2
    ];

    // --- Reorder behavior ---
    // The strat's per-tower plug point, invoked from TowerPositions with the helper
    // instance + the active 4-role group. Yan doesn't reorder (no-op).
    public static void KroxyReorder(UmadP2ForsakenYanAiHelper helper, int index, IList<PartyRole> active)
    {
    }
}
