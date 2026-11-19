using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
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
//   49.79 the lights tether those four; Hiemal Ray on them at 54.65, 56.58, 58.67, each leaving an
//         ice puddle 0.53s later; after the second the four walk in to 7y from the centre.
//   58.55-63.37 the lights fall; 74.6 Icecrusher; 77.9 the Ice Veil falls before Endless Ice Age.
public class FruIceVeilScenarioTests
{
    [Test]
    public void StandingInHiemalStormDies()
        => Negative<FruIceVeilScenario>(MainTank)
            .FreezeAt(44.9f)
            .ShouldKill(ActionId.HiemalStorm, MainTank);

    // The north light falls last, so its tank has dodged every storm before giving up on it. The healer
    // goes back behind its dark crystal, out of the tank's way in.
    [Test]
    public void AimingSinboundBlizzardAtACrystalOfLightWipes()
        => Negative<FruIceVeilScenario>(ShieldHealer)
            .Seed(Enumerable.Range(0, 1000).First(seed => new FruIceVeilState(new Rng(seed)).LightFallsAt(0) == FruIceVeilState.LastLightFallsAt))
            .TeleportAt(43.5f, to: new(3.85f, -8.65f))
            .TeleportAt(45f, to: new(7.99f, -7.99f))
            .ShouldKill(ActionId.EndlessIceAge, PerRole.All);

    // Every light still up leaves a puddle per ray: four, four, then three as the first light has fallen.
    [Test]
    public void EachHiemalRayLeavesAnIcePuddleUnderItsTarget([Values(1, 2, 3)] int seed)
    {
        int? puddles = null;
        var run = ScenarioRun.Execute(typeof(FruIceVeilScenario), 0, seed, new ScenarioRunOptions
        {
            Probe = p =>
            {
                if (p.Crossed(59.5f))
                    puddles = p.World.Children.OfType<SimEventObject>().Count(e => e.IsActive && e.EObjRowId == EObjId.HiemalRayPuddle);
            },
        });
        Assert.That(run.Passed, run.ToString);
        Assert.That(puddles, Is.EqualTo(11));
    }

    [Test]
    public void SteppingBackIntoTheFirstRayPuddleBleedsOut()
        => Negative<FruIceVeilScenario>(MainTank)
            .TeleportAt(62f, to: new(-4f, -17f))
            .ShouldDieOfStatus(StatusId.Bleeding, MainTank);
}
