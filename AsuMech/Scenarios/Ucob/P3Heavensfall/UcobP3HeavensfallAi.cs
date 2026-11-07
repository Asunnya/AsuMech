using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using State = AsuMech.Scenarios.Ucob.P3Heavensfall.UcobP3HeavensfallState;
using Scenario = AsuMech.Scenarios.Ucob.P3Heavensfall.UcobP3HeavensfallScenario;

namespace AsuMech.Scenarios.Ucob.P3Heavensfall;

public sealed class UcobP3HeavensfallAi : IScenarioAi<UcobP3HeavensfallState>
{
    public string Name => "R1 tower under Nael, R towers clockwise, L towers counter-clockwise";

    private static readonly PartyRole[] RightTowersInOrder = [OffTank, ShieldHealer, CasterDps, MeleeDpsB];
    private static readonly PartyRole[] LeftTowersInOrder = [MainTank, RegenHealer, PhysRangedDps, MeleeDpsA];
    private const float DiveSpreadRadius = 19f;
    private const float PuddleBaitRadius = 13f;
    private const float InsideThePuddlesRadius = 5f;
    private const float KnockbackStartRadius = 9f;
    private const float BurstDodgeRadius = 10f;
    private const float StackSpacing = 0.5f;
    private const float After = 0.07f;

    private State state = null!;
    private SimWorld world = null!;

    public void Run(UcobP3HeavensfallState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(0.3f, () => AiMove.Create(StackAt(Vector2.Zero)).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.Dives + After, () => AiMove.Create(DiveSpreadSpots()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.Twisters + 0.05f, () => AiMove.Create(BaitPuddlesInsideTheTwisters()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.MegaflarePuddles + After, () => AiMove.Create(OnTowerBearing(InsideThePuddlesRadius)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.MegaflarePuddlesResolve + After, () => AiMove.Create(OnTowerBearing(KnockbackStartRadius)).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.TowersResolve + After, () => AiMove.Create(BehindTheSecondSlice()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.BurstCasts[0] + Scenario.BurstCastSeconds + After, () => AiMove.Create(IntoTheFirstSliceGone()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.BurstCasts[^1] + Scenario.BurstCastSeconds + After, () => AiMove.Create(StackAt(Vector2.Zero)).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private float MiddleBossBearing => state.Formation switch
    {
        HeavensfallFormation.NaelBetween => state.NaelBearing,
        HeavensfallFormation.NaelCounterClockwise => state.NaelBearing + 45f,
        _ => state.NaelBearing - 45f,
    };

    private float DiveSpreadBearing(PartyRole role) => role switch
    {
        MainTank => state.NaelBearing - 10f,
        OffTank => state.NaelBearing + 10f,
        RegenHealer => MiddleBossBearing - 80f,
        PhysRangedDps => MiddleBossBearing - 100f,
        ShieldHealer => MiddleBossBearing + 80f,
        CasterDps => MiddleBossBearing + 100f,
        MeleeDpsA => state.NaelBearing + 190f,
        _ => state.NaelBearing + 170f,
    };

    private Vector2?[] DiveSpreadSpots()
        => Enum.GetValues<PartyRole>().Select(r => (Vector2?)State.OnBearing(DiveSpreadBearing(r), DiveSpreadRadius)).ToArray();

    private Vector2?[] BaitPuddlesInsideTheTwisters()
        => Enum.GetValues<PartyRole>().Select(r => (Vector2?)State.OnBearing(DiveSpreadBearing(r), PuddleBaitRadius)).ToArray();

    private float TowerBearing(PartyRole role)
    {
        var right = state.TowerSlotsTaken.OrderBy(s => s).Take(4).ToList();
        var left = state.TowerSlotsTaken.Except(right).OrderByDescending(s => s).ToList();
        var r = Array.IndexOf(RightTowersInOrder, role);
        return state.TowerBearing(r >= 0 ? right[r] : left[Array.IndexOf(LeftTowersInOrder, role)]);
    }

    private Vector2?[] OnTowerBearing(float radius)
        => Enum.GetValues<PartyRole>().Select(r => (Vector2?)State.OnBearing(TowerBearing(r), radius)).ToArray();

    private float SweepDirection => state.BurstsClockwise ? 1f : -1f;

    private float NearerFirstSlice(PartyRole role)
    {
        var tower = TowerBearing(role);
        var first = state.FirstBurstBearing;
        return AngleBetween(tower, first) <= AngleBetween(tower, first + 180f) ? first : first + 180f;
    }

    private Vector2?[] BehindTheSecondSlice()
        => Enum.GetValues<PartyRole>()
            .Select(r => (Vector2?)State.OnBearing(NearerFirstSlice(r) + SweepDirection * State.BurstStepDegrees, BurstDodgeRadius))
            .ToArray();

    private Vector2?[] IntoTheFirstSliceGone()
        => Enum.GetValues<PartyRole>().Select(r => (Vector2?)State.OnBearing(NearerFirstSlice(r), BurstDodgeRadius)).ToArray();

    private static float AngleBetween(float a, float b)
    {
        var d = MathF.Abs((a - b) % 360f);
        return d > 180f ? 360f - d : d;
    }

    private static Vector2?[] StackAt(Vector2 centre)
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < 8; i++)
            spots[i] = centre + new Vector2(StackSpacing * (i % 3 - 1), StackSpacing * (i / 3 - 1));
        return spots;
    }
}
