using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Legacy;
using AnoMech.Scenarios.M11s.Opener;
using static AnoMech.Scenarios.M11s.Ecliptic.M11sEclipticScenario;

namespace AnoMech.Scenarios.M11s.Ecliptic;

public sealed class M11sEclipticAi : IScenarioAi<M11sEclipticState>
{
    public string Name => "Toxic (Tank Tank Party)";

    private const float OutForTheMarkers = 10f;
    private const float WallLine = 18.6f;
    private const float InnerRow = 13.3f;
    private const float MeteorCircle = 8.5f;
    private const float CloseBait = 2f;
    private const float FarBait = 4.5f;
    private const float BehindTheLine = 16f;
    private static readonly float[] WallDrops = [9f, 3.3f, -2.5f, -9f];
    private static readonly float[] MeteorBearings = [MathF.PI, 2f * MathF.PI / 3f, MathF.PI / 3f, 0f, -MathF.PI / 3f];

    private M11sEclipticState state = null!;
    private SimWorld world = null!;
    private List<PartyRole>? marked;

    public void Run(M11sEclipticState stateParam, SimWorld world)
    {
        state = stateParam;
        this.world = world;
        marked = null;
        var ai = new AiManager(world);

        ai.Move(0.5f, () => ByRole(OutsBaitTheMarkersRestUnderTheBoss), jitter: 0f);
        ai.Move(AtomicMarkedAt + 0.15f, () => ByRole(role => IsMarked(role) ? WallDrop(role, 0) : UnderTheBoss(role)), jitter: 0f, sprint: true);
        for (var k = 1; k < AtomicHitAt.Length; k++)
        {
            var drop = k;
            ai.Move(AtomicHitAt[drop - 1] + 0.05f, () => ByRole(role => IsMarked(role) ? WallDrop(role, drop) : null), jitter: 0f, sprint: true);
        }
        for (var w = 1; w < MeteorBaitAt.Length; w++)
        {
            var wave = w;
            ai.Move(MeteorBaitAt[wave - 1] + 0.05f, () => ByRole(role => IsMarked(role) ? null : MeteorBait(role, wave)), jitter: 0f, sprint: true);
        }
        ai.Move(MeteorBaitAt[^1] + 0.05f, () => ByRole(role => IsMarked(role) ? null : UnderTheBoss(role)), jitter: 0f, sprint: true);
        ai.Move(MeteorBaitAt[^1] + MeteorResolves + 0.05f, () => ByRole(role => IsMarked(role) ? null : TowerSpot(role)), jitter: 0f, sprint: true);
        ai.Move(AtomicHitAt[^1] + 0.15f, () => ByRole(role => IsMarked(role) ? UnderTheBoss(role) : null), jitter: 0f, sprint: true);
        ai.Move(TetherAt + 0.1f, () => ByRole(role => Corner(role) is { } corner ? new Vector2(corner.X, TowerOf(role).Y) : UnderTheBoss(role)), jitter: 0f, sprint: true);
        ai.Move(TetherAt + 2.6f, () => ByRole(role => Corner(role)), jitter: 0f, sprint: true);
        ai.Move(WrathAt + 0.1f, () => ByRole(LineStackSpot), jitter: 0f, sprint: true);
        ai.Move(FireballLinesAt + 0.5f, () => ByRole(role => UnderTheBoss(role)));
        for (var k = 0; k < KickCastAt.Length; k++)
        {
            var kick = k;
            var soaker = KickSoaker(kick);
            ai.Move(KickCastAt[kick], () => ByRole(role => soaker is not { } tank ? UnderTheBoss(role) : role == tank ? InTheKickTower : OutOfTheKickTower + UnderTheBoss(role)), jitter: 0f, sprint: true);
            if (soaker is { } invulned)
                world.Events.Add(KickCastAt[kick] + FirstKickAfter - 0.5f, () => InvulnForTheKick(invulned, kick));
        }
    }

    private static readonly Vector2 InTheKickTower = new(0f, -2f);
    private static readonly Vector2 OutOfTheKickTower = new(0f, 8f);

    private static PartyRole? KickSoaker(int kick) => kick switch
    {
        0 => PartyRole.MainTank,
        1 => PartyRole.OffTank,
        _ => null,
    };

    private void InvulnForTheKick(PartyRole role, int kick)
    {
        if (world.Party.Get(role) is { } tank && tank.IsAlive() && world.Party.IsBotDriven(tank))
            DamageSolver.GiveBotInvuln(tank, (KickHits[kick] - 1) * KickEvery + 1.5f);
    }

    private static readonly PartyRole[] Outs = [PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.RegenHealer, PartyRole.ShieldHealer];

    private static Vector2? OutsBaitTheMarkersRestUnderTheBoss(PartyRole role) => role switch
    {
        PartyRole.PhysRangedDps => new Vector2(-OutForTheMarkers, -OutForTheMarkers),
        PartyRole.CasterDps => new Vector2(OutForTheMarkers, -OutForTheMarkers),
        PartyRole.RegenHealer => new Vector2(-OutForTheMarkers, OutForTheMarkers),
        PartyRole.ShieldHealer => new Vector2(OutForTheMarkers, OutForTheMarkers),
        _ => UnderTheBoss(role),
    };

