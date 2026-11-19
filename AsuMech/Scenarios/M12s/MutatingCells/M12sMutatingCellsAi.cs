using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using static AsuMech.Scenarios.M12s.MutatingCells.M12sMutatingCellsScenario;

namespace AsuMech.Scenarios.M12s.MutatingCells;

public sealed class M12sMutatingCellsAi : IScenarioAi<M12sMutatingCellsState>
{
    public string Name => "Superchain Theory";

    private const float StepTowardTheHole = 1f;
    private const float BesideTheHole = 3f;

    private static readonly Vector2 SouthOfTheBoss = new(0f, 3f);
    private static readonly Vector2 NearStackOnTheBoss = new(0f, 2f);
    private static readonly Vector2 NearAlphaNorth = new(0f, -12f);
    private static readonly Vector2 FarStackSouth = new(0f, 17f);
    private static readonly Vector2 FarAlphaOnTheBoss = new(0f, -2f);
    private static readonly Vector2 SobatTankNorth = new(0f, -4f);
    private static readonly Vector2 PartyBehindTheSobat = new(0f, 5f);
    private const float PartyFromTheRepeat = 7f;
    private const float TanksFromTheRepeat = 4f;
    private const float TanksApart = 13f;

    private M12sMutatingCellsState state = null!;
    private SimWorld world = null!;

    public void Run(M12sMutatingCellsState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => SouthOfTheBoss + Spread(role, 1.5f)));
        ai.Move(OrbsFlyOutAt + 0.3f, () => ByRole(BetasPopTheMatchingOrbs), jitter: 0.2f);
        ai.Move(LastOrbAbsorbedBy + 0.2f, () => ByRole(role => UnderTheAeroHole(role, 0)), jitter: 0.2f);
        ai.Move(WaveAt[0] + 0.2f, () => ByRole(role => UnderTheAeroHole(role, 1)), jitter: 0.2f);
        ai.Move(WaveAt[1] + 0.2f, () => ByRole(SecondBetasTakeNetherworld));
        ai.Move(MutationEndsAt[1] + 0.3f, () => ByRole(LineUpForDoubleSobat));
        world.Events.Add(SobatBusterAt - 0.5f, InvulnTheSobatTank);
        ai.Move(SobatBusterAt + 0.2f, () => ByRole(DodgeTheRepeatAndSpreadTheTanks), jitter: 0.2f);
    }

    private static bool IsTank(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank;

    private Vector2? BetasPopTheMatchingOrbs(PartyRole role)
    {
        if (state.IsAlpha(role, 0)) return SouthOfTheBoss + Spread(role, 1.5f);
        var targets = state.OrbsToPop.OrderBy(o => o.Destination.Z).ThenBy(o => o.Destination.X).ToList();
        var orb = IsTank(role) ? targets[0] : targets[1];
        var hole = M12sMutatingCellsState.Hole(orb.Side);
        var spot = orb.Destination + Vector3.Normalize(hole - orb.Destination) * StepTowardTheHole;
        return new Vector2(spot.X, spot.Z);
    }

    private Vector2? UnderTheAeroHole(PartyRole role, int wave)
    {
        var side = wave == 0 ? state.FirstSafeSide : -state.FirstSafeSide;
        var hole = M12sMutatingCellsState.Hole(side);
        var north = state.IsAlpha(role, 1) ? -1f : 1f;
        return new Vector2(hole.X, north * BesideTheHole);
    }

    private Vector2? SecondBetasTakeNetherworld(PartyRole role)
    {
        var beta = !state.IsAlpha(role, 1);
        var at = state.NetherworldFar ? (beta ? FarStackSouth : FarAlphaOnTheBoss) : (beta ? NearStackOnTheBoss : NearAlphaNorth);
        return at + Spread(role, 1f);
    }

    private PartyRole SobatTank => new[] { PartyRole.MainTank, PartyRole.OffTank }.First(t => !state.IsAlpha(t, 1));

    private Vector2? LineUpForDoubleSobat(PartyRole role)
        => role == SobatTank ? SobatTankNorth : PartyBehindTheSobat + Spread(role, 1.5f);

    private void InvulnTheSobatTank()
    {
        if (world.Party.Get(SobatTank) is { } tank && tank.IsAlive() && world.Party.IsBotDriven(tank))
            DamageSolver.GiveBotInvuln(tank, 6f);
    }

    private Vector2? DodgeTheRepeatAndSpreadTheTanks(PartyRole role)
    {
        var heading = MathF.PI + state.SobatTurnRadians;
        var safe = -new Vector2(MathF.Sin(heading), MathF.Cos(heading));
        var across = new Vector2(-safe.Y, safe.X);
        return role switch
        {
            PartyRole.MainTank => safe * TanksFromTheRepeat + across * TanksApart,
            PartyRole.OffTank => safe * TanksFromTheRepeat - across * TanksApart,
            _ => safe * PartyFromTheRepeat + Spread(role, 1f),
        };
    }

    private static Vector2 Spread(PartyRole role, float radius)
    {
        var angle = (int)role * MathF.PI / 4f;
        return new Vector2(radius * MathF.Sin(angle), -radius * MathF.Cos(angle));
    }

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
