using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Ucob.P5Exaflares;

public sealed class UcobP5ExaflaresAi : IScenarioAi<UcobP5ExaflaresState>
{
    public string Name => "Dodge";

    private AiManager ai = null!;

    public void Run(UcobP5ExaflaresState state, SimWorld world)
    {
        ai = new AiManager(world);
        new UcobExaflareDodge(state.Pattern, world, MoveToTheSafestLaneSpot, HoldOnTheLane).Schedule();
    }

    private void MoveToTheSafestLaneSpot(SimCharacter bot, Vector3 to, float at) => ai.MoveNow(bot, to, UcobExaflareDodge.DodgeSpeed, at);

    private void HoldOnTheLane(SimCharacter bot, Vector3 to, float at) => ai.MoveNow(bot, to, UcobExaflareDodge.DodgeSpeed, at);
}
