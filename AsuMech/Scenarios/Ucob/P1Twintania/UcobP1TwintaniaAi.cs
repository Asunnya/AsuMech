using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using State = AsuMech.Scenarios.Ucob.P1Twintania.UcobP1TwintaniaState;
using Scenario = AsuMech.Scenarios.Ucob.P1Twintania.UcobP1TwintaniaScenario;

namespace AsuMech.Scenarios.Ucob.P1Twintania;

public sealed class UcobP1TwintaniaAi : IScenarioAi<UcobP1TwintaniaState>
{
    public string Name => "Neurolinks at 1, 2 and 3, ranged baits Liquid Hell";

    private static readonly PartyRole[] NonTanks = [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] BaitOrder = [PhysRangedDps, CasterDps, MeleeDpsB];

    private sealed record Camp(Vector2 Boss, Vector2 Front, Vector2 Side, Vector2 Stack, Vector2 FrontDodge, Vector2 SideDodge);

    private static readonly Camp North = new(Scenario.NorthCamp, new(0.1f, -13f), new(-4.5f, -6.5f), new(0.1f, -4f), new(3.1f, -13f), new(-4.5f, -9.5f));
    private static readonly Camp FirstWest = new(Scenario.FirstWestCamp, new(-11.2f, -3.5f), new(-7.5f, 1f), new(-4f, 1.5f), new(-11.2f, -6.5f), new(-7.5f, 4f));
    private static readonly Camp SecondWest = new(Scenario.SecondWestCamp, new(-12.2f, 5.9f), new(-8.5f, 10.4f), new(-3.5f, 7.5f), new(-12.2f, 2.9f), new(-5.5f, 10.4f));
    private static readonly Camp East = new(Scenario.EastCamp, new(13.3f, 5.7f), new(12.5f, 10.5f), new(5.3f, 5.7f), new(13.3f, 2.7f), new(14.3f, 12.9f));

    private static readonly Vector2[][] BaitPaths =
    [
        [new(16.5f, -9.5f), new(18.7f, -3.3f), new(18.7f, 3.3f), new(16.5f, 9.5f), new(12.2f, 14.6f), new(6.5f, 17.9f)],
        [new(1.7f, 19.9f), new(-5.2f, 19.3f), new(-11.5f, 16.4f), new(-16.4f, 11.5f), new(-19.3f, 5.2f), new(-19.8f, -1f)],
        [new(-16.5f, -9.5f), new(-18.7f, -3.3f), new(-18.7f, 3.3f), new(-16.5f, 9.5f), new(-12.2f, 14.6f), new(-6.5f, 17.9f)],
        [],
        [new(-18.6f, -4.9f), new(-19.5f, 1.5f), new(-18.2f, 7.8f), new(-15f, 13.5f), new(-10f, 17.5f), new(-4f, 19.3f)],
    ];
    private static readonly float[] BaitStarts = [Scenario.ToFirstWestCamp, 44f, Scenario.ToEastCamp + 0.1f, 0f, 118.5f];
    private static readonly Vector2 FifthBaitTwisterSpot = new(-18f, -8f);
    private static readonly Vector2[] TargetedLiquidHellLoop = [new(11f, 17f), new(16f, 13f), new(19f, 7f), new(17f, 1f), new(13f, -3f)];
    private const float RingRadius = 6.5f;
    private const float StepOff = 0.08f;
    private const float AfterDrop = 0.02f;
    private const float LeaveNeurolinkAfterHatch = 1.3f;
    private const float TankSpotsFreeOfPuddles = 106f;

    private State state = null!;

