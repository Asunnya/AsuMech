using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Fru.P5ParadiseLost;

public sealed class FruP5ParadiseLostAi : IScenarioAi<FruP5ParadiseLostState>
{
    public string Name => "Standard";

    public void Run(FruP5ParadiseLostState state, SimWorld world)
    {
        var ai = new AiManager(world);
        ai.Move(2f, () => ByRole(UptimeAroundPandora), arrivalTime: 5f);
    }

    private static Vector3 UptimeAroundPandora(PartyRole role)
    {
        if (role == PartyRole.MainTank) return new(0f, 0f, -4f);
        var bearing = MathF.PI / 6f + ((int)role - 1) * MathF.PI / 9f;
        return new(MathF.Cos(bearing) * 4f, 0f, MathF.Sin(bearing) * 4f);
    }

    private static IAiMove ByRole(Func<PartyRole, Vector3> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => (Vector2?)new Vector2(at(role).X, at(role).Z)).ToArray()).NaturalOrder();
}
