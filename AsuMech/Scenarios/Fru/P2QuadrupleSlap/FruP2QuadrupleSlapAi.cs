using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Scenarios.Fru.P2QuadrupleSlap.FruP2QuadrupleSlapState;

namespace AsuMech.Scenarios.Fru.P2QuadrupleSlap;

public sealed class FruP2QuadrupleSlapAi : IScenarioAi<FruP2QuadrupleSlapState>
{
    public string Name => "NA · MT invuln";
    public string Group => "NA";

    private SimWorld world = null!;

    public void Run(FruP2QuadrupleSlapState state, SimWorld worldParam)
    {
        world = worldParam;
        var ai = new AiManager(world);
        ai.Move(0.5f, () => ByRole(AroundTheUsurper), arrivalTime: TargetableAt);
        world.Events.Add(FirstSlapAt + 2.9f, MainTankInvulnsBothSlaps);
        world.Events.Add(SecondSlapHits + 0.7f, OffTankProvokesOffTheVuln);
    }

    private void MainTankInvulnsBothSlaps()
    {
        if (world.Party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive() && world.Party.IsBotDriven(tank))
            DamageSolver.GiveBotInvuln(tank, 10f);
    }

    private void OffTankProvokesOffTheVuln()
    {
        if (world.Party.Get(PartyRole.OffTank) is not { } tank || !tank.IsAlive() || !world.Party.IsBotDriven(tank)) return;
        world.Children.OfType<SimEnemy>().FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.UsurperP2)?.Provoke(tank);
    }

    private static Vector3? AroundTheUsurper(PartyRole role) => role switch
    {
        PartyRole.MainTank => new Vector3(0, 0, -4),
        PartyRole.OffTank => new Vector3(2.5f, 0, -3.5f),
        PartyRole.MeleeDpsA => new Vector3(-3, 0, 1),
        PartyRole.MeleeDpsB => new Vector3(3, 0, 1),
        PartyRole.RegenHealer => new Vector3(-3, 0, 8),
        PartyRole.ShieldHealer => new Vector3(3, 0, 8),
        PartyRole.PhysRangedDps => new Vector3(-6, 0, 7),
        _ => new Vector3(6, 0, 7),
    };

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}
