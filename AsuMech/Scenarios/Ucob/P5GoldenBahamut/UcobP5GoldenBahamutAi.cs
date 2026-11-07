using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using static AsuMech.Core.Game.Party.PartyRole;
using Scenario = AsuMech.Scenarios.Ucob.P5GoldenBahamut.UcobP5GoldenBahamutScenario;
using State = AsuMech.Scenarios.Ucob.P5GoldenBahamut.UcobP5GoldenBahamutState;

namespace AsuMech.Scenarios.Ucob.P5GoldenBahamut;

public sealed class UcobP5GoldenBahamutAi : IScenarioAi<State>
{
    public string Name => "Center stacks, uptime exaflare dodge";

    public static readonly Vector2 CenterOfTheBoss = Vector2.Zero;
    public static readonly Vector2 AkhMornTankSpotWhereHeFaces = new(0f, -3f);
    public static readonly Vector2 PartyBehindTheBoss = new(0f, 4f);
    private const float AfterTheHit = 0.2f;
    private const float AfterTheLastEruption = 0.1f;
    private const float InvulnBeforeTheFirstHit = 1f;

    private State state = null!;
    private SimWorld world = null!;
    private AiManager ai = null!;

    public void Run(State stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        ai = new AiManager(world);

        ai.Move(state.StartsAt + 1f, () => AiMove.All(CenterOfTheBoss));
        for (var k = 0; k < Scenario.AkhMornCasts.Length; k++)
        {
            var akhMorn = k;
            ai.Move(TimeToLeaveForAkhMorn(k), () => AiMove.Create(AkhMornSpots(akhMorn)).NaturalOrder(), sprint: true);
            if (State.IsInvulnAkhMorn(k))
                world.Events.Add(Scenario.AkhMornCasts[k] + Scenario.AkhMornResolve - InvulnBeforeTheFirstHit, () => InvulnTheLoneTank(akhMorn));
        }
        for (var i = 0; i < Scenario.MornAfahCasts.Length; i++)
            ai.Move(TimeToLeaveForMornAfah(i), () => AiMove.All(CenterOfTheBoss), sprint: true);

        foreach (var exaflares in state.Exaflares) new UcobExaflareDodge(exaflares, world, MoveToTheSafestLaneSpot, HoldOnTheLane).Schedule();
    }

    private float TimeToLeaveForAkhMorn(int akhMorn)
    {
        var mornAfahJustBefore = Scenario.MornAfahCasts.LastOrDefault(c => c < Scenario.AkhMornCasts[akhMorn]);
        var exaflaresJustBefore = state.Exaflares.LastOrDefault(e => e.BossCastAt < Scenario.AkhMornCasts[akhMorn]);
        return MathF.Max(mornAfahJustBefore + Scenario.MornAfahResolve + AfterTheHit, (exaflaresJustBefore?.LastHitAt ?? 0f) + AfterTheLastEruption);
    }

    private float TimeToLeaveForMornAfah(int mornAfah)
    {
        var cast = Scenario.MornAfahCasts[mornAfah];
        var akhMornJustBefore = Enumerable.Range(0, Scenario.AkhMornCasts.Length).Where(k => Scenario.AkhMornCasts[k] < cast).Select(Scenario.AkhMornLastHit).DefaultIfEmpty(0f).Max();
        var exaflaresJustBefore = state.Exaflares.Where(e => e.BossCastAt < cast).Select(e => e.LastHitAt).DefaultIfEmpty(0f).Max();
        return MathF.Max(akhMornJustBefore + AfterTheHit, exaflaresJustBefore + AfterTheLastEruption);
    }

    private Vector2?[] AkhMornSpots(int akhMorn)
    {
        var spots = Enumerable.Repeat<Vector2?>(PartyBehindTheBoss, 8).ToArray();
        var holder = state.AkhMornTargets[akhMorn];
        spots[(int)holder] = AkhMornTankSpotWhereHeFaces;
        if (!State.IsInvulnAkhMorn(akhMorn)) spots[(int)State.OtherTank(holder)] = AkhMornTankSpotWhereHeFaces;
        return spots;
    }

    private void InvulnTheLoneTank(int akhMorn)
    {
        if (world.Party.Get(state.AkhMornTargets[akhMorn]) is not { } tank || !tank.IsAlive() || !world.Party.IsBotDriven(tank)) return;
        var lastHit = Scenario.AkhMornLastHit(akhMorn);
        DamageSolver.GiveBotInvuln(tank, lastHit - world.Events.Elapsed + 1f);
    }

    private void MoveToTheSafestLaneSpot(SimCharacter bot, Vector3 to, float at) => ai.MoveNow(bot, to, UcobExaflareDodge.DodgeSpeed, at);

    private void HoldOnTheLane(SimCharacter bot, Vector3 to, float at) => ai.MoveNow(bot, to, UcobExaflareDodge.DodgeSpeed, at);
}
