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

public class UcobP3TenstrikeAi : IScenarioAi<UcobP3TenstrikeState>
{
    public virtual string Name => "Hatches walk back from 1/2/3, cut in from the side; Earth Shakers away from 1";

    private const float HomeRadius = 12f;
    private const float WallRadius = 17.5f;
    private const float GoToSpots = 0.5f;
    private const float AfterFirstHatches = 19.8f;
    private const float StepOffTheShaker = 0.07f;
    private const float FirstAdaptAfterMarker = 0.5f;
    private const float AdaptEvery = 0.4f;
    private const float LastAdaptBeforeCone = 0.9f;
    private static readonly float[] ConeClearances = [55f, 50f, 46f];
    private const float PeopleCountFrom = 5f;
    private const float PuddleClearance = Scenario.PuddleRadius + 0.4f;

    private static readonly Dictionary<PartyRole, float> HomeBearings = new()
    {
        [MainTank] = 0f, [OffTank] = 180f, [RegenHealer] = 270f, [ShieldHealer] = 90f,
        [MeleeDpsA] = 225f, [MeleeDpsB] = 135f, [PhysRangedDps] = 45f, [CasterDps] = 315f,
    };
    private static readonly float[] AroundOneEdgeBearings = [50f, 85f, 177f, 290f, 320f];
    private static readonly int[] AroundOneEdgeCuttingIntoNeurolink = [4, 2, 1];
    private static readonly float[] AwayFromOneShakerBearings = [67f, 140f, 220f, 293f];
    private static readonly Vector2 One = new(0f, -8f);

    protected virtual IReadOnlyList<Vector2> Neurolinks => Geometry.Neurolinks;
    protected virtual float[] EdgeBearings => AroundOneEdgeBearings;
    protected virtual float EdgeRadius => 17f;
    protected virtual int[] EdgeCuttingIntoNeurolink => AroundOneEdgeCuttingIntoNeurolink;
    protected virtual float[] ShakerBearings => AwayFromOneShakerBearings;
    protected virtual float ShakerArcStart => 60f;
    protected virtual float ShakerArcEnd => 300f;
    protected virtual float FirstShakerRadius => 13f;
    protected virtual float SecondShakerRadius => 8.5f;
    protected virtual Vector2 WaitingSpot => One;

    protected State state = null!;
    protected SimWorld world = null!;
    private readonly List<Vector2> firstPuddles = [];
    protected IReadOnlyList<PartyRole> hatchByNeurolink = [];
    protected IReadOnlyList<PartyRole> waitingAtEdge = [];

    public void Run(UcobP3TenstrikeState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        firstPuddles.Clear();
        var ai = new AiManager(world);
        AssignHatchesAndEdges();

        ai.Move(GoToSpots, () => AiMove.Create(HomeSpots()).NaturalOrder(), jitter: 0f);
        ai.Move(Scenario.GenerateCasts[0], () => AiMove.Create(HatchesIntoNeurolinksOthersToTheEdge()).NaturalOrder(), jitter: 0f);
        ai.Move(AfterFirstHatches, () => AiMove.Create(HatchesWalkBackAndCuttersStepIn()).NaturalOrder(), jitter: 0f, sprint: true);
        ai.Move(Scenario.EarthShakerMarkers[0] + 0.1f, () => AiMove.Create(FirstShakersOutOthersWaiting()).NaturalOrder(), jitter: 0f);
        AdaptBotShakers(ai, Scenario.EarthShakerMarkers[0], Scenario.EarthShakers[0], state.FirstEarthShakers, FirstShakerRadius);
        world.Events.Add(Scenario.EarthShakers[0], RecordFirstPuddles);
        ai.Move(Scenario.EarthShakers[0] + StepOffTheShaker, () => AiMove.Create(SecondShakersOutFirstBackToWait()).NaturalOrder(), jitter: 0f, sprint: true);
        AdaptBotShakers(ai, Scenario.EarthShakers[0], Scenario.EarthShakers[1], state.SecondEarthShakers, SecondShakerRadius);
        ai.Move(Scenario.EarthShakers[1] + StepOffTheShaker, () => AiMove.Create(EveryoneOnTheWaitingSpot()).NaturalOrder(), jitter: 0f, sprint: true);
    }

