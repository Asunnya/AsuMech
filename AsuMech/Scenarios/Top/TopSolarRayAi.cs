using System.Linq;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Top.TopConstants;

namespace AsuMech.Scenarios.Top;

public static class TopSolarRayAi
{
    public static void TankTheSolarRaysAwayFromTheParty(AiManager ai, SimWorld world, float castAt)
    {
        var cast = castAt - world.Events.Elapsed;
        ai.Move(cast + 0.3f, SolarRaySpots);
        world.Events.Add(cast + TopSolarRay.FirstHitAfterCast + 1f, () => OffTankProvokesOffTheVuln(world));
    }

    private static IAiMove SolarRaySpots() => AiMove.Create(
        new(-5f, -6f), new(5f, -6f),
        new(-1.2f, 9f), new(1.2f, 9f),
        new(-1.2f, 11f), new(1.2f, 11f),
        new(-0.4f, 10f), new(0.4f, 12f));

    private static void OffTankProvokesOffTheVuln(SimWorld world)
    {
        if (world.Party.Get(PartyRole.OffTank) is not { } tank || !tank.IsAlive() || !world.Party.IsBotDriven(tank)) return;
        world.Children.OfType<SimEnemy>().FirstOrDefault(e => e.IsActive && e.SpawnConfig.BNpcBaseId == BNpcBaseId.OmegaMDynamis)?.Provoke(tank);
    }
}
