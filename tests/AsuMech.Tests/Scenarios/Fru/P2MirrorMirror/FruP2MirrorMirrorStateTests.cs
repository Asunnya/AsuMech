using AsuMech.Scenarios;
using AsuMech.Scenarios.Fru.P2MirrorMirror;

namespace AsuMech.Tests;

public class FruP2MirrorMirrorStateTests
{
    private static IEnumerable<FruP2MirrorMirrorState> Rolls(FruP2MirrorMirrorStateOverrides? overrides = null, int count = 300)
        => Enumerable.Range(0, count).Select(seed => new FruP2MirrorMirrorState(new Rng(seed), overrides ?? new()));

    [Test]
    public void TheRedsSitNinetyDegreesApartOffTheBlue()
    {
        foreach (var state in Rolls())
        {
            Assert.That((state.RedOctants[1] - state.RedOctants[0] + 8) % 8, Is.EqualTo(2));
            Assert.That(state.RedOctants, Does.Not.Contain(state.BlueOctant));
        }
    }

    [Test]
    public void EveryLayoutTurnsUp()
    {
        var layouts = Rolls(count: 2000).Select(s => (s.BlueOctant, s.FirstRedOctant)).Distinct().Count();
        Assert.That(layouts, Is.EqualTo(8 * 6));
    }

    [Test]
    public void ForcedRedsAreKeptUnlessTheyTakeTheBluesSpot()
    {
        for (var blue = 0; blue < 8; blue++)
            for (var red = 0; red < 8; red++)
                foreach (var state in Rolls(new() { BlueOctant = blue, FirstRedOctant = red }, count: 20))
                {
                    var clear = red != blue && (red + 2) % 8 != blue;
                    Assert.That(state.RedOctants, Does.Not.Contain(blue));
                    if (clear) Assert.That(state.FirstRedOctant, Is.EqualTo(red));
                }
    }

    [Test]
    public void TheUsurperKicksFromOppositeTheBlue()
    {
        foreach (var state in Rolls())
            Assert.That(System.Numerics.Vector3.Dot(state.BossSpot, state.Blue), Is.LessThan(-140f));
    }
}
