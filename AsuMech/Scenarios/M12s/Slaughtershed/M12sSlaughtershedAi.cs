using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.Slaughtershed.M12sSlaughtershedScenario;

namespace AsuMech.Scenarios.M12s.Slaughtershed;

public sealed class M12sSlaughtershedAi : IScenarioAi<M12sSlaughtershedState>
{
    public string Name => "Toxic Friends, Roles Mortal Slayer";

    private static readonly Vector2 FrontBait = new(6f, -13.5f);
    private static readonly Vector2 SecondBait = new(3.5f, -7.5f);
    private static readonly Vector2[] Waiting = [new(8.5f, -2f), new(8.5f, -0.8f), new(8.5f, 0.4f), new(8.5f, 1.6f)];
    private static readonly Vector2[] Baited = [new(16f, -12f), new(16.5f, -7.5f), new(13f, -9f), new(19f, -14f)];
    private static readonly Vector2 StackCorner = new(18.5f, -13.8f);
    private static readonly Vector2[] SpreadCorner = [new(12.5f, -14.5f), new(13.2f, -7.2f), new(19.6f, -6.6f), new(19.5f, -14.5f)];
    private static readonly Vector2 BesideTheBoss = new(0f, -3f);
    private static readonly Vector2 OffTheCleave = new(5f, -7f);
    private static readonly Vector2 UnderTheKnuckles = new(15f, -11f);
    private static readonly Vector2[] Huddle =
        [new(0f, -0.6f), new(0.6f, 0f), new(0f, 0.6f), new(-0.6f, 0f), new(0.45f, -0.45f), new(0.45f, 0.45f), new(-0.45f, 0.45f), new(-0.45f, -0.45f)];
    private const float Reaction = 0.5f;
    private const float KnockbackFlight = 1.8f;

    private M12sSlaughtershedState state = null!;
    private readonly Dictionary<PartyRole, (int Pair, int Slot, int Order)> baits = new();

    public void Run(M12sSlaughtershedState stateParam, SimWorld world)
    {
        state = stateParam;
        PlanTheOrbs();
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => BesideTheBoss + Huddle[(int)role]));
        ai.Move(OrbsAppearAt[0] + Reaction, () => ByRole(role => OrbStation(role, 0)), jitter: 0.2f);
        for (var p = 1; p < M12sSlaughtershedState.Pairs; p++)
        {
            var pair = p;
            ai.Move(OrbsHitAt[pair - 1] + 0.1f, () => ByRole(role => OrbStation(role, pair)), jitter: 0.2f, sprint: true);
        }
        ai.Move(OrbsHitAt[^1] + 0.3f, () => ByRole(role => BesideTheBoss + Huddle[(int)role]));
        for (var r = 0; r < M12sSlaughtershedState.Rounds; r++)
        {
            var round = r;
            var at = SlaughtershedAt[round];
            var arms = state.ArmsOf[round];
            ai.Move(at + MarkersAfter + Reaction, () => ByRole(role => StackOrSpreadCorner(round, role)), jitter: 0f);
            ai.Move(at + BurstHitsAfter + 0.2f, () => ByRole(role => FirstArmSpot(arms, role)), jitter: 0f, sprint: true);
            var firstLands = at + ArmAfter[0] + (arms.Serpentine ? SerpentineHitsAfter + 0.2f : KnucklesHitAfter + KnockbackFlight);
            ai.Move(firstLands, () => ByRole(role => SecondArmSpot(arms, role)), jitter: 0f, sprint: true);
            ai.Move(at + ArmAfter[1] + 1.5f, () => ByRole(role => BesideTheBoss + Huddle[(int)role]));
        }
    }

    private int SupportSide => state.PurpleSide;

    private int SideOf(PartyRole role) => role.IsDps() ? -SupportSide : SupportSide;

    private void PlanTheOrbs()
    {
        foreach (var east in new[] { false, true })
        {
            var side = east ? 1 : -1;
            var dps = new Queue<PartyRole>(M12sSlaughtershedState.Dps);
            var tanks = new Queue<PartyRole>([PartyRole.MainTank, PartyRole.OffTank]);
            var healers = new Queue<PartyRole>([PartyRole.RegenHealer, PartyRole.ShieldHealer]);
            var order = 0;
            for (var pair = 0; pair < M12sSlaughtershedState.Pairs; pair++)
            {
                var falling = state.FallingOn(pair, east);
                for (var slot = 0; slot < falling.Count; slot++)
                {
                    var purple = state.Orbs[pair][falling[slot]].Purple;
                    var queue = side != SupportSide ? dps : purple ? tanks : healers;
                    baits[queue.Dequeue()] = (pair, slot, order++);
                }
            }
        }
    }

    private Vector2? OrbStation(PartyRole role, int pair)
    {
        var (baitPair, slot, order) = baits[role];
        var spot = baitPair == pair ? (slot == 0 ? FrontBait : SecondBait)
            : baitPair < pair ? Baited[order]
            : Waiting[order];
        return spot with { X = spot.X * SideOf(role) };
    }

    private static int IndexInRole(PartyRole role)
        => Array.IndexOf(role.IsDps() ? M12sSlaughtershedState.Dps : M12sSlaughtershedState.Supports, role);

    private Vector2? StackOrSpreadCorner(int round, PartyRole role)
    {
        var blobSide = state.BlobEast[round] ? 1 : -1;
        if (!state.Spreads(round, role)) return new Vector2(StackCorner.X * blobSide, StackCorner.Y) + Huddle[(int)role];
        var spot = SpreadCorner[IndexInRole(role)];
        return spot with { X = -spot.X * blobSide };
    }

    private static Vector2? FirstArmSpot(Arms arms, PartyRole role)
    {
        var first = ArmSide(arms, false);
        return arms.Serpentine
            ? new Vector2(-first * OffTheCleave.X, OffTheCleave.Y) + Huddle[(int)role]
            : UnderTheKnuckles with { X = first * UnderTheKnuckles.X };
    }

    private static Vector2? SecondArmSpot(Arms arms, PartyRole role)
    {
        var second = ArmSide(arms, true);
        return arms.Serpentine
            ? new Vector2(-second * OffTheCleave.X, OffTheCleave.Y) + Huddle[(int)role]
            : UnderTheKnuckles with { X = second * UnderTheKnuckles.X };
    }

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
