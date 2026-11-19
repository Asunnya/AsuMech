using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Umad.P2Trines.UmadP2TrinesScenario;

namespace AsuMech.Scenarios.Umad.P2Trines;

public sealed class UmadP2TrinesAi : IScenarioAi<UmadP2TrinesState>
{
    public string Name => "Party first CW, tanks near and far";

    private const float ClearOfCircles = CircleRadius + 0.8f;

    private UmadP2TrinesState state = null!;
    private Vector2 partySpot;
    private Vector2 farSpot;
    private Vector2 nearSpot;

    public void Run(UmadP2TrinesState stateParam, SimWorld world)
    {
        state = stateParam;
        partySpot = Trines[FirstClockwiseFromNorth()].Centre;
        farSpot = FarthestClearSpot();
        nearSpot = NearestClearSpot();
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => Cluster(new Vector2(0f, 1.5f), role)), jitter: 0f);
        ai.Move(CleaveCastAt + 0.2f, () => ByRole(role => Cluster(new Vector2(state.CleaveWest ? 3.5f : -3.5f, 1f), role)), jitter: 0f);
        ai.Move(CleaveAt + 0.2f, () => ByRole(role => WaitOutsideTheFirstSet(BaitSpot(role), role)), jitter: 0f, arrivalTime: TrineExplodeAt[0] - 0.4f);
        ai.Move(TrineExplodeAt[0] + 0.1f, () => ByRole(BaitSpot), jitter: 0f, sprint: true);
        ai.Move(NearFarAt + 0.15f, () => ByRole(ShareTheEmbraceOnTheNearTank), jitter: 0f, arrivalTime: EmbraceAt - 0.6f);
        ai.Move(EmbraceAt + 0.3f, () => ByRole(role => Cluster(Vector2.Zero, role)), jitter: 0f, arrivalTime: AeroCastAt + 2f);
    }

    private static Vector2 Cluster(Vector2 centre, PartyRole role)
    {
        var angle = (int)role * MathF.PI / 4f;
        return centre + new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * 0.6f;
    }

    private static float ClockwiseFromNorth(Vector2 point)
    {
        var angle = MathF.Atan2(point.X, -point.Y);
        return angle < 0f ? angle + 2f * MathF.PI : angle;
    }

    private int FirstClockwiseFromNorth() => state.FirstSet.OrderBy(t => ClockwiseFromNorth(Trines[t].Centre)).First();

    private IEnumerable<int> LaterTrines => state.Set(1).Concat(state.Set(2));

    private static float ClearanceFrom(IEnumerable<int> trines, Vector2 point)
        => trines.SelectMany(Circles).Select(c => Vector2.Distance(c, point)).DefaultIfEmpty(float.MaxValue).Min();

    private static IEnumerable<Vector2> Grid()
    {
        for (var x = -20f; x <= 20f; x += 0.5f)
            for (var z = -20f; z <= 20f; z += 0.5f)
                yield return new Vector2(x, z);
    }

    private Vector2 FarthestClearSpot()
        => Grid().Where(p => p.Length() is >= 18f and <= 19.3f && ClearanceFrom(LaterTrines, p) >= ClearOfCircles && Vector2.Distance(p, partySpot) >= 9f)
            .OrderByDescending(p => p.Length()).ThenBy(p => p.X).ThenBy(p => p.Y).DefaultIfEmpty(partySpot * 1.2f).First();

    private Vector2 NearestClearSpot()
        => Grid().Where(p => p.Length() is >= 4f and <= 13.5f && ClearanceFrom(LaterTrines, p) >= ClearOfCircles
                && Vector2.Distance(p, partySpot) >= 8f && Vector2.Distance(p, farSpot) >= 8f)
            .OrderBy(p => p.Length()).ThenBy(p => p.X).ThenBy(p => p.Y).DefaultIfEmpty(partySpot * 0.6f).First();

    private Vector2 BaitSpot(PartyRole role) => role switch
    {
        PartyRole.MainTank => nearSpot,
        PartyRole.OffTank => farSpot,
        _ => Cluster(partySpot, role),
    };

    private Vector2 WaitOutsideTheFirstSet(Vector2 bait, PartyRole role)
        => Grid().Where(p => p.Length() <= 19.3f && ClearanceFrom(state.FirstSet, p) >= ClearOfCircles)
            .OrderBy(p => Vector2.Distance(p, bait)).ThenBy(p => p.X).ThenBy(p => p.Y).First()
            + (role.IsTank() ? Vector2.Zero : Cluster(Vector2.Zero, role));

    private Vector2 ShareTheEmbraceOnTheNearTank(PartyRole role) => role switch
    {
        PartyRole.MainTank => nearSpot,
        PartyRole.OffTank => nearSpot + new Vector2(0.6f, 0f),
        _ => Cluster(partySpot, role),
    };

    private static IAiMove ByRole(Func<PartyRole, Vector2> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(r => (Vector2?)at(r)).ToArray()).NaturalOrder();
}
