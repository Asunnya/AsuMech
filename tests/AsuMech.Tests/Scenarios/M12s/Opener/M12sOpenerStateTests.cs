using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.Opener;

namespace AsuMech.Tests;

public class M12sOpenerStateTests
{
    [Test]
    public void FourOrbsFallOnEachSideWithBothPurplesOnOneSide()
    {
        for (var seed = 0; seed < 500; seed++)
        {
            var state = new M12sOpenerState(new Rng(seed));
            var orbs = state.Orbs.SelectMany(pair => pair).ToList();
            Assert.That(state.Orbs, Has.Count.EqualTo(M12sOpenerState.Pairs).And.All.Count.EqualTo(2));
            Assert.That(orbs.Count(o => o.East), Is.EqualTo(4));
            Assert.That(orbs.Where(o => o.Purple).Select(o => o.East), Is.EqualTo(new[] { state.PurpleEast, state.PurpleEast }));
        }
    }

    [Test]
    public void APairNeverDropsAPurpleAndAGreenOnTheSameSide()
    {
        for (var seed = 0; seed < 500; seed++)
            foreach (var pair in new M12sOpenerState(new Rng(seed)).Orbs)
                if (pair[0].East == pair[1].East)
                    Assert.That(pair[0].Purple, Is.EqualTo(pair[1].Purple));
    }

    [Test]
    public void ThePurpleSideOverrideHolds()
    {
        foreach (var east in new[] { true, false })
            for (var seed = 0; seed < 50; seed++)
            {
                var state = new M12sOpenerState(new Rng(seed), new M12sOpenerStateOverrides { PurpleEast = east });
                Assert.That(state.PurpleEast, Is.EqualTo(east));
                Assert.That(state.Orbs.SelectMany(p => p).Where(o => o.Purple).All(o => o.East == east));
            }
    }
}
