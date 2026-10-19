using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.Opener.M10sOpenerScenario;

namespace AnoMech.Scenarios.M10s.Opener;

public sealed class M10sOpenerAi : IScenarioAi<M10sOpenerState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2[] FloaterSpots = [new(-18.8f, 18.8f), new(2f, 19f), new(-14.1f, 13.4f), new(-18.6f, -4.9f)];
    private static readonly Vector2 WaitForCharges = new(-7f, -2f);
    private static readonly Vector2 ThirdLeavesTheCorner = new(-9f, 8.5f);
    private static readonly Vector2 CutbackBait = new(-4.8f, 4.6f);
    private static readonly float[] PyrotationDepths = [3.5f];
    private const float ArenaInner = 18f;

    private static readonly PartyRole[] AlongTheFire =
    [
        PartyRole.ShieldHealer, PartyRole.OffTank, PartyRole.MeleeDpsB, PartyRole.MeleeDpsA,
        PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.CasterDps, PartyRole.PhysRangedDps,
    ];

    private static readonly Vector2[] InfernoSpots =
    [
        new(-12.5f, -7f), new(-12f, -1.2f), new(-11f, 4.5f), new(-8f, 9.3f),
        new(-2.5f, 11.5f), new(3f, 13.2f), new(8.8f, 13.2f), new(14.6f, 13.2f),
    ];

    private const float FollowStep = 0.3f;
    private const float StepPastThePuddle = 5.5f;
    private const float PuddleRadius = M10sOpenerScenario.PuddleRadius;
    private const float PuddleMargin = 1.2f;
    private const float BaitSliceMargin = 0.15f;

    private M10sOpenerState state = null!;
    private SimWorld world = null!;

    public void Run(M10sOpenerState stateParam, SimWorld world)
    {
        state = stateParam;
        this.world = world;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(TanksShareTheBusterPartyBehind));
        ai.Move(LandAt + 0.1f, () => ByRole(role => Spot(WaitForCharges)));
        for (var i = 0; i < M10sOpenerState.Floaters; i++)
        {
            var n = i;
            ai.Move(FloaterStatusAt[n] + 0.3f, () => ByRole(role => FloaterTakesTheirSpot(role, n)), jitter: 0f);
            ai.Move(FloaterHitAt[n] + 0.1f, () => ByRole(role => ChargedFloaterLeaves(role, n)), jitter: 0f, sprint: true);
        }
        ai.Move(FloaterHitAt[2] + 1.1f, () => ByRole(role => state.FloaterNumber(role) == 3 ? Spot(WaitForCharges) : null), jitter: 0f, sprint: true);
        ai.Move(FloaterHitAt[3] + 1.5f, () => ByRole(SpreadAlongTheFire), jitter: 0f);
        ai.Move(InfernoHitAt + 0.12f, () => ByRole(role => Spot(CutbackBait + StackOffset(role))), jitter: 0f, sprint: true);
        ai.Move(CutbackHitAt + 0.7f, () => ByRole(role => AcrossTheBurntSide(role, 0)), jitter: 0f, sprint: true);
        for (var hit = 1; hit <= PyrotationHitAt.Length; hit++)
        {
            var h = hit;
            ai.Move(PyrotationHitAt[h - 1] + 0.1f, () => ByRole(role => AcrossTheBurntSide(role, h)), jitter: 0f, sprint: true);
        }
        for (var t = CutbackHitAt + 1.2f; t < PyrotationHitAt[^1] + 2f; t += FollowStep)
            ai.Move(t, () => ByRole(FollowAPlayerStackTarget), jitter: 0f, sprint: true);
        ai.Move(FireFadesAt + 0.5f, () => ByRole(role => new Vector2(0f, 0f) + StackOffset(role)));
    }

    private static Vector2? TanksShareTheBusterPartyBehind(PartyRole role) =>
        role.IsTank() ? new Vector2(0f, 2.5f) + StackOffset(role) : new Vector2(0f, -9f) + StackOffset(role);

    private Vector2? FloaterTakesTheirSpot(PartyRole role, int n) =>
        state.FloaterNumber(role) == n + 1 ? Spot(FloaterSpots[n]) : null;

    private Vector2? ChargedFloaterLeaves(PartyRole role, int n)
    {
        if (state.FloaterNumber(role) != n + 1) return null;
        return n == 2 ? Spot(ThirdLeavesTheCorner) : Spot(WaitForCharges);
    }

    private Vector2? SpreadAlongTheFire(PartyRole role) => Spot(InfernoSpots[Array.IndexOf(AlongTheFire, role)]);

    private Vector2? AcrossTheBurntSide(PartyRole role, int hit)
    {
        if (hit > 0 && world.Party.Get(state.PyrotationTarget) is { } target && !world.Party.IsBotDriven(target)) return null;
        if (state.SafeSliceRotation is not { } bait) return Spot(CutbackBait + StackOffset(role));
        var origin = new Vector2(state.CutbackOrigin.X, state.CutbackOrigin.Z);
        var away = -new Vector2(MathF.Sin(bait), MathF.Cos(bait));
        if (hit == 0 || state.PyrotationSpots.Count == 0) return origin + away * PyrotationDepths[0] + StackOffset(role) * 0.5f;
        return ClearOfEveryPuddle() + StackOffset(role) * 0.5f;
    }

    private Vector2 ClearOfEveryPuddle()
    {
        var last = Flat(state.PyrotationSpots[^1]);
        var from = state.PyrotationSpots.Count > 1 ? Flat(state.PyrotationSpots[^2]) : new Vector2(state.CutbackOrigin.X, state.CutbackOrigin.Z);
        var heading = last - from;
        heading = heading.LengthSquared() > 0.01f ? Vector2.Normalize(heading) : Vector2.UnitX;
        foreach (var turn in TurnsToTry)
        {
            var direction = Rotate(heading, turn * MathF.PI / 180f);
            for (var step = StepPastThePuddle; step <= StepPastThePuddle + 4f; step += 1f)
            {
                var spot = last + direction * step;
                if (IsClear(spot)) return spot;
            }
        }
        return last + heading * StepPastThePuddle;
    }

    private static readonly float[] TurnsToTry = [0f, 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 180f];

    private bool IsClear(Vector2 spot)
    {
        if (!Inside(spot)) return false;
        var clearance = PuddleRadius + PuddleMargin;
        if (state.PyrotationSpots.Any(p => Vector2.Distance(Flat(p), spot) < clearance)) return false;
        if (state.InfernoPuddles.Any(p => Vector2.Distance(Flat(p), spot) < clearance)) return false;
        return !InTheBaitSlice(spot);
    }

    private bool InTheBaitSlice(Vector2 spot)
    {
        if (state.SafeSliceRotation is not { } bait) return false;
        var d = spot - new Vector2(state.CutbackOrigin.X, state.CutbackOrigin.Z);
        if (d.LengthSquared() < 0.01f) return true;
        var toward = new Vector2(MathF.Sin(bait), MathF.Cos(bait));
        return Vector2.Dot(Vector2.Normalize(d), toward) >= MathF.Cos(MathF.PI - M10sActions.CutbackBlazeHalfAngle + BaitSliceMargin);
    }

    private static Vector2 Rotate(Vector2 v, float radians) =>
        new(v.X * MathF.Cos(radians) - v.Y * MathF.Sin(radians), v.X * MathF.Sin(radians) + v.Y * MathF.Cos(radians));

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);

    private Vector2? FollowAPlayerStackTarget(PartyRole role)
    {
        if (world.Party.Get(state.PyrotationTarget) is not { } target || !target.IsAlive() || world.Party.IsBotDriven(target)) return null;
        var at = new Vector2(target.Position.X, target.Position.Z);
        if (state.PyrotationSpots.Count == PyrotationHitAt.Length && Vector2.Distance(at, Flat(state.PyrotationSpots[^1])) < PuddleRadius + PuddleMargin)
            at = ClearOfEveryPuddle();
        return at + StackOffset(role) * 0.5f;
    }

    private static bool Inside(Vector2 p) => MathF.Abs(p.X) <= ArenaInner && MathF.Abs(p.Y) <= ArenaInner;

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private Vector2 Spot(Vector2 westFrame)
    {
        var p = state.FromWestFrame(westFrame);
        return new Vector2(p.X, p.Z);
    }

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
