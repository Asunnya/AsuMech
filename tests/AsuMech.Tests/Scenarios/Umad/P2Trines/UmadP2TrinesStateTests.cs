using AsuMech.Scenarios;
using AsuMech.Scenarios.Umad.P2Trines;

namespace AsuMech.Tests;

public class UmadP2TrinesStateTests
{
    [Test]
    public void ThreeThenOneThenTheMiddleWithTheLastTwo()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new UmadP2TrinesState(new Rng(seed));
            Assert.That(state.FirstSet, Has.Count.EqualTo(3));
            Assert.That(state.ThirdSet, Has.Count.EqualTo(3));
            Assert.That(state.ThirdSet, Does.Contain(UmadP2TrinesState.Middle));
            var all = state.FirstSet.Append(state.SecondSet).Concat(state.ThirdSet).ToList();
            Assert.That(all.OrderBy(t => t), Is.EqualTo(Enumerable.Range(0, 7)));
        }
    }

    [Test]
    public void PinnedCleaveAndFirstSetHold()
    {
        foreach (var west in new[] { true, false })
            for (var seed = 0; seed < 50; seed++)
            {
                var state = new UmadP2TrinesState(new Rng(seed), new UmadP2TrinesStateOverrides { CleaveWest = west, FirstSet = [2, 4, 6] });
                Assert.That(state.CleaveWest, Is.EqualTo(west));
                Assert.That(state.FirstSet, Is.EquivalentTo(new[] { 2, 4, 6 }));
            }
    }
}