    public void Run(UcobP1TwintaniaState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, () => AiMove.Create(CampSpots(North, 0.5f)).NaturalOrder(), jitter: 0f);
        ai.Move(9.0f, () => AiMove.Create(Ring(North.Stack, RingRadius, 180f, 0f, NonTanks)).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.TwisterSnapshots[0] + StepOff, () => AiMove.Create(CampSpotsDodgingTwisters(North, Scenario.TwisterSnapshots[0] + StepOff)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.AggroSwaps[0] - 0.4f, () => AiMove.Create(CampSpots(North, Scenario.AggroSwaps[0])).NaturalOrder(), jitter: 0f);

        ai.Move(Scenario.ToFirstWestCamp, () => AiMove.Create(CampSpotsWithBaiter(FirstWest, Scenario.ToFirstWestCamp, 0)).NaturalOrder(), jitter: 0f, sprint: true);
        FollowBaitPath(ai, 0);
        GoIntoNeurolinks(ai, 0, Scenario.GenerateCasts[0]);
        ai.Move(BaitStarts[1], () => AiMove.Create(Only(Baiter(1), BaitPaths[1][0])).NaturalOrder(), jitter: 0f, sprint: true);
        FollowBaitPath(ai, 1);
        ai.Move(HatchResolvesBy(0) + LeaveNeurolinkAfterHatch, () => AiMove.Create(Cluster(FirstWest.Stack, state.HatchTargets[0].Where(r => r != Baiter(1)))).NaturalOrder(), jitter: 0f);

        ai.Move(Scenario.ToSecondWestCamp, () => AiMove.Create(CampSpots(SecondWest, Scenario.ToSecondWestCamp)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.AggroSwaps[1] - 1f, () => AiMove.Create(Tanking(SecondWest, Scenario.AggroSwaps[1])).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.GenerateCasts[1], () => AiMove.Create(Only(state.HatchTargets[1], [FarSideOfNeurolink(State.NorthNeurolink, SecondWest.Boss)])).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(66f, () => AiMove.Create(Ring(SecondWest.Stack, RingRadius, -90f, 90f, NonTanks.Except(state.HatchTargets[1]))).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.TwisterSnapshots[1] + StepOff, () => AiMove.Create(DodgeIntoNeurolinks(SecondWest, Scenario.TwisterSnapshots[1] + StepOff, 1)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(TwistersGone(1), () => AiMove.Create(Cluster(SecondWest.Stack, state.HatchTargets[1])).NaturalOrder(), jitter: 0f);

        ai.Move(Scenario.ToEastCamp + 0.1f, () => AiMove.Create(CampSpotsWithBaiter(East, Scenario.ToEastCamp, 2)).NaturalOrder(), jitter: 0f, sprint: true);
        FollowBaitPath(ai, 2);
        ai.Move(Scenario.LiquidHellDrops[2][^1] + AfterDrop, () => AiMove.Create(Cluster(East.Stack, [Baiter(2)])).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.GenerateCasts[2], () => AiMove.Create(StackAwayFromTheTargetedLiquidHell()).NaturalOrder(), jitter: 0f, sprint: true);
        RunTheTargetedLiquidHellLoop(ai);
        ai.Move(HatchResolvesBy(2) + LeaveNeurolinkAfterHatch, () => AiMove.Create(Cluster(East.Stack, NonTanks.Where(r => r != state.LiquidHellTarget))).NaturalOrder(), jitter: 0f);
        ai.Move(TankSpotsFreeOfPuddles, () => AiMove.Create(CampSpots(East, TankSpotsFreeOfPuddles)).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.AggroSwaps[2] - 1.4f, () => AiMove.Create(Tanking(East, Scenario.AggroSwaps[2])).NaturalOrder(), jitter: 0f);

        ai.Move(Scenario.GenerateCasts[3], () => AiMove.Create(HatchTargetsWaitBesideTheirNeurolinks(3)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(BaitStarts[4], () => AiMove.Create(SpreadForTheLastTwisters()).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.TwisterSnapshots[2] + StepOff, () => AiMove.Create(DodgeIntoNeurolinks(East, Scenario.TwisterSnapshots[2] + StepOff, 3)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(TwistersGone(2), () => AiMove.Create(Cluster(East.Stack, state.HatchTargets[3].Where(r => r != Baiter(4)))).NaturalOrder(), jitter: 0f);
        FollowBaitPath(ai, 4);
        ai.Move(Scenario.LiquidHellDrops[4][^1] + AfterDrop, () => AiMove.Create(Cluster(East.Stack, [Baiter(4)])).NaturalOrder(), jitter: 0f);
        GoIntoNeurolinks(ai, 4, Scenario.GenerateCasts[4]);
    }

    private PartyRole Baiter(int set)
    {
        var busy = set switch
        {
            1 => state.HatchTargets[0],
            4 => state.HatchTargets[3],
            _ => [],
        };
        return BaitOrder.First(r => !busy.Contains(r));
    }

    private static float TwistersGone(int set) => Scenario.TwisterSnapshots[set] + Scenario.TwisterFades + 0.1f;

    private static float HatchResolvesBy(int set) => set switch
    {
        0 => 47.3f,
        1 => 71.8f,
        2 => 94.3f,
        _ => 124.2f,
    };

    private void FollowBaitPath(AiManager ai, int set)
    {
        var path = BaitPaths[set];
        var drops = Scenario.LiquidHellDrops[set];
        for (var i = 0; i < drops.Length; i++)
        {
            var next = path[i + 1];
            ai.Move(drops[i] + AfterDrop, () => AiMove.Create(Only(Baiter(set), next)).NaturalOrder(), jitter: 0f, sprint: true);
        }
    }

    private void GoIntoNeurolinks(AiManager ai, int set, float at)
        => ai.Move(at, () => AiMove.Create(Neurolinks(set)).NaturalOrder(), jitter: 0f, sprint: true);

    private Vector2?[] Neurolinks(int set)
    {
        var spots = new Vector2?[8];
        var targets = state.HatchTargets[set];
        spots[(int)targets[0]] = State.NorthNeurolink;
        if (targets.Count > 1) spots[(int)targets[1]] = State.WestNeurolink;
        return spots;
    }

    private Vector2?[] HatchTargetsWaitBesideTheirNeurolinks(int set)
    {
        var spots = new Vector2?[8];
        var targets = state.HatchTargets[set];
        spots[(int)targets[0]] = FarSideOfNeurolink(State.NorthNeurolink, East.Boss);
        if (targets.Count > 1) spots[(int)targets[1]] = FarSideOfNeurolink(State.WestNeurolink, East.Boss);
        return spots;
    }

    private static Vector2 FarSideOfNeurolink(Vector2 neurolink, Vector2 boss)
        => neurolink + Vector2.Normalize(neurolink - boss) * (State.TwisterTriggerRadius + 0.7f);

    private Vector2?[] CampSpots(Camp camp, float at)
    {
        var spots = Cluster(camp.Stack, NonTanks);
        Place(spots, Tanking(camp, at));
        return spots;
    }

    private static Vector2?[] Tanking(Camp camp, float at)
    {
        var spots = new Vector2?[8];
        var aggro = Scenario.AggroTank(at);
        spots[(int)aggro] = camp.Front;
        spots[(int)(aggro == MainTank ? OffTank : MainTank)] = camp.Side;
        return spots;
    }

    private Vector2?[] CampSpotsWithBaiter(Camp camp, float at, int set)
    {
        var spots = CampSpots(camp, at);
        spots[(int)Baiter(set)] = BaitPaths[set][0];
        return spots;
    }

    private static Vector2?[] CampSpotsDodgingTwisters(Camp camp, float at)
    {
        var spots = Cluster(camp.Stack, NonTanks);
        Place(spots, TanksSidestep(camp, at));
        return spots;
    }

    private static Vector2?[] TanksSidestep(Camp camp, float at)
    {
        var spots = new Vector2?[8];
        var aggro = Scenario.AggroTank(at);
        spots[(int)aggro] = camp.FrontDodge;
        spots[(int)(aggro == MainTank ? OffTank : MainTank)] = camp.SideDodge;
        return spots;
    }

    private Vector2?[] DodgeIntoNeurolinks(Camp camp, float at, int set)
    {
        var spots = Cluster(camp.Stack, NonTanks.Except(state.HatchTargets[set]).Where(r => set != 3 || r != Baiter(4)));
        Place(spots, TanksSidestep(camp, at));
        Place(spots, Neurolinks(set));
        if (set == 3) spots[(int)Baiter(4)] = BaitPaths[4][0];
        return spots;
    }

    private Vector2?[] SpreadForTheLastTwisters()
    {
        var spreaders = NonTanks.Except(state.HatchTargets[3]).Where(r => r != Baiter(4)).ToList();
        var spots = Ring(East.Stack, RingRadius, 90f, 270f, spreaders);
        spots[(int)Baiter(4)] = FifthBaitTwisterSpot;
        return spots;
    }

    private Vector2?[] StackAwayFromTheTargetedLiquidHell()
    {
        var spots = Cluster(East.Stack, NonTanks.Except(state.HatchTargets[2]).Where(r => r != state.LiquidHellTarget));
        Place(spots, Neurolinks(2));
        spots[(int)state.LiquidHellTarget] = TargetedLiquidHellLoop[0];
        return spots;
    }

    private void RunTheTargetedLiquidHellLoop(AiManager ai)
    {
        var drops = Scenario.LiquidHellDrops[Scenario.TargetedLiquidHellSet];
        var target = state.LiquidHellTarget;
        for (var i = 0; i < drops.Length; i++)
        {
            var next = i + 1 < TargetedLiquidHellLoop.Length ? TargetedLiquidHellLoop[i + 1] : East.Stack;
            ai.Move(drops[i] + AfterDrop, () => AiMove.Create(Only(target, next)).NaturalOrder(), jitter: 0f, sprint: true);
        }
    }

    private static Vector2?[] Ring(Vector2 centre, float radius, float fromDegrees, float toDegrees, IEnumerable<PartyRole> roles, int? slots = null)
    {
        var list = roles.ToList();
        var count = slots ?? Math.Max(list.Count, 2);
        var spots = new Vector2?[8];
        for (var i = 0; i < list.Count; i++)
        {
            var degrees = fromDegrees + (toDegrees - fromDegrees) * i / (count - 1);
            var radians = degrees * MathF.PI / 180f;
            spots[(int)list[i]] = centre + radius * new Vector2(MathF.Cos(radians), MathF.Sin(radians));
        }
        return spots;
    }

    private static Vector2?[] Only(PartyRole role, Vector2 spot)
    {
        var spots = new Vector2?[8];
        spots[(int)role] = spot;
        return spots;
    }

    private static Vector2?[] Only(IEnumerable<PartyRole> roles, Vector2[] spots)
    {
        var result = new Vector2?[8];
        var i = 0;
        foreach (var role in roles) result[(int)role] = spots[Math.Min(i++, spots.Length - 1)];
        return result;
    }

    private static void Place(Vector2?[] into, Vector2?[] from)
    {
        for (var i = 0; i < into.Length; i++)
            if (from[i] is { } spot) into[i] = spot;
    }

    private static Vector2?[] Cluster(Vector2 centre, IEnumerable<PartyRole> roles)
    {
        var spots = new Vector2?[8];
        var i = 0;
        foreach (var role in roles)
        {
            spots[(int)role] = centre + new Vector2(0.5f * (i % 3 - 1), 0.5f * (i / 3 - 1));
            i++;
        }
        return spots;
    }
}
