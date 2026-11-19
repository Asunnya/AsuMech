using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Core.UserActions;

namespace AsuMech.Scenarios.Fru.P5PandorasBox;

public sealed class FruP5PandorasBoxAi : IScenarioAi<FruP5PandorasBoxState>
{
    public string Name => "Standard";

    private FruP5PandorasBoxState state = null!;
    private SimWorld world = null!;

    public void Run(FruP5PandorasBoxState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(2f, () => ByRole(StackBehindTheMainTank), jitter: 0f, arrivalTime: 6f);
        world.Events.Add(7.76f, TankLimitBreak);
    }

    private static Vector3 StackBehindTheMainTank(PartyRole role) => role switch
    {
        PartyRole.MainTank => new(0f, 0f, 4f),
        _ => new(((int)role - 4f) * 0.4f, 0f, 6.5f),
    };

    private void TankLimitBreak()
    {
        if (world.Party.Get(state.LimitBreakTank) is not ISimPartyMember tank || tank is not SimCharacter user) return;
        var limitBreak = LimitBreakHandler.ActionId(tank.ClassJob, 3);
        if (limitBreak != 0 && tank.UseAction(limitBreak)) LimitBreakHandler.Resolve(world.Party, user, limitBreak);
    }

    private static IAiMove ByRole(Func<PartyRole, Vector3> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => (Vector2?)new Vector2(at(role).X, at(role).Z)).ToArray()).NaturalOrder();
}
