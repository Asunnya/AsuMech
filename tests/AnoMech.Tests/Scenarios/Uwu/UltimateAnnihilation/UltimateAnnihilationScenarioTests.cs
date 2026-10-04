using System.Numerics;
using AnoMech.Scenarios.Uwu.UltimateAnnihilation;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.Uwu.UwuConstants;
using static AnoMech.Tests.NegativeRun;

namespace AnoMech.Tests;

// With Ultima above 50%, another Eye + Radiant Plume round runs before Suppression.
public class UltimateAnnihilationScenarioTests
{
    [Test]
    public void LeavingTheEyeBeforeItResolvesDies()
        => Negative<UltimateAnnihilationScenario>(MeleeDpsA)
            .TeleportAt(65f, to: new Vector2(-10.5f, -10.5f))
            .ShouldKill(ActionId.EyeOfTheStorm, MeleeDpsA);

    [Test]
    public void StayingInTheStackDiesToARadiantPlume()
        => Negative<UltimateAnnihilationScenario>(CasterDps)
            .TeleportAt(66.6f, to: new Vector2(-6.5f, -6f))
            .ShouldKill(ActionId.RadiantPlumePuddle, CasterDps);

    [Test]
    public void StandingBehindTheMainTankDiesToDiffractiveLaser()
        => Negative<UltimateAnnihilationScenario>(RegenHealer)
            .TeleportAt(70.5f, to: new Vector2(0.5f, -10f))
            .ShouldKill(ActionId.DiffractiveLaser, RegenHealer);

    [TestCase(true, 60f, 70f)]
    [TestCase(false, 80f, 90f)]
    public void SuppressionComesSoonerWhenUltimaIsPushedBelowHalf(bool pushed, float endsAfter, float endsBefore)
    {
        var run = ScenarioRun.Execute(typeof(UltimateAnnihilationScenario), 0, 99, new ScenarioRunOptions
        {
            WriteArtifactsOnFailure = false,
            Overrides = o => ((UltimateAnnihilationStateOverrides)o).UltimaPushedBelowHalf = pushed,
        });
        Assert.That(run.Deaths, Is.Empty);
        Assert.That(run.Elapsed, Is.InRange(endsAfter, endsBefore));
    }
}
