using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.ArenaSplit.M10sArenaSplitScenario;
using Actions = AnoMech.Scenarios.M10s.M10sActions;

namespace AnoMech.Scenarios.M10s.ArenaSplit;

public sealed class M10sArenaSplitAi : IScenarioAi<M10sArenaSplitState>
{
    public string Name => "Hector (Toxic)";

    private const float CornerX = 18.5f;
    private const float FarColumnX = 5f;
    private const float OffTheFire = 4.8f;
    private const float InsideTheWalls = 19f;
    private const float AroundDeepBlue = 5f;
    private const float FarNorthernmost = 18.9f;
    private const float FarMiddle = 3.8f;
    private const float SteamClearance = 10f;
    private const float ReachBeforeTheSteam = 8f;
    private const float PairX = 10f;
    private const float NorthPairZ = -15.5f;
    private const float SouthPairZ = 10f;
    private const float StackX = 10f;
    private const float DodgeReach = 12f;
    private const float OwnHalfOverlap = 2f;
    private const float ConeMargin = 1.5f * MathF.PI / 180f;
    private const float SearchStep = 0.5f;

    private M10sArenaSplitState state = null!;
    private SimWorld world = null!;

    public void Run(M10sArenaSplitState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, () => ByRole(SpreadInTheCorner), jitter: 0f);
        ai.Move(InfernoHitAt + 0.3f, () => ByRole(TakeAProteanSpot), jitter: 0f, sprint: true);
        ai.Move(FirstConesAt + 0.1f, () => ByRole(DodgeTheSecondCones), jitter: 0f, sprint: true);
        ai.Move(SecondConesAt + 0.1f, () => ByRole(WaitClearOfTheSteam), jitter: 0f, sprint: true);
        ai.Move(FirstSteamAt + SteamResolves + 0.1f, () => ByRole(PairUpForPyrotation), jitter: 0f, sprint: true);
        ai.Move(PyrotationHitAt + 0.4f, () => ByRole(WaitForTheWave), jitter: 0f, sprint: true);
        ai.Move(WaveHitsAt + 0.95f, () => ByRole(StackWithTheLightParty), jitter: 0f, sprint: true);
    }

    private static bool North(PartyRole role) => role.IsTank() || role is PartyRole.PhysRangedDps or PartyRole.CasterDps;

    private static bool Ranged(PartyRole role) => role is PartyRole.PhysRangedDps or PartyRole.CasterDps;

    private static bool Healer(PartyRole role) => role is PartyRole.RegenHealer or PartyRole.ShieldHealer;

    private static float Side(PartyRole role) => M10sArenaSplitState.GroupSide(role);

    private bool NearDeepBlue(PartyRole role) => M10sArenaSplitState.GroupSide(role) == state.DeepBlueSide;

    private static Vector2? SpreadInTheCorner(PartyRole role)
    {
        var z = Ranged(role) ? -CornerX : role.IsTank() ? -12.5f : Healer(role) ? CornerX : 12.5f;
        return new Vector2(CornerX * Side(role), z);
    }

    private Vector2? TakeAProteanSpot(PartyRole role)
    {
        var side = Side(role);
        if (!NearDeepBlue(role))
        {
            var z = Ranged(role) ? -FarNorthernmost : role.IsTank() ? -FarMiddle : Healer(role) ? FarNorthernmost : FarMiddle;
            return new Vector2(FarColumnX * side, z);
        }
        var boss = Flat(state.DeepBlueSpot);
        var offset = Ranged(role) ? new Vector2(0f, -AroundDeepBlue)
            : role.IsTank() ? new Vector2(0.8f * AroundDeepBlue * side, -0.5f * AroundDeepBlue)
            : Healer(role) ? new Vector2(0f, AroundDeepBlue)
            : new Vector2(0.8f * AroundDeepBlue * side, 0.5f * AroundDeepBlue);
        return boss + offset;
    }

    private Vector2? DodgeTheSecondCones(PartyRole role)
    {
        if (world.Party.Get(role) is not { } me) return null;
        var here = Flat(me.Position);
        var boss = Flat(state.DeepBlueSpot);
        var cones = SecondConeBearings();
        var side = Side(role);
        var north = North(role);
        Vector2? best = null;
        var bestDistance = float.MaxValue;
        var fallback = here;
        var fallbackClearance = Clearance(here, boss, cones);
        for (var x = OffTheFire; x <= InsideTheWalls; x += SearchStep)
        for (var z = -InsideTheWalls; z <= InsideTheWalls; z += SearchStep)
        {
            var spot = new Vector2(x * side, z);
            if (north ? z > OwnHalfOverlap : z < -OwnHalfOverlap) continue;
            var distance = Vector2.Distance(spot, here);
            if (distance > DodgeReach) continue;
            if (state.InfernoPuddles.Any(p => Vector2.Distance(Flat(p), spot) < PuddleRadius + 0.8f)) continue;
            if (Vector2.Distance(spot, boss) < 2f) continue;
            var clearance = Clearance(spot, boss, cones);
            if (clearance > fallbackClearance + 1e-3f)
            {
                fallbackClearance = clearance;
                fallback = spot;
            }
            if (clearance < Actions.ProteanHalfAngle + ConeMargin || distance >= bestDistance) continue;
            bestDistance = distance;
            best = spot;
        }
        return best ?? fallback;
    }

    private List<float> SecondConeBearings() => state.DoubleDip
        ? [.. state.FirstCones]
        : [.. state.FirstCones.SelectMany(a => new[] { a - ReverseTurn, a + ReverseTurn })];

    private static float Clearance(Vector2 spot, Vector2 boss, List<float> cones)
    {
        if (cones.Count == 0) return MathF.PI;
        var d = spot - boss;
        var bearing = MathF.Atan2(d.X, d.Y);
        return cones.Min(c => MathF.Abs(MathF.IEEERemainder(bearing - c, 2f * MathF.PI)));
    }

    private Vector2? WaitClearOfTheSteam(PartyRole role)
    {
        if (world.Party.Get(role) is not { } me) return null;
        var here = Flat(me.Position);
        var pair = PairSpot(role);
        var bursts = state.InfernoPuddles.Select(Flat)
            .Concat(state.FirstCones.Select(c => StripCrossing(state.DeepBlueSpot, c)).OfType<Vector3>().Select(Flat))
            .ToList();
        var side = Side(role);
        Vector2? nearest = null;
        Vector2? towardThePair = null;
        var nearestDistance = float.MaxValue;
        var pairDistance = float.MaxValue;
        for (var x = OffTheFire; x <= InsideTheWalls; x += SearchStep)
        for (var z = -InsideTheWalls; z <= InsideTheWalls; z += SearchStep)
        {
            var spot = new Vector2(x * side, z);
            if (bursts.Any(b => Vector2.Distance(b, spot) < SteamClearance)) continue;
            var distance = Vector2.Distance(spot, here);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = spot;
            }
            if (distance > ReachBeforeTheSteam) continue;
            var toPair = Vector2.Distance(spot, pair);
            if (toPair >= pairDistance) continue;
            pairDistance = toPair;
            towardThePair = spot;
        }
        return towardThePair ?? nearest;
    }

    private static Vector2? PairUpForPyrotation(PartyRole role) => PairSpot(role) + PairOffset(role);

    private static Vector2 PairSpot(PartyRole role) => new(PairX * Side(role), North(role) ? NorthPairZ : SouthPairZ);

    private Vector2? WaitForTheWave(PartyRole role)
    {
        var side = Side(role);
        var before = StackSpot(role) - state.Push * WaveCarries;
        var x = side * MathF.Max(before.X * side, OffTheFire);
        return new Vector2(Math.Clamp(x, -InsideTheWalls, InsideTheWalls), Math.Clamp(before.Y, -InsideTheWalls, InsideTheWalls)) + PairOffset(role);
    }

    private static Vector2? StackWithTheLightParty(PartyRole role) => StackSpot(role) + PairOffset(role);

    private static Vector2 StackSpot(PartyRole role) => new(StackX * Side(role), 0f);

    private static Vector2 PairOffset(PartyRole role) => new(0f, 0.3f * ((int)role % 4 - 1.5f));

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
