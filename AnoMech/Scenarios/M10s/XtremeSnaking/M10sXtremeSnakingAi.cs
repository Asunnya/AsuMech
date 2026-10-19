using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.M10s.InsaneAir;
using static AnoMech.Scenarios.M10s.XtremeSnaking.M10sXtremeSnakingScenario;

namespace AnoMech.Scenarios.M10s.XtremeSnaking;

public sealed class M10sXtremeSnakingAi : IScenarioAi<M10sXtremeSnakingState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2 ByRedHot = new(8f, 0f);
    private static readonly Vector2 ByDeepBlue = new(-8f, 0f);
    private const float FanRadius = 4.5f;
    private static readonly float[] FanAngles = [-67.5f, -22.5f, 22.5f, 67.5f];
    private const float StackOut = 3.5f;
    private const float TankOut = 2f;
    private const float PartyAside = 7f;

    private M10sXtremeSnakingState state = null!;

    public void Run(M10sXtremeSnakingState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => (M10sXtremeSnakingState.StartsByRedHot(role) ? ByRedHot : ByDeepBlue) + StackOffset(role)));
        ai.Move(SnakingHitAt + 0.6f, () => ByRole(role => ForTheStop(role, 0)), jitter: 0f, sprint: true);
        for (var round = 1; round < M10sXtremeSnakingState.Rounds; round++)
        {
            var r = round;
            ai.Move(StopAt[r - 1] + StopToHit + 0.6f, () => ByRole(role => ForTheStop(role, r)), jitter: 0f, sprint: true);
        }
        ai.Move(StopAt[^1] + StopToHit + 0.8f, () => ByRole(role => Vector2.Zero + StackOffset(role)));
    }

    private static int Kind(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => 0,
        PartyRole.RegenHealer or PartyRole.ShieldHealer => 1,
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => 2,
        _ => 3,
    };

    private int WashedOffInRound(PartyRole role)
    {
        if (Kind(role) == 0) return state.VerticalRound;
        var others = Enumerable.Range(0, M10sXtremeSnakingState.Rounds).Where(r => r != state.VerticalRound).ToList();
        return others[Kind(role) - 1];
    }

    private bool AtRedHotsBoard(PartyRole role, int round)
    {
        var fire = state.Fire.Contains(role);
        var crossed = Kind(role) == 1 || round >= WashedOffInRound(role);
        return fire != crossed;
    }

    private Vector2? ForTheStop(PartyRole role, int round)
    {
        var red = AtRedHotsBoard(role, round);
        var board = red ? state.Boards.RedHotStop(round) : state.Boards.DeepBlueStop(round);
        var other = red ? state.Boards.DeepBlueStop(round) : state.Boards.RedHotStop(round);
        var outward = board.LengthSquared() > 1f ? Vector2.Normalize(board) : Vector2.Normalize(board - other);
        var place = Kind(role);
        return state.Moves[round] switch
        {
            SurfMove.Snap => board + Rotate(outward, FanAngles[place] * MathF.PI / 180f) * FanRadius,
            SurfMove.Reentry => board + outward * StackOut + StackOffset(role) * 0.5f,
            _ => place == 0 ? board + outward * TankOut : board + AwayFromTheOtherBoard(outward, board, other) * PartyAside + outward * (place - 2),
        };
    }

    private static Vector2 AwayFromTheOtherBoard(Vector2 outward, Vector2 board, Vector2 other)
    {
        var aside = new Vector2(-outward.Y, outward.X);
        return Vector2.Dot(aside, other - board) > 0f ? -aside : aside;
    }

    private static Vector2 Rotate(Vector2 v, float radians) =>
        new(v.X * MathF.Cos(radians) - v.Y * MathF.Sin(radians), v.X * MathF.Sin(radians) + v.Y * MathF.Cos(radians));

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
