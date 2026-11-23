using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Fru;

public static class FruCyclonicBreakDance
{
    private static readonly float[] ClockDegrees = [0f, 90f, 270f, 180f, 225f, 135f, 315f, 45f];
    private static readonly float[] SpreadRadius = [5f, 12f, 13f, 15f, 8f, 6f, 12f, 16f];
    private const float PairRadius = 8f;
    private const float HalfCone = 22.5f;

    public static void Dance(AiManager ai, float bait, bool fire)
    {
        ai.Move(bait - 10f, () => ByRole(Spoke), jitter: 0f, arrivalTime: bait - 0.5f);
        ai.Move(bait + 0.05f, () => ByRole(role => fire ? PairUp(role) : Clockwise(role)), jitter: 0f, arrivalTime: bait + 1.85f);
        ai.Move(bait + FruCyclonicBreak.ElementAfterBait + 0.05f, () => ByRole(Spoke), jitter: 0f, arrivalTime: bait + 3.9f);
        ai.Move(bait + FruCyclonicBreak.FirstTurnAfterBait + 0.05f, () => ByRole(Clockwise), jitter: 0f, arrivalTime: bait + 6f);
    }

    public static Vector3 Spoke(PartyRole role) => Point(ClockDegrees[(int)role], SpreadRadius[(int)role]);

    public static Vector3 Clockwise(PartyRole role) => Point(ClockDegrees[(int)role] + HalfCone, SpreadRadius[(int)role]);

    private static Vector3 PairUp(PartyRole role)
        => Point(ClockDegrees[(int)role] + (role >= PartyRole.MeleeDpsA ? HalfCone : -HalfCone), PairRadius);

    public static Vector3 Point(float degrees, float radius)
    {
        var angle = degrees * MathF.PI / 180f;
        return new Vector3(MathF.Sin(angle) * radius, 0, -MathF.Cos(angle) * radius);
    }

    public static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();

    public static IAiMove ByRole(Func<PartyRole, Vector3> at) => ByRole(role => (Vector3?)at(role));
}
