using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P1Knights;
using AnoMech.Scenarios.Dsr.P5Death;

namespace AnoMech.Tests;

public class DsrP5DeathStateTests
{
    private static IEnumerable<DsrP5DeathState> Rolls(int count = 300)
        => Enumerable.Range(0, count).Select(seed => new DsrP5DeathState(new Rng(seed)));

    [Test]
    public void DeathstormDoomsFourPlayers()
    {
        foreach (var state in Rolls())
            Assert.That(state.Dooms.Distinct().Count(), Is.EqualTo(4));
    }

    [Test]
    public void TheWarriorSitsOnACardinal()
    {
        foreach (var state in Rolls())
            Assert.That(new[] { 0f, 90f, 180f, 270f }, Does.Contain(state.WarriorBearing));
    }

    // The far dooms get circles, the other dooms a square and a triangle, two non-dooms the crosses.
    [Test]
    public void SymbolsFollowWhoStandsFarthest()
    {
        foreach (var state in Rolls())
        {
            var dooms = state.Dooms.OrderBy(r => r).ToList();
            var distances = Enum.GetValues<PartyRole>().ToDictionary(r => r, r => r == dooms[0] || r == dooms[3] ? 5f : 1f);
            state.HandOutSymbols(distances);
            Assert.That(state.Symbols[dooms[0]], Is.EqualTo(ChainSymbol.Circle));
            Assert.That(state.Symbols[dooms[3]], Is.EqualTo(ChainSymbol.Circle));
            Assert.That(new[] { state.Symbols[dooms[1]], state.Symbols[dooms[2]] }, Is.EquivalentTo(new[] { ChainSymbol.Square, ChainSymbol.Triangle }));
            var nonDooms = Enum.GetValues<PartyRole>().Where(r => !state.IsDoomed(r)).Select(r => state.Symbols[r]).ToList();
            Assert.That(nonDooms, Is.EquivalentTo(new[] { ChainSymbol.Cross, ChainSymbol.Cross, ChainSymbol.Square, ChainSymbol.Triangle }));
            Assert.That(state.ChainPairs().Count(), Is.EqualTo(4));
        }
    }
}
