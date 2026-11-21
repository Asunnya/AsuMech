using AsuMech.Scenarios.Dsr.P5Death;
using NUnit.Framework;
using static AsuMech.Scenarios.Dsr.DsrConstants;
using static AsuMech.Tests.WholePhaseTrace;

namespace AsuMech.Tests;

// P5 played whole: the gap from the last mechanic of one part to the first cast of the next
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
}
