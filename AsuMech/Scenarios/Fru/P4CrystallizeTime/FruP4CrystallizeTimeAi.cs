using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Scenarios.Fru.P4CrystallizeTime.FruP4CrystallizeTimeRoutes;
using static AsuMech.Scenarios.Fru.P4CrystallizeTime.FruP4CrystallizeTimeState;

namespace AsuMech.Scenarios.Fru.P4CrystallizeTime;

internal enum CrystallizeTimeAssignment { AeroWest, IceWest, Eruption, Ice, Darkness, Water, AeroEast, IceEast }

public sealed class FruP4CrystallizeTimeAi : IScenarioAi<FruP4CrystallizeTimeState>
{
    public string Name => "NA priority / 7-1 Akh Morn (MT solo)";
    public string Group => "NA";

    private const float RunSpeed = 6f;
    private const float ReferenceScale = 10f / 23.74f;
    private const float SpreadClearance = 6f;
    private const float FragmentClearance = 9f;
    private const float AeroKnockbackEndsAt = 27.45f;
    private const float StopCleansingAt = 41.1f;
    private const float ArrivalMargin = 0.25f;

    private static readonly PartyRole[] Priority =
        [PartyRole.ShieldHealer, PartyRole.RegenHealer, PartyRole.OffTank, PartyRole.MainTank,
         PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    private static readonly Vector3[] SpreadCandidates = CreateSpreadCandidates();

    private FruP4CrystallizeTimeState state = null!;
    private SimWorld world = null!;
    private AiManager ai = null!;
    private readonly Dictionary<PartyRole, CrystallizeTimeAssignment> assignments = new();
    private readonly Dictionary<PartyRole, CrystallizeTimeAssignment> puddleTargets = new();
    private readonly Dictionary<PartyRole, Vector3> spreadSpots = new();
    private readonly HashSet<PartyRole> knockedByAero = [];
    private readonly HashSet<PartyRole> holdingClaw = [];
    private readonly HashSet<PartyRole> holdingFang = [];
    private readonly Dictionary<PartyRole, int> moveOrders = new();
    private float elapsed;

    public void Run(FruP4CrystallizeTimeState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        ai = new AiManager(world);
        elapsed = 0f;
        puddleTargets.Clear();
        spreadSpots.Clear();
        knockedByAero.Clear();
        holdingClaw.Clear();
        holdingFang.Clear();
        moveOrders.Clear();
        AssignSidesByPriority();

        world.Events.Add(15.5f, () => FollowRoute(state.SlowNorthwest ? PRE_HG_1_NW : PRE_HG_1_NE, at: 15.5f, arriveBy: 22.7f));
        world.Events.Add(23.3f, () => FollowRoute(state.SlowNorthwest ? POST_HG_1_NW : POST_HG_1_NE, at: 23.3f, arriveBy: 25.9f));
        world.Events.Add(25.9f, RememberWhoAeroKnocksBack);
        world.Events.Add(26f, () => FollowRoute(PUDDLE_DODGE, at: 26f));
        world.Events.Add(26.7f, () => FollowRoute(state.SlowNorthwest ? POST_KB_NW : POST_KB_NE, at: 26.7f, arriveBy: 28.2f));
        world.Events.Add(28.5f, () => FollowRoute(state.SlowNorthwest ? POST_HG_2_NW : POST_HG_2_NE, at: 28.5f, arriveBy: 28.8f));
        world.Events.Add(29.8f, () => FollowRoute(state.East ? POST_UD_E : POST_UD_W, at: 29.8f, arriveBy: 31.5f));
        world.Events.Add(31.5f, () => FollowRoute(state.East ? POST_EARLY_SOAK_E : POST_EARLY_SOAK_W, at: 31.5f, arriveBy: 32f));
        world.Events.Add(33.6f, () => FollowRoute(state.East ? POST_HG_3_E : POST_HG_3_W, at: 33.6f, arriveBy: 34.1f));
        world.Events.Add(34.6f, () => FollowRoute(state.East ? POST_EXA_2_E : POST_EXA_2_W, at: 34.6f, arriveBy: 36.1f));
        world.Events.Add(36.5f, () => FollowRoute(state.Corner switch
        {
            CrystallizeCorner.NW => POST_EXA_3_NW,
            CrystallizeCorner.NE => POST_EXA_3_NE,
            CrystallizeCorner.SE => POST_EXA_3_SE,
            _ => POST_EXA_3_SW,
        }, at: 36.5f, arriveBy: 38.1f));
        world.Events.Add(38.9f, () => FollowRoute(state.Corner switch
        {
            CrystallizeCorner.NW => POST_EXA_4_NW,
            CrystallizeCorner.NE => POST_EXA_4_NE,
            CrystallizeCorner.SE => POST_EXA_4_SE,
            _ => POST_EXA_4_SW,
        }, at: 38.9f, arriveBy: 40.5f));
        world.Events.Add(41.1f, () => MoveEveryBot(RewindSpot, at: 41.1f, arriveBy: 42.5f));
        world.Events.Add(45.9f, spreadSpots.Clear);
        world.Events.Add(61.7f, () => MoveEveryBot(AkhMornSpot, at: 61.7f, arriveBy: 68.9f));
    }

    public void Tick(FruP4CrystallizeTimeState stateParam, SimWorld worldParam, float delta)
    {
        elapsed += delta;
        foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
        {
            var role = ((ISimPartyMember)member).Role;
            ReactToClawCaught(member, role);
            ReactToFangCleansed(member, role);
        }
        ChasePuddles();
        if (elapsed is >= 45.9f and < 48.5f) SpreadAwayFromPlayerForSpiritTaker();
    }

    private void AssignSidesByPriority()
    {
        assignments.Clear();
        foreach (var role in Enum.GetValues<PartyRole>())
            assignments[role] = state.Debuff(role) switch
            {
                CrystallizeDebuff.RedAero => IsWestOfPair(role) ? CrystallizeTimeAssignment.AeroWest : CrystallizeTimeAssignment.AeroEast,
                CrystallizeDebuff.RedIce => IsWestOfPair(role) ? CrystallizeTimeAssignment.IceWest : CrystallizeTimeAssignment.IceEast,
                CrystallizeDebuff.BlueEruption => CrystallizeTimeAssignment.Eruption,
                CrystallizeDebuff.BlueIce => CrystallizeTimeAssignment.Ice,
                CrystallizeDebuff.BlueDarkness => CrystallizeTimeAssignment.Darkness,
                _ => CrystallizeTimeAssignment.Water,
            };
    }

    private bool IsWestOfPair(PartyRole role) => Priority.First(r => state.Debuff(r) == state.Debuff(role)) == role;

    private PartyRole RoleOf(CrystallizeTimeAssignment assignment) => assignments.First(a => a.Value == assignment).Key;

    private IEnumerable<SimCharacter> Bots() => world.Party.ActiveMembers().Where(world.Party.IsBotDriven).ToList();

    private void FollowRoute(IReadOnlyDictionary<CrystallizeTimeAssignment, CrystallizeTimeDestination> route, float at, float? arriveBy = null, [CallerLineNumber] int line = 0)
    {
        foreach (var (assignment, destination) in route)
        {
            var role = RoleOf(assignment);
            puddleTargets.Remove(role);
            if (world.Party.Get(role) is not { } bot || !bot.IsAlive()) continue;
            var target = destination.Puddle is { } puddleOf && state.DragonPuddles.TryGetValue(RoleOf(puddleOf), out var puddle) ? puddle : destination.Position;
            ai.Order(bot, target, at, arriveBy, line: line);
            if (!world.Party.IsBotDriven(bot)) continue;
            if (destination.Puddle is { } source)
            {
                CancelPendingMove(role);
                puddleTargets[role] = source;
            }
            else
            {
                var leaveNoSoonerThan = knockedByAero.Contains(role) ? AeroKnockbackEndsAt : 0f;
                MoveLeavingAsLateAsPossible(bot, role, destination.Position, arriveBy, leaveNoSoonerThan);
            }
        }
    }

    private void MoveEveryBot(Func<PartyRole, Vector3> spot, float at, float arriveBy, [CallerLineNumber] int line = 0)
    {
        puddleTargets.Clear();
        foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
        {
            var role = ((ISimPartyMember)member).Role;
            ai.Order(member, spot(role), at, arriveBy, line: line);
            if (world.Party.IsBotDriven(member)) MoveLeavingAsLateAsPossible(member, role, spot(role), arriveBy);
        }
    }

    private void MoveLeavingAsLateAsPossible(SimCharacter bot, PartyRole role, Vector3 destination, float? arriveBy, float leaveNoSoonerThan = 0f)
    {
        var walk = Vector2.Distance(new Vector2(bot.Position.X, bot.Position.Z), new Vector2(destination.X, destination.Z)) / RunSpeed;
        var leaveAt = MathF.Max(leaveNoSoonerThan, arriveBy is { } deadline ? deadline - ArrivalMargin - walk : 0f);
        var order = CancelPendingMove(role);
        if (leaveAt <= elapsed)
        {
            bot.MoveTo(destination, RunSpeed);
            return;
        }
        world.Events.Add(leaveAt - elapsed, () =>
        {
            if (moveOrders[role] == order && bot.IsAlive()) bot.MoveTo(destination, RunSpeed);
        });
    }

    private void MoveNow(SimCharacter member, PartyRole role, Vector3 destination, [CallerLineNumber] int line = 0)
    {
        CancelPendingMove(role);
        ai.MoveNow(member, destination, RunSpeed, line: line);
    }

    private int CancelPendingMove(PartyRole role) => moveOrders[role] = moveOrders.GetValueOrDefault(role) + 1;

    private void RememberWhoAeroKnocksBack()
    {
        var aeros = state.Holders(CrystallizeDebuff.RedAero).Select(world.Party.Get).OfType<SimCharacter>().ToList();
        foreach (var member in world.Party.ActiveMembers())
            if (member is ISimPartyMember slot && aeros.Any(a => a != member && Vector3.Distance(a.Position, member.Position) <= 15f))
                knockedByAero.Add(slot.Role);
    }

    private void ReactToClawCaught(SimCharacter bot, PartyRole role)
    {
        var holds = bot.HasStatus(StatusId.Wyrmclaw);
        if (holds) { holdingClaw.Add(role); return; }
        if (!holdingClaw.Remove(role) || state.Debuff(role) != CrystallizeDebuff.RedAero) return;
        var escape = state.East ? POST_EARLY_SOAK_E : POST_EARLY_SOAK_W;
        MoveNow(bot, role, escape[assignments[role]].Position);
    }

    private void ReactToFangCleansed(SimCharacter bot, PartyRole role)
    {
        var holds = bot.HasStatus(StatusId.Wyrmfang);
        if (holds) { holdingFang.Add(role); return; }
        if (!holdingFang.Remove(role) || elapsed >= StopCleansingAt) return;
        puddleTargets.Remove(role);
        if (AfterCleanse.TryGetValue(assignments[role], out var next)) MoveNow(bot, role, next.Position);
    }

    private IReadOnlyDictionary<CrystallizeTimeAssignment, CrystallizeTimeDestination> AfterCleanse => state.Corner switch
    {
        CrystallizeCorner.NW => POST_SOAK_TARGET_NW,
        CrystallizeCorner.NE => POST_SOAK_TARGET_NE,
        CrystallizeCorner.SE => POST_SOAK_TARGET_SE,
        _ => POST_SOAK_TARGET_SW,
    };

    private void ChasePuddles()
    {
        foreach (var (role, source) in puddleTargets)
            if (world.Party.Get(role) is { } bot && bot.IsAlive()
                && state.DragonPuddles.TryGetValue(RoleOf(source), out var puddle))
                bot.MoveTo(puddle, RunSpeed);
    }

    private Vector3 RewindSpot(PartyRole role)
    {
        var groupOne = role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.PhysRangedDps;
        var tank = role.IsTank();
        var leadEast = state.Corner is CrystallizeCorner.NW or CrystallizeCorner.SE ? groupOne : !groupOne;
        var front = tank ? 22.7f : 20.5f;
        var back = tank ? 18.2f : 15.4f;
        var north = (state.North ? 1 : -1) * (leadEast ? back : front);
        var east = (state.East ? 1 : -1) * (leadEast ? front : back);
        return new Vector3(east * ReferenceScale, 0, -north * ReferenceScale);
    }

    private static Vector3 AkhMornSpot(PartyRole role) => role == PartyRole.MainTank ? new Vector3(-9.3f, 0, 0) : Vector3.Zero;

    private static Vector3[] CreateSpreadCandidates()
    {
        var spots = new List<Vector3>();
        for (var x = -18; x <= 18; x += 6)
            for (var z = -18; z <= 18; z += 6)
            {
                var spot = new Vector3(x, 0, z);
                if (spot.Length() <= 18.5f && Vector3.Distance(spot, FragmentPosition) >= FragmentClearance) spots.Add(spot);
            }
        return spots.ToArray();
    }

    private void SpreadAwayFromPlayerForSpiritTaker()
    {
        var player = world.Party.Player;
        foreach (var bot in Bots().OrderBy(b => ((ISimPartyMember)b).Role))
        {
            var role = ((ISimPartyMember)bot).Role;
            bool Clear(Vector3 spot) =>
                (!player.IsAlive() || world.Party.IsBotDriven(player!) || Vector3.Distance(spot, player!.Position) >= SpreadClearance)
                && spreadSpots.All(s => s.Key == role || Vector3.Distance(spot, s.Value) >= SpreadClearance);
            if (spreadSpots.TryGetValue(role, out var current) && Clear(current)) continue;
            var spot = SpreadCandidates.Where(Clear).OrderBy(c => Vector3.DistanceSquared(bot.Position, c)).Cast<Vector3?>().FirstOrDefault();
            if (spot is not { } chosen) continue;
            spreadSpots[role] = chosen;
            bot.MoveTo(chosen, RunSpeed);
        }
    }
}
