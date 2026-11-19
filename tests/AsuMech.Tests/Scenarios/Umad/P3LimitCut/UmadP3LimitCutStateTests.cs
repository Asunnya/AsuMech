using AsuMech.Scenarios;
using AsuMech.Scenarios.Umad.P3LimitCut;

namespace AsuMech.Tests;

public class UmadP3LimitCutStateTests
{
    [Test]
    public void HeadwindsLeanToThreeOverFiveAndNeverDropBelowThree()
    {
        var counts = Enumerable.Range(0, 2000)
            .Select(seed => UmadP3LimitCutState.RollHeadwindCount(new Rng(seed)))
            .GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());
        Assert.That(counts.Keys, Is.SubsetOf(new[] { 3, 4, 5, 6 }));
        Assert.That(counts[4], Is.GreaterThan(counts[3]));
        Assert.That(counts[3], Is.GreaterThan(counts[5]));
        Assert.That(counts[5], Is.GreaterThan(counts[6]));
    }
}
