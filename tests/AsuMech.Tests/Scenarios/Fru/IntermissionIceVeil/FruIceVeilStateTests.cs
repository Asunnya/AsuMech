using AsuMech.Scenarios;
using AsuMech.Scenarios.Fru.IntermissionIceVeil;

namespace AsuMech.Tests;

public class FruIceVeilStateTests
{
    [Test]
    public void TheLightsFallOneAtATimeInAnyOrder()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var state = new FruIceVeilState(new Rng(seed));
            var times = Enumerable.Range(0, 4).Select(state.LightFallsAt).ToList();
            Assert.That(times.Distinct().Count(), Is.EqualTo(4));
            Assert.That(times.Max(), Is.EqualTo(FruIceVeilState.LastLightFallsAt));
            Assert.That(times.Min(), Is.GreaterThan(FruIceVeilState.HiemalRayAt[^1] - 0.2f));
        }
    }

    [Test]
    public void EveryLightFallsFirstSometimes()
    {
        var firsts = Enumerable.Range(0, 200).Select(seed => new FruIceVeilState(new Rng(seed)))
            .Select(state => Enumerable.Range(0, 4).MinBy(state.LightFallsAt)).Distinct().Count();
        Assert.That(firsts, Is.EqualTo(4));
    }
}
