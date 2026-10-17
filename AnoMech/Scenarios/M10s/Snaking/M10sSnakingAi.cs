using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.M10sConstants;
using static AnoMech.Scenarios.M10s.Snaking.M10sSnakingScenario;

namespace AnoMech.Scenarios.M10s.Snaking;

public sealed class M10sSnakingAi : IScenarioAi<M10sSnakingState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2 ByRedHot = new(8f, 0f);
    private static readonly Vector2 ByDeepBlue = new(-8f, 0f);
    private static readonly Vector2 WaitForTheCorner = new(-11f, 0f);
    private static readonly Vector2[] FireBox = [new(12.5f, 4f), new(18.7f, 2.5f), new(12.5f, -2.2f), new(19f, -3.8f)];
    private static readonly float[] CornerBearings = [2.99f, 2.45f, 0.07f, 0.73f];
    private const float CornerRadius = 4.5f;
    private const float DodgeTurn = MathF.PI / 8f;
    private const float BusterX = 6f;
    private const float PartyX = 14f;
    private const float EastLaneZ = 9f;

    private M10sSnakingState state = null!;
    private SimWorld world = null!;

    public void Run(M10sSnakingState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => (M10sSnakingState.StartsByRedHot(role) ? ByRedHot : ByDeepBlue) + StackOffset(role)));
        ai.Move(SnakingHitAt + 0.6f, () => ByRole(role => IsFire(role) ? FireBox[PlaceInElement(role)] : WaitForTheCorner + StackOffset(role)), jitter: 0f);
        ai.Move(DeepBlueDashAt + 0.2f, () => ByRole(role => IsFire(role) ? null : AroundDeepBlue(role, 0f)), jitter: 0f, sprint: true);
        ai.Move(InfernoHitAt + 0.3f, () => ByRole(role => IsFire(role) ? AlongTheEastLane(role) : null), jitter: 0f, sprint: true);
        ai.Move(FirstConesAt + 0.25f, () => ByRole(role => !IsFire(role) && state.DoubleDip ? AroundDeepBlue(role, DodgeTurn) : null), jitter: 0f, sprint: true);
        ai.Move(SecondConesAt + 0.3f, () => ByRole(AlongTheEastLane), jitter: 0f, sprint: true);
        ai.Move(HotImpactHitAt + 0.3f, () => ByRole(role => Vector2.Zero + StackOffset(role)));
    }

    private bool IsFire(PartyRole role) => world.Party.Get(role)?.HasStatus(StatusId.Firesnaking) == true;

    private int PlaceInElement(PartyRole role)
    {
        var fire = IsFire(role);
        var same = Enum.GetValues<PartyRole>().Where(r => IsFire(r) == fire).OrderBy(Kind).ThenBy(r => r).ToList();
        return Math.Clamp(same.IndexOf(role), 0, 3);
    }

    private static int Kind(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => 0,
        PartyRole.RegenHealer or PartyRole.ShieldHealer => 1,
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => 2,
        _ => 3,
    };

    private float Side => state.DeepBlueSouth ? 1f : -1f;

    private Vector2? AroundDeepBlue(PartyRole role, float dodge)
    {
        var bearing = CornerBearings[PlaceInElement(role)];
        if (!state.DeepBlueSouth) bearing = MathF.PI - bearing;
        if (dodge != 0f) bearing += MathF.Sign(MathF.PI / 2f - bearing) * dodge;
        var corner = new Vector2(state.DeepBlueCorner.X, state.DeepBlueCorner.Z);
        return corner + new Vector2(MathF.Sin(bearing), MathF.Cos(bearing)) * CornerRadius;
    }

    private Vector2? AlongTheEastLane(PartyRole role) =>
        Kind(role) == 0
            ? new Vector2(BusterX, Side * EastLaneZ) + StackOffset(role)
            : new Vector2(PartyX, Side * EastLaneZ) + StackOffset(role);

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