    private static Vector2 UnderTheBoss(PartyRole role) => M11sOpenerAi.StackOffset(role);

    private List<PartyRole> Marked()
    {
        if (marked != null) return marked;
        var boss = Vector3.Zero;
        var priority = state.MarkPriority.ToList();
        marked = Enum.GetValues<PartyRole>()
            .Select(r => (Role: r, Member: world.Party.Get(r)))
            .Where(x => x.Member != null && x.Member.IsAlive())
            .OrderByDescending(x => MathF.Round(Vector3.Distance(x.Member!.Position, boss)))
            .ThenBy(x => priority.IndexOf(x.Role))
            .Take(2).Select(x => x.Role).ToList();
        return marked;
    }

    private bool IsMarked(PartyRole role) => Marked().Contains(role);

    private static int OwnWall(PartyRole role) => role is PartyRole.PhysRangedDps or PartyRole.CasterDps ? -1 : 1;

    private static int OwnSide(PartyRole role) => role is PartyRole.PhysRangedDps or PartyRole.RegenHealer ? -1 : 1;

    private int MammothSideOnWall(int wall) => state.MammothNortheast ? -wall : wall;

    private int Wall(PartyRole role)
    {
        var wall = OwnWall(role);
        var other = Marked().FirstOrDefault(r => r != role);
        if (OwnWall(other) != wall) return wall;
        var nearerTheMeteor = OwnSide(role) == MammothSideOnWall(wall);
        return nearerTheMeteor ? -wall : wall;
    }

    private Vector2? WallDrop(PartyRole role, int drop)
    {
        var wall = Wall(role);
        var safeSide = -MammothSideOnWall(wall);
        return drop switch
        {
            < 4 => new Vector2(safeSide * WallDrops[drop], wall * WallLine),
            4 => new Vector2(-safeSide * 6f, wall * InnerRow),
            _ => new Vector2(0f, wall * InnerRow),
        };
    }

    private static Vector2 MeteorBait(PartyRole role, int wave)
    {
        var bearing = MeteorBearings[wave - 1];
        return MeteorCircle * new Vector2(MathF.Sin(bearing), MathF.Cos(bearing)) + M11sOpenerAi.StackOffset(role);
    }

    private static readonly int[] Clockwise = [0, 1, 2, 3];
    private static readonly int[] Counterclockwise = [3, 2, 1, 0];

    private int TowerIndex(PartyRole role) => role switch
    {
        PartyRole.MainTank => Clockwise.First(state.IsSingleTower),
        PartyRole.OffTank => Counterclockwise.First(state.IsSingleTower),
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => Counterclockwise.First(i => !state.IsSingleTower(i)),
        _ => Clockwise.First(i => !state.IsSingleTower(i)),
    };

    private Vector2 TowerOf(PartyRole role)
    {
        var at = M11sEclipticState.Intercard(TowerIndex(role), M11sEclipticState.TowerOffset);
        return new Vector2(at.X, at.Z);
    }

    private Vector2? TowerSpot(PartyRole role) => TowerOf(role) + M11sOpenerAi.StackOffset(role) * 2f;

    private List<PartyRole> SoakersOf(int tower)
        => Enum.GetValues<PartyRole>().Where(r => !IsMarked(r) && TowerIndex(r) == tower).OrderBy(r => r).ToList();

    private PartyRole? TetheredIn(int tower)
    {
        var soakers = SoakersOf(tower);
        if (soakers.Count == 0) return null;
        if (state.IsSingleTower(tower)) return soakers[0];
        var pairIndex = Enumerable.Range(0, tower).Count(i => !state.IsSingleTower(i));
        return state.PairTetherFirst[Math.Min(pairIndex, 1)] || soakers.Count < 2 ? soakers[0] : soakers[1];
    }

    private Vector2? Corner(PartyRole role)
    {
        if (IsMarked(role)) return null;
        var tower = TowerIndex(role);
        if (TetheredIn(tower) != role) return null;
        var corner = M11sEclipticState.Intercard(M11sEclipticState.CornerForShade(state.ShadeForTower[tower]), M11sEclipticState.CornerOffset);
        return new Vector2(corner.X, corner.Z);
    }

    private Vector2? LineStackSpot(PartyRole role)
    {
        if (Corner(role) is { } corner) return new Vector2(MathF.Sign(corner.X) * BehindTheLine, MathF.Sign(corner.Y) * 0.6f);
        var free = Enum.GetValues<PartyRole>().Where(r => Corner(r) == null).ToList();
        var pairs = free.Where(r => !IsMarked(r)).OrderBy(r => r).ToList();
        var marks = free.Where(IsMarked).OrderBy(r => r).ToList();
        if (pairs.Contains(role)) return new Vector2(pairs.IndexOf(role) == 0 ? -CloseBait : CloseBait, 0f);
        return new Vector2(marks.IndexOf(role) == 0 ? -FarBait : FarBait, 0f);
    }

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
