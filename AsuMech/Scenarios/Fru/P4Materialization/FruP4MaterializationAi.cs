using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.P4Materialization.FruP4MaterializationState;

namespace AsuMech.Scenarios.Fru.P4Materialization;

public sealed class FruP4MaterializationAi : IScenarioAi<FruP4MaterializationState>
{
    public string Name => "NA";
    public string Group => "NA";

    public void Run(FruP4MaterializationState state, SimWorld world)
    {
        var ai = new AiManager(world);
        ai.Move(0.5f, () => ByRole(AroundTheUsurperSouth), arrivalTime: TargetableAt);
    }

    private static Vector3? AroundTheUsurperSouth(PartyRole role) => role switch
    {
        PartyRole.MainTank => new Vector3(0, 0, 6),
        PartyRole.OffTank => new Vector3(2.5f, 0, 6.5f),
        PartyRole.MeleeDpsA => new Vector3(-3.5f, 0, 10),
        PartyRole.MeleeDpsB => new Vector3(3.5f, 0, 10),
        PartyRole.RegenHealer => new Vector3(-3, 0, 3),
        PartyRole.ShieldHealer => new Vector3(3, 0, 3),
        PartyRole.PhysRangedDps => new Vector3(-5.5f, 0, 4.5f),
        _ => new Vector3(5.5f, 0, 4.5f),
    };

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}
