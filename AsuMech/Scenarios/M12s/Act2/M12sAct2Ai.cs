using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.Act2.M12sAct2Scenario;

namespace AsuMech.Scenarios.M12s.Act2;

public sealed class M12sAct2Ai : IScenarioAi<M12sAct2State>
{
    public string Name => "Toxic Friends";

    private const float OutsideWidth = 17.5f;
    private const float OutsideDepth = 14.2f;
    private const float BetaStep = 8f;
    private const float ChainTowerSpot = 7f;
    private const float LegTime = 2.2f;
    private const float FirstLegAfterChain = 3.4f;
    private const float BetaReturnsAfterChain = 2.5f;
    private const float ChainTowerSoakerLeavesAfterChain = 6f;
    private const float BlobSoakerArrivesBefore = 2f;
    private const float LeaveTheCoilAt = 57.5f;
    private const float RegroupAt = 63f;

    private M12sAct2State state = null!;

    public void Run(M12sAct2State stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, () => Everyone(_ => Vector2.Zero), jitter: 0f);
        for (var p = 0; p < M12sAct2State.Pairs; p++)
        {
            var order = p + 1;
            var tower = order;
            ai.Move(ChainAt[p] + 0.05f, () => Only(new()
            {
                [state.Alpha(order)] = Outside(state.Facing(order + 2)),
                [state.Beta(order)] = state.BetaDirection(order) * BetaStep,
            }), jitter: 0f);
            ai.Move(ChainAt[p] + BetaReturnsAfterChain, () => Only(new()
            {
                [state.Beta(order)] = Vector2.Zero,
                [state.Beta(M12sAct2State.SoakerOrder(tower))] = state.BetaDirection(tower) * ChainTowerSpot,
            }), jitter: 0f);
            ai.Move(ChainAt[p] + ChainTowerSoakerLeavesAfterChain, () => Only(new()
            {
                [state.Beta(M12sAct2State.SoakerOrder(tower))] = Vector2.Zero,
            }), jitter: 0f);
        }
        SoakTheInnerBlobs(ai);
        WalkAroundTheCoilToTheOuterBlobs(ai);
        ai.Move(LeaveTheCoilAt, () => Only(Enumerable.Range(1, M12sAct2State.Pairs).ToDictionary(o => state.Beta(o), o => Outside(state.Facing(o)))), jitter: 0f);
        ai.Move(RegroupAt, () => Everyone(_ => Vector2.Zero));
    }

    private void SoakTheInnerBlobs(AiManager ai)
    {
        for (var tower = 1; tower <= 2; tower++)
        {
            var blob = tower;
            var soaker = M12sAct2State.SoakerOrder(blob);
            var resolves = BlobTowerCastAt[blob - 1] + TowerResolves;
            ai.Move(resolves - BlobSoakerArrivesBefore, () => Only(new() { [state.Alpha(soaker)] = state.BlobSpot(blob) }), jitter: 0f);
            ai.Move(resolves + 0.2f, () => Only(new() { [state.Alpha(soaker)] = Vector2.Zero }), jitter: 0f);
        }
    }

    private void WalkAroundTheCoilToTheOuterBlobs(AiManager ai)
    {
        for (var tower = 3; tower <= 4; tower++)
        {
            var blob = tower;
            var soaker = M12sAct2State.SoakerOrder(blob);
            var legs = PathAroundTheCoil(state.Facing(soaker + 2), state.BlobSpot(blob));
            for (var i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                ai.Move(ChainAt[soaker - 1] + FirstLegAfterChain + i * LegTime, () => Only(new() { [state.Alpha(soaker)] = leg }), jitter: 0f);
            }
        }
    }

    private static List<Vector2> PathAroundTheCoil(float from, Vector2 to)
    {
        var step = MathF.PI / 4f;
        var target = MathF.Round(MathF.Atan2(to.X, to.Y) / step) * step;
        var turn = MathF.IEEERemainder(target - from, 2f * MathF.PI);
        var steps = (int)MathF.Round(MathF.Abs(turn) / step);
        var legs = new List<Vector2>();
        for (var i = 1; i <= steps; i++) legs.Add(Outside(from + MathF.Sign(turn) * i * step));
        legs.Add(to);
        return legs;
    }

    public static Vector2 Outside(float bearing) => new(OutsideWidth * MathF.Sin(bearing), OutsideDepth * MathF.Cos(bearing));

    private static IAiMove Everyone(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();

    private static IAiMove Only(Dictionary<PartyRole, Vector2> spots)
        => Everyone(role => spots.TryGetValue(role, out var spot) ? spot : null);
}
