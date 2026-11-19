using System.Numerics;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.Replication1;

namespace AsuMech.Tests;

public class M12sReplication1StateTests
{
    private static M12sReplication1State Rolled(int seed) => new(new Rng(seed));

    [Test]
    public void TheDarkCloneFacesTheFireCloneAcrossTheArenaAndTheCleavesTakeTheOtherAxis()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var state = Rolled(seed);
            Assert.That(Vector3.Distance(state.FireClone, -state.DarkClone), Is.LessThan(0.01f));
            foreach (var cleave in state.CleaveClones)
            {
                Assert.That(Vector3.Dot(cleave, state.FireClone), Is.EqualTo(0f).Within(0.01f));
                Assert.That(cleave.Length(), Is.EqualTo(M12sReplication1State.CloneRing).Within(0.01f));
            }
        }
    }

    [Test]
    public void TheSecondLineAlternatesFireAndDarkAlongOneDiagonal()
    {
        for (var outer = 0; outer < 4; outer++)
        {
            var state = new M12sReplication1State(new Rng(0), new M12sReplication1StateOverrides { OuterFireIntercard = outer });
            var diagonal = Vector3.Normalize(state.OuterFire);
            Vector3[] line = [state.OuterFire, state.InnerDark, state.InnerFire, state.OuterDark];
            var along = line.Select(p => Vector3.Dot(p, diagonal)).ToArray();
            Assert.That(along, Is.Ordered.Descending);
            foreach (var p in line)
                Assert.That(Vector3.Distance(p, Vector3.Dot(p, diagonal) * diagonal), Is.LessThan(0.01f));
            foreach (var cleave in state.SecondCleaveClones)
                Assert.That(Vector3.Dot(cleave, diagonal), Is.EqualTo(0f).Within(0.01f));
        }
    }

    [Test]
    public void OverridesPinEveryRoll()
    {
        var overrides = new M12sReplication1StateOverrides
        {
            FireCardinal = 3, VerticalCleaves = false, OuterFireIntercard = 2, SnakingKickEast = true, SobatTurns = 1,
        };
        for (var seed = 0; seed < 20; seed++)
        {
            var state = new M12sReplication1State(new Rng(seed), overrides);
            Assert.That(state.FireCardinal, Is.EqualTo(3));
            Assert.That(state.VerticalCleaves, Is.False);
            Assert.That(state.CleaveHeadings, Is.EquivalentTo(new[] { MathF.PI / 2f, -MathF.PI / 2f }));
            Assert.That(state.OuterFireIntercard, Is.EqualTo(2));
            Assert.That(state.SnakingKickHeading, Is.EqualTo(M12sReplication1State.SnakingKickTurn));
            Assert.That(state.SobatTurns, Is.EqualTo(1));
        }
    }

    [Test]
    public void EveryRollComesUp()
    {
        var states = Enumerable.Range(0, 200).Select(Rolled).ToList();
        Assert.That(states.Select(s => s.FireCardinal).Distinct().Count(), Is.EqualTo(4));
        Assert.That(states.Select(s => s.OuterFireIntercard).Distinct().Count(), Is.EqualTo(4));
        Assert.That(states.Select(s => s.SobatTurns).Distinct().Count(), Is.EqualTo(4));
        Assert.That(states.Select(s => s.VerticalCleaves).Distinct().Count(), Is.EqualTo(2));
        Assert.That(states.Select(s => s.SnakingKickHeading).Distinct().Count(), Is.EqualTo(2));
    }
}
