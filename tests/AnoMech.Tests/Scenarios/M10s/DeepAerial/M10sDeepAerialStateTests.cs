using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M10s.DeepAerial;

namespace AnoMech.Tests;

public class M10sDeepAerialStateTests
{
    [Test]
    public void EachHalfGivesEveryNonHealerOneTether()
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var state = new M10sDeepAerialState(new Rng(seed));
            Assert.That(state.Tethers, Has.Count.EqualTo(6));
            foreach (var half in new[] { state.Tethers.Take(3), state.Tethers.Skip(3) })
                Assert.That(half.SelectMany(t => new[] { t.Fire, t.Water }), Is.EquivalentTo(M10sDeepAerialState.Tetherable));
        }
    }

    [Test]
    public void ThePlayerGetsTheTethersTheyPicked()
    {
        foreach (var player in M10sDeepAerialState.Tetherable)
        for (var slot = 0; slot < 6; slot++)
        for (var seed = 0; seed < 10; seed++)
        {
            var state = new M10sDeepAerialState(new Rng(seed), new M10sDeepAerialStateOverrides { PlayerFirstTether = slot, PlayerSecondTether = 5 - slot }, player);
            var first = state.Tethers[slot / 2];
            Assert.That(slot % 2 == 0 ? first.Fire : first.Water, Is.EqualTo(player));
            var second = state.Tethers[3 + (5 - slot) / 2];
            Assert.That((5 - slot) % 2 == 0 ? second.Fire : second.Water, Is.EqualTo(player));
        }
    }
}
