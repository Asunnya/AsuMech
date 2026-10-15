using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P4CrystallizeTime.FruP4CrystallizeTimeRoutes;
using static AnoMech.Scenarios.Fru.P4CrystallizeTime.FruP4CrystallizeTimeState;

namespace AnoMech.Scenarios.Fru.P4CrystallizeTime;

internal enum CrystallizeTimeAssignment { AeroWest, IceWest, Eruption, Ice, Darkness, Water, AeroEast, IceEast }

public sealed class FruP4CrystallizeTimeAi : IScenarioAi<FruP4CrystallizeTimeState>
{
    public string Name => "NA priority / 7-1 Akh Morn (MT solo)";
    public string Group => "NA";

    private const float RunSpeed = 6f;
    private const float ReferenceScale = 10f / 23.74f;
    private const float SpreadClearance = 6f;
    private const float AeroKnockbackEndsAt = 22.15f;
    private const float StopCleansingAt = 35.8f;

    private static readonly PartyRole[] Priority =
        [PartyRole.ShieldHealer, PartyRole.RegenHealer, PartyRole.OffTank, PartyRole.MainTank,
         PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    private static readonly Vector3[] SpreadCandidates = CreateSpreadCandidates();

    private FruP4CrystallizeTimeState state = null!;
    private SimWorld world = null!;
    private readonly Dictionary<PartyRole, CrystallizeTimeAssignment> assignments = new();
    private readonly Dictionary<PartyRole, CrystallizeTimeAssignment> puddleTargets = new();
    private readonly Dictionary<PartyRole, Vector3> spreadSpots = new();
    private readonly HashSet<PartyRole> knockedByAero = [];
    private readonly HashSet<PartyRole> holdingClaw = [];
    private readonly HashSet<PartyRole> holdingFang = [];
    private float elapsed;

    public void Run(FruP4CrystallizeTimeState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        elapsed = 0f;
        puddleTargets.Clear();
        spreadSpots.Clear();
        knockedByAero.Clear();
        holdingClaw.Clear();
        holdingFang.Clear();
        AssignSidesByPriority();

        world.Events.Add(14f, () => FollowRoute(state.SlowNorthwest ? PRE_HG_1_NW : PRE_HG_1_NE));
        world.Events.Add(18f, () => FollowRoute(state.SlowNorthwest ? POST_HG_1_NW : POST_HG_1_NE));
        world.Events.Add(20.6f, RememberWhoAeroKnocksBack);
        world.Events.Add(20.7f, () => FollowRoute(PUDDLE_DODGE));
        world.Events.Add(21.4f, () => FollowRoute(state.SlowNorthwest ? POST_KB_NW : POST_KB_NE));
        world.Events.Add(23.2f, () => FollowRoute(state.SlowNorthwest ? POST_HG_2_NW : POST_HG_2_NE));
        world.Events.Add(24.5f, () => FollowRoute(state.East ? POST_UD_E : POST_UD_W));
        world.Events.Add(26.2f, () => FollowRoute(state.East ? POST_EARLY_SOAK_E : POST_EARLY_SOAK_W));
        world.Events.Add(28.3f, () => FollowRoute(state.East ? POST_HG_3_E : POST_HG_3_W));
        world.Events.Add(29.3f, () => FollowRoute(state.East ? POST_EXA_2_E : POST_EXA_2_W));
        world.Events.Add(31.2f, () => FollowRoute(state.Corner switch
        {
            CrystallizeCorner.NW => POST_EXA_3_NW,
            CrystallizeCorner.NE => POST_EXA_3_NE,
            CrystallizeCorner.SE => POST_EXA_3_SE,
            _ => POST_EXA_3_SW,
        }));
        world.Events.Add(33.6f, () => FollowRoute(state.Corner switch
        {
            CrystallizeCorner.NW => POST_EXA_4_NW,
            CrystallizeCorner.NE => POST_EXA_4_NE,
            CrystallizeCorner.SE => POST_EXA_4_SE,
            _ => POST_EXA_4_SW,
        }));
        world.Events.Add(StopCleansingAt, () => MoveEveryBot(RewindSpot));
        world.Events.Add(40.6f, spreadSpots.Clear);
        world.Events.Add(56.4f, () => MoveEveryBot(AkhMornSpot));
    }

    public void Tick(FruP4CrystallizeTimeState stateParam, SimWorld worldParam, float delta)
    {
        elapsed += delta;
        foreach (var bot in Bots())
        {
            var role = ((ISimPartyMember)bot).Role;
            ReactToClawCaught(bot, role);
            ReactToFangCleansed(bot, role);
        }
        ChasePuddles();
        if (elapsed is >= 40.6f and < 43.4f) SpreadAwayFromPlayerForSpiritTaker();
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

    private void FollowRoute(IReadOnlyDictionary<CrystallizeTimeAssignment, CrystallizeTimeDestination> route)
    {
        foreach (var (assignment, destination) in route)
        {
            var role = RoleOf(assignment);
            puddleTargets.Remove(role);
            if (world.Party.Get(role) is not { } bot || !bot.IsAlive() || !world.Party.IsBotDriven(bot)) continue;
            if (destination.Puddle is { } source) puddleTargets[role] = source;
            else if (knockedByAero.Contains(role) && elapsed < AeroKnockbackEndsAt)
                world.Events.Add(AeroKnockbackEndsAt - elapsed, () => bot.MoveTo(destination.Position, RunSpeed));
            else bot.MoveTo(destination.Position, RunSpeed);
        }
    }

    private void MoveEveryBot(Func<PartyRole, Vector3> spot)
    {
        puddleTargets.Clear();
        foreach (var bot in Bots()) bot.MoveTo(spot(((ISimPartyMember)bot).Role), RunSpeed);
    }

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
        bot.MoveTo(escape[assignments[role]].Position, RunSpeed);
    }

    private void ReactToFangCleansed(SimCharacter bot, PartyRole role)
    {
        var holds = bot.HasStatus(StatusId.Wyrmfang);
        if (holds) { holdingFang.Add(role); return; }
        if (!holdingFang.Remove(role) || elapsed >= StopCleansingAt) return;
        puddleTargets.Remove(role);
        if (AfterCleanse.TryGetValue(assignments[role], out var next)) bot.MoveTo(next.Position, RunSpeed);
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
                if (spot.Length() <= 18.5f && Vector3.Distance(spot, FragmentPosition) >= SpreadClearance) spots.Add(spot);
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
