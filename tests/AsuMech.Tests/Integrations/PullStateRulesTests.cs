using AsuMech.Integrations;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Dsr;
using AsuMech.Scenarios.Fru;
using AsuMech.Scenarios.Top;
using AsuMech.Scenarios.Umad;
using NUnit.Framework;

namespace AsuMech.Tests;

public class PullStateRulesTests
{
    private readonly object runA = new(), runB = new();

    [Test]
    public void NothingIsHeldOutsideAZoneOrWhenOff()
    {
        var rules = new PullStateRules();
        Assert.That(rules.Tick(enabled: true, inZone: false, runA, running: true), Is.Null);
        Assert.That(rules.Tick(enabled: false, inZone: true, runA, running: true), Is.Null);
    }

    [Test]
    public void InTheZoneTheDutyIsHeldAndCombatFollowsTheRun()
    {
        var rules = new PullStateRules();
        Assert.That(rules.Tick(true, true, null, false), Is.EqualTo(new PullStateRules.Frame(true, false)));
        Assert.That(rules.Tick(true, true, runA, true), Is.EqualTo(new PullStateRules.Frame(true, true)));
        Assert.That(rules.Tick(true, true, runA, false), Is.EqualTo(new PullStateRules.Frame(true, false)), "a finished run ends combat");
    }

    [Test]
    public void ARestartReadsOutOfCombatForTheGapThenEngagesAgain()
    {
        var rules = new PullStateRules();
        Assert.That(rules.Tick(true, true, runA, true)!.Value.InCombat, Is.True);
        for (var i = 0; i < PullStateRules.GapFrames; i++)
            Assert.That(rules.Tick(true, true, runB, true)!.Value.InCombat, Is.False, $"gap frame {i}");
        Assert.That(rules.Tick(true, true, runB, true)!.Value.InCombat, Is.True);
    }

    [Test]
    public void ANewRunAfterAFinishedOneStillGetsTheGap()
    {
        var rules = new PullStateRules();
        rules.Tick(true, true, runA, true);
        rules.Tick(true, true, runA, false);
        Assert.That(rules.Tick(true, true, runB, true)!.Value.InCombat, Is.False);
    }

    [Test]
    public void LeavingForgetsTheLastRun()
    {
        var rules = new PullStateRules();
        rules.Tick(true, true, runA, true);
        rules.Tick(true, false, null, false);
        Assert.That(rules.Tick(true, true, runB, true)!.Value.InCombat, Is.True, "the first run of a stay has no gap");
    }

    // The scenes Splatoon's own scripts check for these phases.
    [Test]
    public void PhasesCarryTheScenesSplatoonScriptsCheck()
    {
        Assert.That(TopZone.P1.EnvScene, Is.EqualTo((byte)2));
        Assert.That(TopZone.P5.EnvScene, Is.EqualTo((byte)6));
        Assert.That(TopZone.P6.EnvScene, Is.EqualTo((byte)7));
        Assert.That(DsrZone.Thordan.EnvScene, Is.EqualTo((byte)4));
        Assert.That(DsrZone.Nidhogg.EnvScene, Is.EqualTo((byte)6));
        Assert.That(DsrZone.DarkThordan.EnvScene, Is.EqualTo((byte)5));
        Assert.That(FruZone.P1.EnvScene, Is.EqualTo((byte)1));
        Assert.That(UmadZone.P1.EnvScene, Is.EqualTo((byte)4));
        Assert.That(UmadZone.P2.EnvScene, Is.EqualTo((byte)7));
        Assert.That(UmadZone.P3.EnvScene, Is.EqualTo((byte)8));
        Assert.That(((IPhase)DsrZone.Knights).EnvScene, Is.Null);
    }
}
