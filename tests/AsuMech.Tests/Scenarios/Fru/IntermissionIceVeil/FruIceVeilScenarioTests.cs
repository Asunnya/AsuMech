using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Fru.IntermissionIceVeil;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

//   14.91 Swelling Frost pushes everyone 10y out of the centre and freezes them.
//   43.93 Sinbound Blizzard from each Crystal of Darkness at its nearest: the healers and ranged
//         3.3y behind them on the intercards.
//   44.82 Hiemal Storm from each Crystal of Light at its nearest: MT N, M1 E, OT S, M2 W at 17y,
//         4y aside, swapping sides each wave.
//   49.79 the lights tether those four; Hiemal Ray on them at 54.65, 56.58, 58.67.
//   58.55-63.37 the lights fall; 74.6 Icecrusher; 77.9 the Ice Veil falls before Endless Ice Age.
public class FruIceVeilScenarioTests
{
    [Test]
    public void StandingInHiemalStormDies()
        => Negative<FruIceVeilScenario>(MainTank)
            .FreezeAt(44.9f)
            .ShouldKill(ActionId.HiemalStorm, MainTank);

    // The north light falls last, so its tank has dodged every storm before giving up on it.
    [Test]
    public void AimingSinboundBlizzardAtACrystalOfLightWipes()
        => Negative<FruIceVeilScenario>(ShieldHealer)
            .Seed(Enumerable.Range(0, 1000).First(seed => new FruIceVeilState(new Rng(seed)).LightFallsAt(0) == FruIceVeilState.LastLightFallsAt))
            .TeleportAt(43.5f, to: new(3.85f, -8.65f))
            .ShouldKill(ActionId.EndlessIceAge, PerRole.All);
}
