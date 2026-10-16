using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P5Wrath;

namespace AnoMech.Tests;

public class DsrP5WrathStateTests
{
    private static IEnumerable<DsrP5WrathState> Rolls(int count = 300)
        => Enumerable.Range(0, count).Select(seed => new DsrP5WrathState(new Rng(seed)));

    [Test]
    public void TethersDefamationAndDivebombGoToFourDifferentPlayers()
    {
        foreach (var state in Rolls())
        {
            var marked = state.Tethered.Append(state.Defamation).Append(state.Divebomb).ToList();
            Assert.That(marked.Distinct().Count(), Is.EqualTo(4));
        }
    }

    [Test]
    public void LiquidHeavenTakesAPlainPlayerAndEruptionsAMarkedOne()
    {
        foreach (var state in Rolls())
        {
            Assert.That(state.Plain, Does.Contain(state.LiquidHeaven));
            Assert.That(state.Marked, Does.Contain(state.Eruptions));
        }
    }

    [Test]
    public void TheDragonSitsOnACardinalAndTheCastersOnItsAxis()
    {
        foreach (var state in Rolls())
        {
            Assert.That(new[] { 0f, 90f, 180f, 270f }, Does.Contain(state.DragonBearing));
            Assert.That((state.WarriorBearing - state.DragonBearing + 360f) % 180f, Is.EqualTo(0f));
        }
    }

    [Test]
    public void LightningHitsTwoDifferentPlayers()
    {
        foreach (var state in Rolls())
            Assert.That(state.Lightning.Distinct().Count(), Is.EqualTo(2));
    }
}
