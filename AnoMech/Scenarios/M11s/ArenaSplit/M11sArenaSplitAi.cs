using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.M11s.Opener;
using static AnoMech.Scenarios.M11s.ArenaSplit.M11sArenaSplitScenario;

namespace AnoMech.Scenarios.M11s.ArenaSplit;

public sealed class M11sArenaSplitAi : IScenarioAi<M11sArenaSplitState>
{
    public string Name => "Hector";

    private const float BesideTheBoss = 2.5f;
    private const float TowerEdge = 2.5f;
    private const float InnerWall = 7.5f;
    private const float PairStep = 1.5f;
    private const float PuddleStep = 7f;
    private const float TetherCorner = 19.2f;
    private const float NumberMarker = 12.5f;
    private const float StackOnThePlatform = 9f;
    private const float SliceCorner = 24f;
    private const float SliceEnd = 18.5f;
    private const float CrownSpot = 16f;
    private const float CrownEnd = 15f;
    private const float LandingTime = 2f;

    private M11sArenaSplitState state = null!;
    private readonly Dictionary<PartyRole, (int Side, int Z)>[] finals = [new(), new()];
    private readonly Dictionary<PartyRole, (int Side, int Z)> startQuadrant = new();

    public void Run(M11sArenaSplitState stateParam, SimWorld world)
    {
        state = stateParam;
        PlanRounds();
        var ai = new AiManager(world);

        ai.Move(0.5f, () => ByRole(role => BesideTheBossOnYourSide(role)), jitter: 0f);
        for (var r = 0; r < M11sArenaSplitState.Rounds; r++)
        {
            var round = r;
            var before = round == 0 ? startQuadrant : finals[0];
            ai.Move(TowerCastAt[round] + 1f, () => ByRole(role => TowerStance(before[role], finals[round][role].Side)), jitter: 0f);
            ai.Move(TowerCastAt[round] + TowerResolves + LandingTime, () => ByRole(role => PuddleSpot(round, role, 0)), jitter: 0f, sprint: true);
            for (var w = 1; w <= 3; w++)
            {
                var wave = w;
                ai.Move(PuddleAt[round][wave - 1] + 0.05f, () => ByRole(role => wave < 3 ? PuddleSpot(round, role, wave) : FinalSpot(round, role)), jitter: 0f, sprint: true);
            }
        }
        ai.Move(BreathHitsAt[1] + 1f, () => ByRole(role => StackOnYourPlatform(role)));
        ai.Move(AvalancheCastAt + 0.2f, () => ByRole(role => LastTowerStance(role)), jitter: 0f, sprint: true);
        ai.Move(TowerCastAt[2] + TowerResolves + LandingTime, () => ByRole(role => SafeSlice(role)), jitter: 0f, sprint: true);
        ai.Move(AvalancheCastAt + AvalancheResolves + 0.3f, () => ByRole(role => InsideTheCrown(role)), jitter: 0f, sprint: true);
    }

    private static int Group(PartyRole role) => M11sConstants.LightParties[0].Contains(role) ? -1 : 1;

