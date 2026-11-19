using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Umad.P3BlackHole;

namespace AsuMech.Tests;

public class UmadP3BlackHoleStateTests
{
    [Test]
    public void KnockDownMarksOneSupportAndOneDpsInEitherOrder()
    {
        var firsts = new HashSet<bool>();
        for (var seed = 0; seed < 200; seed++)
        {
            var targets = UmadP3BlackHoleState.NextStackTargets(new Rng(seed));
            Assert.That(targets, Has.Length.EqualTo(2));
            Assert.That(targets[0].IsDps(), Is.Not.EqualTo(targets[1].IsDps()));
            firsts.Add(targets[0].IsDps());
        }
        Assert.That(firsts, Is.EquivalentTo(new[] { true, false }));
    }
}
