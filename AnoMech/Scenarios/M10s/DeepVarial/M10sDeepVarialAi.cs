using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.DeepVarial.M10sDeepVarialScenario;

namespace AnoMech.Scenarios.M10s.DeepVarial;

public sealed class M10sDeepVarialAi : IScenarioAi<M10sDeepVarialState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2[] FireSpreadByTheWall = [new(6.5f, 17.5f), new(18.5f, 18.5f), new(12.5f, 18.5f), new(18.5f, 13f)];
    private static readonly Vector2 WaterStackByTheWall = new(-7f, 18f);
    private static readonly Vector2[] WaterSpreadByTheWall = [new(-6f, 18f), new(-12f, 18f), new(-18f, 18f), new(-18f, 12f)];
    private static readonly Vector2 WaterWaits = new(-10f, 0f);
    private static readonly Vector2 PuddleCorner = new(18f, 18f);
    private static readonly Vector2 EmptyCorner = new(18f, -18f);
    private static readonly Vector2 PuddleSideWait = new(18f, 9f);
    private static readonly Vector2 EmptySideWait = new(18f, -9f);
    private static readonly Vector2 AfterTheBait = new(-4f, 0f);
    private const float StackBeforeTheWave = -12f;
    private const float WaterSpreadAcross = 3f;
    private static readonly float[] WaterSpreadRows = [-5f, 1f];
    private const float WaterStackBeforeTheWave = -3f;
    private const float AcrossDepth = 3.5f;
    private const float DeepImpactCorner = 14f;
    private const float SteamResolves = FirstSteamAt + 3.3f;

    private M10sDeepVarialState state = null!;

    public void Run(M10sDeepVarialState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => state.IsFire(role) ? Mirror(FireSpreadByTheWall[Kind(role)]) : Mirror(WaterStackByTheWall) + StackOffset(role)), jitter: 0f, sprint: true);
        ai.Move(DeepVarialAt + 0.2f, () => ByRole(role => !state.IsFire(role) && state.VarialSpread ? Mirror(WaterSpreadByTheWall[Kind(role)]) : null), jitter: 0f, sprint: true);
        ai.Move(InfernoHitAt + 0.3f, () => ByRole(role => state.IsFire(role) ? (Kind(role) == 0 ? Mirror(PuddleCorner) : null) : WaterWaits + StackOffset(role)), jitter: 0f, sprint: true);
        ai.Move(SteamResolves, () => ByRole(FireWaitsForItsJump), jitter: 0f, sprint: true);
        ai.Move(JumpAt[0] + 0.1f, () => ByRole(role => state.IsFire(role) && Kind(role) == 1 ? Mirror(EmptyCorner) : null), jitter: 0f, sprint: true);
        ai.Move(JumpAt[0] + JumpToLanding + 0.3f, () => ByRole(role => !state.IsFire(role) ? null : Kind(role) switch
        {
            0 => AfterTheBait + StackOffset(role),
            2 => Mirror(PuddleCorner),
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(JumpAt[1] + JumpToLanding + 0.3f, () => ByRole(role => !state.IsFire(role) ? null : Kind(role) switch
        {
            1 => AfterTheBait + StackOffset(role),
            3 => Mirror(EmptyCorner),
            _ => null,
        }), jitter: 0f, sprint: true);
        ai.Move(TakeoffAt + 0.2f, () => ByRole(role => state.IsFire(role) && Kind(role) == 3 ? null : BeforeTheWave(role)), jitter: 0f, sprint: true);
        ai.Move(JumpAt[3] + JumpToLanding + 0.3f, () => ByRole(role => state.IsFire(role) && Kind(role) == 3 ? BeforeTheWave(role) : null), jitter: 0f, sprint: true);
        ai.Move(WaveHitsAt + 0.9f, () => ByRole(role => BeforeTheWave(role) + Pushed), jitter: 0f);
        ai.Move(TakeoffOrbsAt + 0.3f, () => ByRole(role => FireStack + Pushed + StackOffset(role)), jitter: 0f, sprint: true);
        ai.Move(CutbackHitAt + 0.7f, () => ByRole(AfterTheCutback), jitter: 0f, sprint: true);
        ai.Move(DeepImpactHitAt + 0.8f, () => ByRole(role => Vector2.Zero + StackOffset(role)));
    }

    private static int Kind(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => 0,
        PartyRole.RegenHealer or PartyRole.ShieldHealer => 1,
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => 2,
        _ => 3,
    };

    private Vector2 Mirror(Vector2 p) => new(p.X, p.Y * state.Side);

    private Vector2? FireWaitsForItsJump(PartyRole role)
    {
        if (!state.IsFire(role)) return null;
        return Kind(role) switch
        {
            0 => Mirror(PuddleCorner),
            2 => Mirror(PuddleSideWait),
            _ => Mirror(EmptySideWait),
        };
    }

    private float PartyLaneX => state.Lane == -1 ? -4f : -15f;

    private Vector2 FireStack => new(PartyLaneX, StackBeforeTheWave * state.Side);

    private Vector2 Pushed => new(0f, M10sDeepVarialScenario.WaveCarries * state.Side);

    private Vector2 BeforeTheWave(PartyRole role)
    {
        if (state.IsFire(role)) return FireStack + StackOffset(role);
        if (!state.TakeoffSpread) return new Vector2(PartyLaneX, WaterStackBeforeTheWave * state.Side) + StackOffset(role);
        var kind = Kind(role);
        var across = kind % 2 == 0 ? -WaterSpreadAcross : WaterSpreadAcross;
        return new Vector2(PartyLaneX + across, WaterSpreadRows[kind / 2] * state.Side);
    }

    private Vector2? AfterTheCutback(PartyRole role)
    {
        var origin = new Vector2(state.CutbackOrigin.X, state.CutbackOrigin.Z);
        var bait = state.SafeSliceRotation ?? 0f;
        var across = origin - new Vector2(MathF.Sin(bait), MathF.Cos(bait)) * AcrossDepth;
        if (Kind(role) != 0 || state.IsFire(role)) return across + StackOffset(role) * 0.5f;
        var deepBlue = new Vector2(DeepBlueLands.X, DeepBlueLands.Z);
        return new[] { new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f) }
            .Select(c => c * DeepImpactCorner)
            .OrderByDescending(c => MathF.Min(Vector2.Distance(c, across), Vector2.Distance(c, deepBlue)))
            .First();
    }

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
