using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.InsaneAir.M10sInsaneAirScenario;

namespace AnoMech.Scenarios.M10s.InsaneAir;

public sealed class M10sInsaneAirAi : IScenarioAi<M10sInsaneAirState>
{
    public string Name => "Hector (Toxic)";

    private static readonly Vector2 NorthWall = new(0f, -17.5f);
    private const float FanRadius = 4.5f;
    private static readonly float[] FanAngles = [-67.5f, -22.5f, 22.5f, 67.5f];
    private const float StackOut = 3.5f;
    private const float TankOut = 2f;
    private const float PartyIn = 6.5f;
    private const float PartySpread = 1.5f;

    private M10sInsaneAirState state = null!;

    public void Run(M10sInsaneAirState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);

        ai.Move(0.3f, () => ByRole(role => Vector2.Zero + StackOffset(role)));
        ai.Move(XtremeCastAt, () => ByRole(role => NorthWall + StackOffset(role)), jitter: 0f);
        ai.Move(InsaneAirAt, () => ByRole(role => ForTheStop(role, 0)), jitter: 0f, sprint: true);
        for (var round = 1; round < M10sInsaneAirState.Rounds; round++)
        {
            var r = round;
            ai.Move(StopAt[r - 1] + StopToHit + 0.3f, () => ByRole(role => ForTheStop(role, r)), jitter: 0f, sprint: true);
        }
        ai.Move(StopAt[^1] + StopToHit + 0.5f, () => ByRole(role => Vector2.Zero + StackOffset(role)));
    }

    private static bool FollowsDeepBlue(PartyRole role) =>
        role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.PhysRangedDps;

    private static int PlaceInGroup(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => 0,
        PartyRole.RegenHealer or PartyRole.ShieldHealer => 1,
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => 2,
        _ => 3,
    };

    private Vector2? ForTheStop(PartyRole role, int round)
    {
        var blue = FollowsDeepBlue(role);
        var board = blue ? state.Variant.DeepBlueStop(round) : state.Variant.RedHotStop(round);
        var other = blue ? state.Variant.RedHotStop(round) : state.Variant.DeepBlueStop(round);
        var move = blue ? state.Variant.DeepBlueMoves[round] : state.Variant.RedHotMoves[round];
        var outward = board.LengthSquared() > 1f ? Vector2.Normalize(board) : Vector2.Normalize(board - other);
        var place = PlaceInGroup(role);
        return move switch
        {
            SurfMove.Snap => board + Rotate(outward, FanAngles[place] * MathF.PI / 180f) * FanRadius,
            SurfMove.Reentry => board + outward * StackOut + StackOffset(role) * 0.5f,
            _ => place == 0
                ? board + outward * TankOut
                : board - outward * PartyIn + new Vector2(-outward.Y, outward.X) * PartySpread * (place - 2),
        };
    }

    private static Vector2 Rotate(Vector2 v, float radians) =>
        new(v.X * MathF.Cos(radians) - v.Y * MathF.Sin(radians), v.X * MathF.Sin(radians) + v.Y * MathF.Cos(radians));

    private static Vector2 StackOffset(PartyRole role) => new(0.3f * ((int)role % 3 - 1), 0.3f * ((int)role / 3 - 1));

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
