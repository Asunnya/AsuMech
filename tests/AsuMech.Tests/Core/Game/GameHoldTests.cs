using AsuMech.Scenarios.Uwu.P1Garuda;

namespace AsuMech.Tests.Core.Game;

public class GameHoldTests
{
    [Test]
    public void AHeldRunStopsItsClockAndGoesOnWhenReleased()
    {
        var frames = 0;
        float? heldAt = null;
        var heldFrames = 0;
        var run = ScenarioRun.Execute(typeof(UwuP1GarudaScenario), 0, 1, new ScenarioRunOptions
        {
            WriteArtifactsOnFailure = false,
            Probe = p =>
            {
                frames++;
                var elapsed = p.World.Events.Elapsed;
                if (frames == 200) { p.Game.Held = true; heldAt = elapsed; }
                else if (p.Game.Held && ++heldFrames == 100)
                {
                    Assert.That(elapsed, Is.EqualTo(heldAt), "nothing advances while held");
                    p.Game.Held = false;
                }
            },
        });
        Assert.That(heldFrames, Is.EqualTo(100));
        Assert.That(run.Passed, run.Failure);
    }
}
