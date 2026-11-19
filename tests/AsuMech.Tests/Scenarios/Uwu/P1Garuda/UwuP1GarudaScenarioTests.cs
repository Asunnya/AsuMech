using System.Linq;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Uwu.P1Garuda;
using static AsuMech.Scenarios.Uwu.UwuConstants;

namespace AsuMech.Tests;

public class UwuP1GarudaScenarioTests
{
    [Test]
    public void TheFirstPlumesAreUpTwoSecondsBeforeTheSecondSlipstream()
    {
        var plumes = 0;
        ScenarioRun.Execute(typeof(UwuP1GarudaScenario), 0, 1234, new ScenarioRunOptions
        {
            StopAt = 19f,
            WriteArtifactsOnFailure = false,
            Probe = probe =>
            {
                if (!probe.Crossed(18.7f)) return;
                plumes = probe.World.Children.OfType<SimEnemy>()
                    .Count(e => e.SpawnConfig.BNpcBaseId is BNpcBaseId.SatinPlume or BNpcBaseId.SpinyPlume);
            },
        });

        Assert.That(plumes, Is.EqualTo(5));
    }
}
