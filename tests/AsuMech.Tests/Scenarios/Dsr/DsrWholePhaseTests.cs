using AsuMech.Scenarios.Dsr.P5Death;
using AsuMech.Scenarios.Dsr.P6DoubleDragons;
using NUnit.Framework;
using static AsuMech.Scenarios.Dsr.DsrConstants;
using static AsuMech.Tests.WholePhaseTrace;

namespace AsuMech.Tests;

// P5 and P6 played whole: the gap from the last mechanic of one part to the first cast of the next
// is the fight's.
public class DsrWholePhaseTests
{
    private const float Tolerance = 1f;

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void DeathOfTheHeavensFollowsWrathAsInTheFight(int seed)
    {
        var trace = Execute(typeof(DsrP5DeathScenario), seed);
        Assert.That(trace.Run.Passed, trace.Run.ToString());
        var lastMight = trace.Last("Wrath of the Heavens", Ability(ActionId.AscalonsMight));
        Assert.That(trace.First("Death of the Heavens", Cast(ActionId.DeathOfTheHeavens)) - lastMight, Is.EqualTo(7.08f).Within(Tolerance));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void TheSecondAkhAfahFollowsTheSecondVowPassAsInTheFight(int seed)
    {
        var trace = Execute(typeof(DsrP6CauterizeScenario), seed);
        Assert.That(trace.Run.Passed, trace.Run.ToString());
        var vowPass = trace.Last("Wyrmsbreath to Wroth Flames", Ability(ActionId.MortalVowPass));
        Assert.That(trace.First("Hallowed Wings 2 to Cauterize", Cast(ActionId.AkhAfahNidhogg)) - vowPass, Is.EqualTo(4.28f).Within(Tolerance));
    }

    [Test]
    public void P6MusicCarriesOnIntoTheSecondHalf()
    {
        var trace = Execute(typeof(DsrP6CauterizeScenario), 0);
        var starts = trace.Rows.Where(r => r.Kind == "bgm").ToList();
        Assert.That(starts, Has.Count.EqualTo(2));
        Assert.That(UwuUltimaWholePhaseTests.SecondsIn(starts[1]) - UwuUltimaWholePhaseTests.SecondsIn(starts[0]),
            Is.EqualTo(starts[1].At - starts[0].At).Within(0.5f));
    }
}