    private static int NorthOrSouth(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank or PartyRole.PhysRangedDps or PartyRole.CasterDps ? -1 : 1;

    private void PlanRounds()
    {
        foreach (var role in Enum.GetValues<PartyRole>()) startQuadrant[role] = (Group(role), NorthOrSouth(role));
        var platform = Enum.GetValues<PartyRole>().ToDictionary(r => r, Group);
        for (var round = 0; round < M11sArenaSplitState.Rounds; round++)
        {
            var target = new Dictionary<PartyRole, int>();
            foreach (var t in state.Tethers[round]) target[t.Role] = -t.Side;
            var free = Enum.GetValues<PartyRole>().Where(r => !target.ContainsKey(r)).ToList();
            foreach (var role in free) target[role] = platform[role];
            foreach (var side in new[] { -1, 1 })
            {
                var crowded = free.Where(r => target[r] == side).ToList();
                foreach (var extra in crowded.Skip(2)) target[extra] = -side;
            }
            if (round == 1) KeepHealersApart(target, free);
            var final = finals[round];
            foreach (var t in state.Tethers[round]) final[t.Role] = (target[t.Role], -MathF.Sign(t.Z));
            foreach (var side in new[] { -1, 1 })
            {
                var tetheredNorth = state.Tethers[round].Any(t => target[t.Role] == side && MathF.Sign(t.Z) > 0);
                var onSide = free.Where(r => target[r] == side).ToList();
                for (var i = 0; i < onSide.Count; i++) final[onSide[i]] = (side, i == 0 ? (tetheredNorth ? 1 : -1) : (tetheredNorth ? -1 : 1));
            }
            foreach (var role in Enum.GetValues<PartyRole>()) platform[role] = target[role];
        }
    }

    private static void KeepHealersApart(Dictionary<PartyRole, int> target, List<PartyRole> free)
    {
        var h1 = target[PartyRole.RegenHealer];
        var h2 = target[PartyRole.ShieldHealer];
        if (h1 != h2) return;
        var movable = new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer }.FirstOrDefault(free.Contains);
        if (!free.Contains(movable)) return;
        var swap = free.FirstOrDefault(r => r is not (PartyRole.RegenHealer or PartyRole.ShieldHealer) && target[r] != target[movable]);
        if (!free.Contains(swap)) return;
        (target[movable], target[swap]) = (target[swap], target[movable]);
    }

    private static Vector2? BesideTheBossOnYourSide(PartyRole role)
        => new Vector2(Group(role) * BesideTheBoss, NorthOrSouth(role) * 1f) + M11sOpenerAi.StackOffset(role) * 0.5f;

    private static Vector2 TowerOf((int Side, int Z) quadrant) => new(quadrant.Side * 16f, quadrant.Z * 11f);

    private static Vector2? TowerStance((int Side, int Z) quadrant, int toSide)
    {
        var tower = TowerOf(quadrant);
        return quadrant.Side != toSide
            ? tower - new Vector2(quadrant.Side * TowerEdge, 0f)
            : tower - new Vector2(0f, quadrant.Z * TowerEdge);
    }

    private int PairIndex(int round, PartyRole role)
    {
        var mine = finals[round][role];
        return finals[round].Where(kv => kv.Value == mine).Select(kv => kv.Key).OrderBy(r => r).ToList().IndexOf(role);
    }

    private Vector2? PuddleSpot(int round, PartyRole role, int wave)
    {
        var (side, z) = finals[round][role];
        var x = side * (InnerWall + PairIndex(round, role) * PairStep);
        return new Vector2(x, z * wave * PuddleStep);
    }

    private Vector2? FinalSpot(int round, PartyRole role)
    {
        var (side, z) = finals[round][role];
        var tethered = state.TetherOf(round, role) != null;
        return new Vector2(state.SafeX(round, side), z * (tethered ? TetherCorner : NumberMarker));
    }

    private Vector2? StackOnYourPlatform(PartyRole role)
        => new Vector2(finals[1][role].Side * StackOnThePlatform, 0f) + M11sOpenerAi.StackOffset(role);

    private Vector2? LastTowerStance(PartyRole role)
    {
        var side = finals[1][role].Side;
        var onSide = finals[1].Where(kv => kv.Value.Side == side).Select(kv => kv.Key).OrderBy(NorthOrSouth).ThenBy(r => r).ToList();
        var z = onSide.IndexOf(role) < 2 ? -1 : 1;
        return TowerStance((side, z), state.AvalancheSafeSide) + new Vector2(0f, (onSide.IndexOf(role) % 2) * 0.6f - 0.3f);
    }

    private Vector2? SafeSlice(PartyRole role)
        => new Vector2(state.AvalancheSafeSide * SliceCorner, state.AvalancheSafeZ * SliceEnd) + M11sOpenerAi.StackOffset(role);

    private Vector2? InsideTheCrown(PartyRole role)
        => new Vector2(state.AvalancheSafeSide * CrownSpot, state.AvalancheSafeZ * CrownEnd) + M11sOpenerAi.StackOffset(role);

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
