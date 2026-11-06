using System.Linq;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Uwu.P2Ifrit;
using static AsuMech.Scenarios.Uwu.UwuConstants;

namespace AsuMech.Tests;

public class UwuP2IfritScenarioTests
{
    [Test]
    public void RadiantPlumesShowTheirOmensWhileTheirCastersAreStillArriving()
    {
        var plumesCasting = 0;
        ScenarioRun.Execute(typeof(UwuP2IfritScenario), 0, 1234, new ScenarioRunOptions
        {
            PacketSpawnFrames = (2, 5),
            StopAt = 7f,
            WriteArtifactsOnFailure = false,
            Probe = probe =>
            {
                if (!probe.Crossed(6f)) return;
                plumesCasting = probe.World.Children.OfType<SimEnemy>()
                    .Count(e => e.IsCasting && e.CastActionId == ActionId.RadiantPlumePuddle);
            },
        });

        Assert.That(plumesCasting, Is.GreaterThan(0));
    }
}
