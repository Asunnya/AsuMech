using System.Linq;
using System.Numerics;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Uwu.PrimalRoulette;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Uwu.UwuConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// Order indices follow PrimalRouletteState.Orders; the first primal starts at 51.37s.
public class PrimalRouletteScenarioTests
{
    private const int IfritTitanGaruda = 0;
    private const int TitanIfritGaruda = 1;
    private const int GarudaIfritTitan = 2;
    private const float FirstRoulette = 51.37f;

    [Test]
    public void StayingOnTwoDiesToIfritsDash()
        => Negative<PrimalRouletteScenario>(MeleeDpsA)
            .Overrides<PrimalRouletteStateOverrides>(o => o.Order = IfritTitanGaruda)
            .TeleportAt(FirstRoulette + 6.5f, to: PrimalRouletteState.North)
            .ShouldKill(ActionId.CrimsonCyclone, MeleeDpsA);

    [Test]
    public void StayingInTheStackDiesToTheFirstWeights()
        => Negative<PrimalRouletteScenario>(RegenHealer)
            .Overrides<PrimalRouletteStateOverrides>(o => o.Order = TitanIfritGaruda)
            .TeleportAt(FirstRoulette + 4.2f, to: PrimalRouletteState.North)
            .ShouldKill(ActionId.WeightOfTheLand, RegenHealer);

    [Test]
    public void StandingUnderGarudaDiesToWickedWheel()
        => Negative<PrimalRouletteScenario>(PhysRangedDps)
            .Overrides<PrimalRouletteStateOverrides>(o => o.Order = GarudaIfritTitan)
            .TeleportAt(FirstRoulette + 7f, to: new Vector2(0f, -3f))
            .ShouldKill(ActionId.WickedWheelAwaken, PhysRangedDps);

    [Test]
    public void StayingOnTheWheelEdgeDiesToWickedTornado()
        => Negative<PrimalRouletteScenario>(OffTank)
            .Overrides<PrimalRouletteStateOverrides>(o => o.Order = GarudaIfritTitan)
            .TeleportAt(FirstRoulette + 10.8f, to: new Vector2(0f, -10.5f))
            .ShouldKill(ActionId.WickedTornado, OffTank);

    [Test]
    public void NotLeavingAfterGarudaJumpsDiesToFeatherRain()
        => Negative<PrimalRouletteScenario>(CasterDps)
            .Overrides<PrimalRouletteStateOverrides>(o => o.Order = GarudaIfritTitan)
            .TeleportAt(FirstRoulette + 22.62f, to: new Vector2(0f, -3.5f))
            .ShouldKill(ActionId.FeatherRain, CasterDps);

    [Test]
    public void TheBotsBurstOneOrbOfEveryPairBeforeTheyFuse()
    {
        var orbsLeft = -1;
        var run = ScenarioRun.Execute(typeof(PrimalRouletteScenario), 0, 11, new ScenarioRunOptions
        {
            WriteArtifactsOnFailure = false,
            Probe = probe =>
            {
                if (probe.Crossed(36.6f))
                    orbsLeft = probe.World.Children.OfType<SimEnemy>().Count(e => e.IsActive && e.SpawnConfig.BNpcBaseId == BNpcBaseId.Ultimaplasm);
            },
        });
        Assert.That(run.Deaths, Is.Empty);
        Assert.That(orbsLeft, Is.EqualTo(0));
    }
}