    protected virtual void AssignHatchesAndEdges()
    {
        hatchByNeurolink = ClosestAssignment(state.HatchTargets, Neurolinks.Select(BearingOf).ToArray());
        waitingAtEdge = ClosestAssignment(Enum.GetValues<PartyRole>().Except(state.HatchTargets).ToList(), EdgeBearings);
    }

    protected virtual Vector2?[] HomeSpots()
    {
        var spots = new Vector2?[8];
        foreach (var (role, bearing) in HomeBearings) spots[(int)role] = AtBearing(bearing, HomeRadius);
        return spots;
    }

    private Vector2?[] HatchesIntoNeurolinksOthersToTheEdge()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < hatchByNeurolink.Count; i++) spots[(int)hatchByNeurolink[i]] = Neurolinks[i];
        for (var i = 0; i < waitingAtEdge.Count; i++) spots[(int)waitingAtEdge[i]] = AtBearing(EdgeBearings[i], EdgeRadius);
        return spots;
    }

    private Vector2?[] HatchesWalkBackAndCuttersStepIn()
    {
        var spots = new Vector2?[8];
        for (var i = 0; i < hatchByNeurolink.Count; i++)
        {
            spots[(int)hatchByNeurolink[i]] = OutBehind(Neurolinks[i]);
            spots[(int)waitingAtEdge[EdgeCuttingIntoNeurolink[i]]] = Neurolinks[i];
        }
        return spots;
    }

    protected virtual Vector2 OutBehind(Vector2 neurolink) => Vector2.Normalize(neurolink) * WallRadius;

    private Vector2?[] FirstShakersOutOthersWaiting()
    {
        var spots = Cluster(WaitingSpot, state.SecondEarthShakers);
        Place(spots, ShakerSpots(state.FirstEarthShakers, FirstShakerRadius));
        return spots;
    }

    private Vector2?[] SecondShakersOutFirstBackToWait()
    {
        var spots = Cluster(WaitingSpot, state.FirstEarthShakers);
        Place(spots, ShakerSpots(state.SecondEarthShakers, SecondShakerRadius));
        return spots;
    }

    private void AdaptBotShakers(AiManager ai, float from, float cone, IReadOnlyList<PartyRole> shakers, float radius)
    {
        for (var at = from + FirstAdaptAfterMarker; at <= cone - LastAdaptBeforeCone; at += AdaptEvery)
            ai.Move(at, () => AiMove.Create(BotShakersClearOfPeople(shakers, radius)).NaturalOrder(), jitter: 0f);
    }

    private void RecordFirstPuddles()
    {
        firstPuddles.Clear();
        foreach (var role in state.FirstEarthShakers)
            if (world.Party.Get(role) is { } member && member.IsAlive())
                firstPuddles.Add(Flat(member.Position));
    }

    private Vector2?[] BotShakersClearOfPeople(IReadOnlyList<PartyRole> shakers, float radius)
    {
        var defaults = DefaultShakerBearings(shakers);
        var bots = shakers.Where(IsBot).OrderBy(r => defaults[r]).ToList();
        var people = PeopleBearings();
        var wanted = bots.Select(r => defaults[r]).ToArray();
        var bearings = ShakerBearingsClearOf(people, wanted, radius) ?? wanted;
        var spots = new Vector2?[8];
        for (var i = 0; i < bots.Count; i++) spots[(int)bots[i]] = AtBearing(bearings[i], radius);
        return spots;
    }

    private float[]? ShakerBearingsClearOf(IReadOnlyList<float> people, float[] wanted, float radius)
    {
        if (wanted.All(b => Fits(b, people, radius, ConeClearances[0]))) return wanted;
        foreach (var clearance in ConeClearances)
            if (ClosestSpreadBearings(people, wanted, radius, clearance) is { } found)
                return found;
        return null;
    }

    private float[]? ClosestSpreadBearings(IReadOnlyList<float> people, float[] wanted, float radius, float clearance)
    {
        var candidates = Enumerable.Range((int)ShakerArcStart, (int)(ShakerArcEnd - ShakerArcStart) + 1)
            .Select(b => (float)b)
            .Where(b => Fits(b, people, radius, clearance))
            .ToArray();
        var n = wanted.Length;
        if (n == 0) return [];
        var cost = new float[n, candidates.Length];
        var from = new int[n, candidates.Length];
        for (var i = 0; i < n; i++)
            for (var c = 0; c < candidates.Length; c++)
            {
                cost[i, c] = float.PositiveInfinity;
                var own = MathF.Abs(candidates[c] - wanted[i]);
                if (i == 0)
                {
                    cost[i, c] = own;
                    continue;
                }
                for (var p = 0; p < c; p++)
                    if (candidates[c] - candidates[p] >= clearance && cost[i - 1, p] + own < cost[i, c])
                    {
                        cost[i, c] = cost[i - 1, p] + own;
                        from[i, c] = p;
                    }
            }
        var best = -1;
        for (var c = 0; c < candidates.Length; c++)
            if (float.IsFinite(cost[n - 1, c]) && (best < 0 || cost[n - 1, c] < cost[n - 1, best]))
                best = c;
        if (best < 0) return null;
        var chosen = new float[n];
        for (var i = n - 1; i >= 0; i--)
        {
            chosen[i] = candidates[best];
            best = from[i, best];
        }
        return chosen;
    }

    private bool Fits(float bearing, IReadOnlyList<float> people, float radius, float clearance)
        => people.All(p => AngleBetween(p, bearing) >= clearance)
           && firstPuddles.All(p => Vector2.Distance(p, AtBearing(bearing, radius)) >= PuddleClearance);

    private Dictionary<PartyRole, float> DefaultShakerBearings(IReadOnlyList<PartyRole> shakers)
    {
        var assigned = ClosestAssignment(shakers, ShakerBearings);
        return assigned.Select((role, i) => (role, i)).ToDictionary(x => x.role, x => ShakerBearings[x.i]);
    }

    private bool IsBot(PartyRole role) => world.Party.Get(role) is { } member && world.Party.IsBotDriven(member);

    private List<float> PeopleBearings()
        => Enum.GetValues<PartyRole>()
            .Select(r => world.Party.Get(r))
            .OfType<SimCharacter>()
            .Where(m => m.IsAlive() && !world.Party.IsBotDriven(m) && Flat(m.Position).Length() >= PeopleCountFrom)
            .Select(m => BearingOf(Flat(m.Position)))
            .ToList();

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);

    private Vector2?[] EveryoneOnTheWaitingSpot() => Cluster(WaitingSpot, Enum.GetValues<PartyRole>());

    private Vector2?[] ShakerSpots(IReadOnlyList<PartyRole> shakers, float radius)
    {
        var spots = new Vector2?[8];
        var assigned = ClosestAssignment(shakers, ShakerBearings);
        for (var i = 0; i < assigned.Count; i++) spots[(int)assigned[i]] = AtBearing(ShakerBearings[i], radius);
        return spots;
    }

    protected virtual float BearingToAssignFrom(PartyRole role) => HomeBearings[role];

    protected IReadOnlyList<PartyRole> ClosestAssignment(IReadOnlyList<PartyRole> roles, float[] bearings)
        => Permutations(roles.ToList())
            .OrderBy(order => order.Select((r, i) => AngleBetween(BearingToAssignFrom(r), bearings[i])).Max())
            .ThenBy(order => order.Select((r, i) => AngleBetween(BearingToAssignFrom(r), bearings[i])).Sum())
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

    protected static float AngleBetween(float a, float b)
    {
        var d = MathF.Abs(a - b) % 360f;
        return d > 180f ? 360f - d : d;
    }

    protected static float BearingOf(Vector2 at) => (MathF.Atan2(at.X, -at.Y) * 180f / MathF.PI + 360f) % 360f;

    protected static Vector2 AtBearing(float degrees, float radius)
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
