using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Legacy;
using static AnoMech.Core.Game.Party.PartyRole;
using State = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingState;
using Scenario = AnoMech.Scenarios.Dsr.P7DragonKing.DsrP7DragonKingScenario;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

public sealed class DsrP7DragonKingAi : IScenarioAi<DsrP7DragonKingState>
{
    public string Name => "Static south exaflare, 3/3/2 then 6/1/1";

    private static readonly PartyRole[][] TrinityPairs = [[MeleeDpsA, MeleeDpsB], [PhysRangedDps, CasterDps], [RegenHealer, ShieldHealer]];
    private static readonly PartyRole[] GroupOne = [RegenHealer, MeleeDpsA, PhysRangedDps];
    private static readonly PartyRole[] GroupTwo = [ShieldHealer, MeleeDpsB, CasterDps];
    private static readonly PartyRole[] NonTanks = [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();
    public static readonly Vector2 BehindOutsideTheDragonKing = new(0f, 10.5f);
    public static readonly Vector2 MiddleOfTheDragonKing = Vector2.Zero;
    public static readonly Vector2 MainTankFrontLeft = new(-7.8f, -7.8f);
    public static readonly Vector2 OffTankFrontRight = new(7.8f, -7.8f);
    public static readonly Vector2 IceExaflareStartInsideRearLeft = new(-6.08f, 3.47f);
    public static readonly Vector2 FlamesExaflareStartOutsideRearLeft = new(-8.07f, 4.61f);
    public static readonly Vector2 StepBackFromTheRearExaflare = new(0f, 13.5f);
    public static readonly float StepForwardDelay = State.ExaflareDelays[3] + 0.1f;
    private static readonly Vector2 OutOfEveryTower = new(0f, 1f);
    private const float IceAkhMornRadius = 7.2f;
    private const float FlamesAkhMornRadius = 9f;
    private const float IceGigaflareRadius = 6.5f;
    private const float GigaflareRadius = 10f;
    private const float StepAfterHit = 0.25f;
    private const float AfterTrinity = 0.3f;

    private DsrP7DragonKingState state = null!;
    private SimWorld world = null!;

    public void Run(DsrP7DragonKingState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(Scenario.StartAt + 0.5f, () => AiMove.Create(Cluster(BehindOutsideTheDragonKing, Everyone)).NaturalOrder(), jitter: 0f);
        for (var cycle = 0; cycle < State.Cycles; cycle++)
        {
            var c = cycle;
            var exaflarePair = 3 * c;
            var akhMornPair = 3 * c + 1;
            var gigaflarePair = 3 * c + 2;
            var firstHit = Scenario.ExaflareFirstHits[c];

            var waitAtTheRearLeft = c == 0 ? Scenario.ExaflareCasts[0] + 1.3f : Scenario.TrinityHits[2 * (gigaflarePair - 3) + 1] + AfterTrinity;
            ai.Move(waitAtTheRearLeft, () => AiMove.Create(Cluster(ExaflareStart(c), Everyone)).NaturalOrder(), jitter: 0f);
            ai.Move(firstHit + StepAfterHit, () => AiMove.Create(Cluster(State.Spot(EdgeSpot.Tanks), Everyone)).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(firstHit + State.ExaflareDelays[1] + 0.1f, () => AiMove.Create(Cluster(StepBackFromTheRearExaflare, Everyone)).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(firstHit + StepForwardDelay, () => AiMove.Create(TrinitySpots(exaflarePair, 0)).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(firstHit + State.ExaflareDelays[^1] + 0.2f, () => AiMove.Create(TrinitySpots(exaflarePair, 1)).NaturalOrder(), jitter: 0f);

            ai.Move(Scenario.TrinityHits[2 * exaflarePair + 1] + AfterTrinity, () => AiMove.Create(AkhMornSpots(c)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.AkhMornFirstHits[c] + 0.3f, () => AiMove.Create(SideTowerTankJoinsTheSix(c)).NaturalOrder(), jitter: 0f);
            world.Events.Add(Scenario.AkhMornFirstHits[c] + 0.3f, () => InvulnTheRearTowerTank(c));
            ai.Move(Scenario.AkhMornLastHit(c) + AfterTrinity, () => AiMove.Create(TrinitySpots(akhMornPair, 0)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.TrinityHits[2 * akhMornPair] + AfterTrinity, () => AiMove.Create(TrinitySpots(akhMornPair, 1)).NaturalOrder(), jitter: 0f);

            if (c >= State.GigaflareCycles) continue;
            ai.Move(Scenario.TrinityHits[2 * akhMornPair + 1] + AfterTrinity, () => AiMove.Create(Cluster(OppositeGigaflare(c, 0), Everyone)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.GigaflareHits[c][0] + StepAfterHit, () => AiMove.Create(Cluster(OppositeGigaflare(c, 1), Everyone)).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(Scenario.GigaflareHits[c][1] + StepAfterHit, () => AiMove.Create(Cluster(OppositeGigaflare(c, 2), Everyone)).NaturalOrder(), jitter: 0f, sprint: true);
            ai.Move(Scenario.GigaflareHits[c][2] + AfterTrinity, () => AiMove.Create(TrinitySpots(gigaflarePair, 0)).NaturalOrder(), jitter: 0f);
            ai.Move(Scenario.TrinityHits[2 * gigaflarePair] + AfterTrinity, () => AiMove.Create(TrinitySpots(gigaflarePair, 1)).NaturalOrder(), jitter: 0f);
        }
        ai.Move(Scenario.MornAfahsEdgeCast + 0.5f, () => AiMove.Create(HealersAndMainTankTakeTheFirstTowers()).NaturalOrder(), jitter: 0f);
    }

    private Vector2 ExaflareStart(int cycle)
        => state.ExaflareElement(cycle) == AscalonElement.Ice ? IceExaflareStartInsideRearLeft : FlamesExaflareStartOutsideRearLeft;

    private Vector2 OppositeGigaflare(int cycle, int blast)
    {
        var radius = blast == 0 && state.GigaflareElement(cycle) == AscalonElement.Ice ? IceGigaflareRadius : GigaflareRadius;
        return -Vector2.Normalize(state.GigaflareSpot(cycle, blast)) * radius;
    }

    private static Vector2?[] TrinitySpots(int pair, int trinity)
    {
        var bait = Bait(pair, trinity);
        var spots = Cluster(BehindOutsideTheDragonKing, NonTanks.Where(r => r != bait));
        spots[(int)bait] = MiddleOfTheDragonKing;
        spots[(int)MainTank] = MainTankFrontLeft;
        spots[(int)OffTank] = OffTankFrontRight;
        return spots;
    }

    private static PartyRole Bait(int pair, int trinity) => TrinityPairs[pair % TrinityPairs.Length][trinity];

    private Vector2 AkhMornSpot(int cycle, EdgeSpot spot)
        => State.Spot(spot) * ((state.AkhMornElement(cycle) == AscalonElement.Ice ? IceAkhMornRadius : FlamesAkhMornRadius) / State.SpotRadius);

    private Vector2?[] AkhMornSpots(int cycle)
    {
        if (cycle == 0)
        {
            var spots = Cluster(AkhMornSpot(cycle, EdgeSpot.Party), GroupOne);
            Place(spots, Cluster(AkhMornSpot(cycle, EdgeSpot.SecondParty), GroupTwo));
            Place(spots, Cluster(AkhMornSpot(cycle, EdgeSpot.Tanks), [MainTank, OffTank]));
            return spots;
        }
        var six = Cluster(AkhMornSpot(cycle, EdgeSpot.Party), NonTanks);
        six[(int)RearTowerTank(cycle)] = AkhMornSpot(cycle, EdgeSpot.Tanks);
        six[(int)SideTowerTank(cycle)] = AkhMornSpot(cycle, EdgeSpot.SecondParty);
        return six;
    }

    private Vector2?[] SideTowerTankJoinsTheSix(int cycle)
    {
        var spots = new Vector2?[8];
        if (cycle > 0) spots[(int)SideTowerTank(cycle)] = AkhMornSpot(cycle, EdgeSpot.Party);
        return spots;
    }

    private void InvulnTheRearTowerTank(int cycle)
    {
        if (cycle == 0 || world.Party.Get(RearTowerTank(cycle)) is not { } tank || !tank.IsAlive() || !world.Party.IsBotDriven(tank)) return;
        DamageSolver.GiveBotInvuln(tank, Scenario.AkhMornLastHit(cycle) - Scenario.AkhMornFirstHits[cycle] + 1f);
    }

    private static PartyRole RearTowerTank(int cycle) => cycle == 1 ? MainTank : OffTank;

    private static PartyRole SideTowerTank(int cycle) => cycle == 1 ? OffTank : MainTank;

    private static Vector2?[] HealersAndMainTankTakeTheFirstTowers()
    {
        var spots = Cluster(OutOfEveryTower, Everyone);
        spots[(int)MainTank] = State.Spot(EdgeSpot.Party);
        spots[(int)ShieldHealer] = State.Spot(EdgeSpot.SecondParty);
        spots[(int)RegenHealer] = State.Spot(EdgeSpot.Tanks);
        return spots;
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
