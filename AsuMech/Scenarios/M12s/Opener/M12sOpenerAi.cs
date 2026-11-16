using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.Opener.M12sOpenerScenario;

namespace AsuMech.Scenarios.M12s.Opener;

public sealed class M12sOpenerAi : IScenarioAi<M12sOpenerState>
{
    public string Name => "Roles Mortal Slayer";

    private const float OrbsAllSeenAt = 28.9f;

    private static readonly Vector2 GatherInTheMiddle = new(0f, -2f);
    private static readonly Vector2 FirstBait = new(4f, -11f);
    private static readonly Vector2 SecondBait = new(11f, -9f);
    private static readonly Vector2 WaitInLine = new(6f, 2f);
    private static readonly Vector2 BaitedAndPoisoned = new(13f, 8f);

    private static readonly PartyRole[] DpsOrder = [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];
    private static readonly PartyRole[] HealerOrder = [PartyRole.RegenHealer, PartyRole.ShieldHealer];
    private static readonly PartyRole[] TankOrder = [PartyRole.MainTank, PartyRole.OffTank];

    private M12sOpenerState state = null!;
    private Dictionary<PartyRole, (int Pair, int Slot)> baits = null!;

    public void Run(M12sOpenerState stateParam, SimWorld world)
    {
        state = stateParam;
        baits = AssignOrbsByRole();
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => GatherInTheMiddle + StackOffset(role)));
        ai.Move(OrbsAllSeenAt, () => ByRole(role => LineUpForPair(role, 0)), jitter: 0f, sprint: true);
        for (var p = 1; p < M12sOpenerState.Pairs; p++)
        {
            var pair = p;
            ai.Move(OrbsHitAt[pair - 1] + 0.1f, () => ByRole(role => LineUpForPair(role, pair)), jitter: 0f, sprint: true);
        }
        ai.Move(OrbsHitAt[^1] + 0.3f, () => ByRole(role => GatherInTheMiddle + StackOffset(role)));
    }

    private Dictionary<PartyRole, (int Pair, int Slot)> AssignOrbsByRole()
    {
        var dps = new Queue<PartyRole>(DpsOrder);
        var healers = new Queue<PartyRole>(HealerOrder);
        var tanks = new Queue<PartyRole>(TankOrder);
        var taken = new Dictionary<PartyRole, (int, int)>();
        for (var p = 0; p < M12sOpenerState.Pairs; p++)
            foreach (var east in new[] { false, true })
            {
                var slot = 0;
                foreach (var orb in state.OrbsOn(p, east))
                {
                    var queue = east != state.PurpleEast ? dps : orb.Purple ? tanks : healers;
                    taken[queue.Dequeue()] = (p, slot++);
                }
            }
        return taken;
    }

    private bool SupportsEast => state.PurpleEast;

    private float SideOf(PartyRole role) => role.IsDps() != SupportsEast ? 1f : -1f;

    private Vector2? LineUpForPair(PartyRole role, int pair)
    {
        var side = new Vector2(SideOf(role), 1f);
        var (baitPair, slot) = baits[role];
        if (baitPair == pair) return (slot == 0 ? FirstBait : SecondBait) * side;
        if (baitPair < pair) return BaitedAndPoisoned * side + StackOffset(role);
        return WaitInLine * side + StackOffset(role);
    }

    public static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
