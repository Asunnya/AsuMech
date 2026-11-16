using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.Finale.M12sFinaleScenario;

namespace AsuMech.Scenarios.M12s.Finale;

public sealed class M12sFinaleAi : IScenarioAi<M12sFinaleState>
{
    public string Name => "DN";

    private const float TankSpot = 5f;
    private const float PartyBehind = 6f;
    private const float FinisherX = 8f;
    private const float FinisherZ = -4f;

    public void Run(M12sFinaleState state, SimWorld world)
    {
        var ai = new AiManager(world);
        ai.Move(0.5f, () => ByRole(TankInFrontPartyBehind), jitter: 0f);
        ai.Move(FirstKickAt + 0.3f, () => ByRole(TanksSpreadOutOfTheSecondKick), jitter: 0f, sprint: true);
        ai.Move(FinisherAt + 1f, () => ByRole(TankInFrontPartyBehind), jitter: 0f);
    }

    private static Vector2 Offset(PartyRole role)
    {
        var angle = (int)role * MathF.PI / 4f;
        return new Vector2(MathF.Sin(angle), -MathF.Cos(angle)) * 1.1f;
    }

    private static Vector2? TankInFrontPartyBehind(PartyRole role)
        => role == PartyRole.MainTank ? new Vector2(0f, -TankSpot) : new Vector2(0f, PartyBehind) + Offset(role);

    private static Vector2? TanksSpreadOutOfTheSecondKick(PartyRole role) => role switch
    {
        PartyRole.MainTank => new Vector2(-FinisherX, FinisherZ),
        PartyRole.OffTank => new Vector2(FinisherX, FinisherZ),
        _ => new Vector2(0f, PartyBehind + 1f) + Offset(role),
    };

    private static IAiMove ByRole(Func<PartyRole, Vector2?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(at).ToArray()).NaturalOrder();
}
