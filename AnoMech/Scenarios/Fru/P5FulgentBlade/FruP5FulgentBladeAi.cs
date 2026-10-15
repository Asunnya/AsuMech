using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

public sealed class FruP5FulgentBladeAi : IScenarioAi<FruP5FulgentBladeState>
{
    public string Name => "Standard";

    private const float S = FruP5FulgentBladeState.ReferenceScale;

    private FruP5FulgentBladeState state = null!;

    public void Run(FruP5FulgentBladeState stateParam, SimWorld world)
    {
        state = stateParam;
        var ai = new AiManager(world);
        ai.Move(8.5f, () => ByRole(role => role == PartyRole.MainTank ? MainTankSetup() : null));
        ai.Move(15.5f, () => ByRole(_ => state.Origin), jitter: 0f);
        ai.Move(19.5f, () => ByRole(_ => Dodge(0)), jitter: 0f);
        for (var step = 1; step < 6; step++)
        {
            var dodge = step;
            ai.Move(21.5f + step * 2f, () => ByRole(_ => Dodge(dodge)), jitter: 0f);
        }
        ai.Move(33.5f, () => ByRole(LightPartyStack));
    }

    private Vector3 MainTankSetup() => Vector3.Normalize(state.Origin) * 6f;

    private Vector3 Dodge(int step)
    {
        var offset = step switch
        {
            0 or 5 => new Vector3(1.15f, 0f, 2.77f) * (state.EastFirst ? -1f : 1f),
            1 or 4 => new Vector3(1.15f, 0f, 2.77f) * (state.EastFirst ? 1f : -1f),
            2 => new Vector3(-2.77f, 0f, 1.15f),
            _ => new Vector3(2.77f, 0f, -1.15f),
        };
        return state.Origin + Vector3.Transform(offset * S, Quaternion.CreateFromAxisAngle(Vector3.UnitY, state.Rotation));
    }

    private Vector3? LightPartyStack(PartyRole role)
    {
        var outward = Vector3.Normalize(state.Origin);
        var tangent = new Vector3(outward.Z, 0f, -outward.X);
        var side = (int)role % 2 == 0 ? -1f : 1f;
        return outward * (14.6f * S) + tangent * (side * 9.6f * S);
    }

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}
