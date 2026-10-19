using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.SickestTakeoff.M10sSickestTakeoffScenario;

namespace AnoMech.Scenarios.M10s.SickestTakeoff;

public sealed class M10sSickestTakeoffAi : IScenarioAi<M10sSickestTakeoffState>
{
    public string Name => "Hector (Toxic)";

    private const float SideLane = 13f;
    private const float StackBeforeTheWave = -7f;
    private const float FrontRowBeforeTheWave = -4f;
    private const float BackRowBeforeTheWave = -10f;
    private const float SpreadAcross = 3f;
    private const float CloverRadius = 4f;
    private const float DodgeTurn = MathF.PI / 8f;
    private static readonly Vector2 DeepImpactBait = new(0f, -12f);
    private static readonly Vector2 AwayFromTheBait = new(0f, 2f);

    private M10sSickestTakeoffState state = null!;

    public void Run(M10sSickestTakeoffState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => role == PartyRole.MainTank ? new Vector2(0f, -3f) : new Vector2(0f, 3f) + StackOffset(role)));
        ai.Move(OrbsShownAt + 0.6f, () => ByRole(role => ReadyForTheWave(role, carried: false)), jitter: 0f);
        ai.Move(WaveHitsAt + 1.2f, () => ByRole(role => ReadyForTheWave(role, carried: true)), jitter: 0f);
        ai.Move(OrbsResolveAt + 0.5f, () => ByRole(CloverSpot), jitter: 0f, sprint: true);
        ai.Move(FirstConesAt + 0.25f, () => ByRole(DodgeIntoTheEmptySlice), jitter: 0f, sprint: true);
        ai.Move(SecondConesAt + 0.5f, () => ByRole(BaitDeepImpact), jitter: 0f, sprint: true);
        ai.Move(DiversDareAt, () => ByRole(role => Vector2.Zero + StackOffset(role)));
    }

    private static bool InGroupOne(PartyRole role) =>
        role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.PhysRangedDps;

    private float GroupLane(PartyRole role)
    {
        var free = new[] { -1, 0, 1 }.Where(l => l != state.Lane).OrderByDescending(l => l).ToArray();
        var lane = InGroupOne(role) ? free[0] : free[1];
        return lane * SideLane;
    }

    private Vector2? ReadyForTheWave(PartyRole role, bool carried)
    {
        var lane = GroupLane(role);
        var landed = carried ? M10sSickestTakeoffScenario.WaveCarries : 0f;
        if (!state.EightOrbs) return Flat(state.FromWaveFrame(StackBeforeTheWave + landed, lane)) + StackOffset(role);
        var front = role.IsTank() || role is PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;
        var right = role.IsTank() || role is PartyRole.RegenHealer or PartyRole.ShieldHealer;
        var forward = (front ? FrontRowBeforeTheWave : BackRowBeforeTheWave) + landed;
        return Flat(state.FromWaveFrame(forward, lane + (right ? -SpreadAcross : SpreadAcross)));
    }

    private static float CloverBearing(PartyRole role) => role switch
    {
        PartyRole.MainTank => 180f,
        PartyRole.OffTank => 0f,
        PartyRole.RegenHealer => 270f,
        PartyRole.ShieldHealer => 90f,
        PartyRole.MeleeDpsA => 315f,
        PartyRole.MeleeDpsB => 45f,
        PartyRole.PhysRangedDps => 225f,
        _ => 135f,
    };

    private static Vector2 OnTheClover(float radians) => new(CloverRadius * MathF.Sin(radians), CloverRadius * MathF.Cos(radians));

    private static Vector2? CloverSpot(PartyRole role) => OnTheClover(CloverBearing(role) * MathF.PI / 180f);

    private Vector2? DodgeIntoTheEmptySlice(PartyRole role) =>
        state.DoubleDip ? OnTheClover(CloverBearing(role) * MathF.PI / 180f + DodgeTurn) : null;

    private static Vector2? BaitDeepImpact(PartyRole role) =>
        role == PartyRole.MainTank ? DeepImpactBait : AwayFromTheBait + StackOffset(role);

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static Vector2 Flat(Vector3 p) => new(p.X, p.Z);

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
