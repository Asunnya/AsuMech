using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using Scenario = AsuMech.Scenarios.Ucob.P3Tenstrike.UcobP3TenstrikeScenario;
using State = AsuMech.Scenarios.Ucob.P3Tenstrike.UcobP3TenstrikeState;

namespace AsuMech.Scenarios.Ucob.P3Tenstrike;

public sealed class UcobP3TenstrikeAi : IScenarioAi<UcobP3TenstrikeState>
{
    public string Name => "Hatches walk back from 1/2/3, cut in from the side; Earth Shakers away from 1";

    private const float HomeRadius = 12f;
    private const float EdgeRadius = 17f;
    private const float WallRadius = 17.5f;
    private const float FirstShakerRadius = 13f;
    private const float SecondShakerRadius = 8.5f;
    private const float GoToSpots = 0.5f;
    private const float AfterFirstHatches = 19.8f;
    private const float StepOffTheShaker = 0.07f;

    private static readonly Dictionary<PartyRole, float> HomeBearings = new()
    {
        [MainTank] = 0f, [OffTank] = 180f, [RegenHealer] = 270f, [ShieldHealer] = 90f,
        [MeleeDpsA] = 225f, [MeleeDpsB] = 135f, [PhysRangedDps] = 45f, [CasterDps] = 315f,
    };
    private static readonly float[] EdgeBearings = [50f, 85f, 177f, 290f, 320f];
    private static readonly int[] EdgeCuttingIntoNeurolink = [4, 2, 1];
    private static readonly float[] ShakerBearings = [67f, 140f, 220f, 293f];
    private static readonly Vector2 One = new(0f, -8f);

    private State state = null!;
    private IReadOnlyList<PartyRole> hatchByNeurolink = [];
    private IReadOnlyList<PartyRole> waitingAtEdge = [];

    public void Run(UcobP3TenstrikeState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        hatchByNeurolink = ClosestAssignment(state.HatchTargets, Geometry.Neurolinks.Select(BearingOf).ToArray());
        waitingAtEdge = ClosestAssignment(Enum.GetValues<PartyRole>().Except(state.HatchTargets).ToList(), EdgeBearings);

        ai.Move(GoToSpots, () => AiMove.Create(HomeSpots()).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.GenerateCasts[0], () => AiMove.Create(HatchesIntoNeurolinksOthersToTheEdge()).NaturalOrder(), jitter: 0f);
        ai.Move(AfterFirstHatches, () => AiMove.Create(HatchesWalkBackAndCuttersStepIn()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.EarthShakerMarkers[0] + 0.1f, () => AiMove.Create(FirstShakersOutOthersOnOne()).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.EarthShakers[0] + StepOffTheShaker, () => AiMove.Create(SecondShakersOutFirstBackToOne()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.EarthShakers[1] + StepOffTheShaker, () => AiMove.Create(EveryoneOnOne()).NaturalOrder(), jitter: 0f, sprint: true);
    }

    private static Vector2?[] HomeSpots()
    {
        var spots = new Vector2?[8];
        foreach (var (role, bearing) in HomeBearings) spots[(int)role] = AtBearing(bearing, HomeRadius);
        return spots;
    }

    private Vector2?[] HatchesIntoNeurolinksOthersToTheEdge()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < hatchByNeurolink.Count; i++) spots[(int)hatchByNeurolink[i]] = Geometry.Neurolinks[i];
        for (var i = 0; i < waitingAtEdge.Count; i++) spots[(int)waitingAtEdge[i]] = AtBearing(EdgeBearings[i], EdgeRadius);
        return spots;
    }

    private Vector2?[] HatchesWalkBackAndCuttersStepIn()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < hatchByNeurolink.Count; i++)
        {
            spots[(int)hatchByNeurolink[i]] = Vector2.Normalize(Geometry.Neurolinks[i]) * WallRadius;
            spots[(int)waitingAtEdge[EdgeCuttingIntoNeurolink[i]]] = Geometry.Neurolinks[i];
        }
        return spots;
    }

    private Vector2?[] FirstShakersOutOthersOnOne()
    {
        var spots = Cluster(One, state.SecondEarthShakers);
        Place(spots, ShakerSpots(state.FirstEarthShakers, FirstShakerRadius));
        return spots;
    }

    private Vector2?[] SecondShakersOutFirstBackToOne()
    {
        var spots = Cluster(One, state.FirstEarthShakers);
        Place(spots, ShakerSpots(state.SecondEarthShakers, SecondShakerRadius));
        return spots;
    }

    private static Vector2?[] EveryoneOnOne() => Cluster(One, Enum.GetValues<PartyRole>());

    private static Vector2?[] ShakerSpots(IReadOnlyList<PartyRole> shakers, float radius)
    {
        var spots = new Vector2?[8];
        var assigned = ClosestAssignment(shakers, ShakerBearings);
        for (var i = 0; i < assigned.Count; i++) spots[(int)assigned[i]] = AtBearing(ShakerBearings[i], radius);
        return spots;
    }

    private static IReadOnlyList<PartyRole> ClosestAssignment(IReadOnlyList<PartyRole> roles, float[] bearings)
        => Permutations(roles.ToList())
            .OrderBy(order => order.Select((r, i) => AngleBetween(HomeBearings[r], bearings[i])).Max())
            .ThenBy(order => order.Select((r, i) => AngleBetween(HomeBearings[r], bearings[i])).Sum())
            .First();

    private static IEnumerable<List<PartyRole>> Permutations(List<PartyRole> roles)
    {
        if (roles.Count <= 1)
        {
            yield return roles;
            yield break;
        }
        foreach (var first in roles)
            foreach (var rest in Permutations(roles.Where(r => r != first).ToList()))
                yield return [first, .. rest];
    }

    private static float AngleBetween(float a, float b)
    {
        var d = MathF.Abs(a - b) % 360f;
        return d > 180f ? 360f - d : d;
    }

    private static float BearingOf(Vector2 at) => (MathF.Atan2(at.X, -at.Y) * 180f / MathF.PI + 360f) % 360f;

    private static Vector2 AtBearing(float degrees, float radius)
    {
        var radians = degrees * MathF.PI / 180f;
        return new Vector2(MathF.Sin(radians), -MathF.Cos(radians)) * radius;
    }

    private static Vector2?[] Cluster(Vector2 centre, IEnumerable<PartyRole> roles)
    {
        var spots = new Vector2?[8];
        var i = 0;
        foreach (var role in roles)
        {
            spots[(int)role] = centre + new Vector2(0.6f * (i % 3 - 1), 0.6f * (i / 3 - 1));
            i++;
        }
        return spots;
    }

    private static void Place(Vector2?[] into, Vector2?[] from)
    {
        for (var i = 0; i < into.Length; i++)
            if (from[i] is { } spot) into[i] = spot;
    }
}
