using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P3Nidhogg;
using static AnoMech.Core.Game.Party.PartyRole;

namespace AnoMech.Tests;

public class DsrP3NidhoggStateTests
{
    private static IEnumerable<DsrP3NidhoggState> Rolls(int count = 300)
        => Enumerable.Range(0, count).Select(seed => new DsrP3NidhoggState(new Rng(seed)));

    [Test]
    public void DiveFromGraceNumbersAreThreeTwoThree()
    {
        foreach (var state in Rolls())
        {
            Assert.That(state.Lines.Keys, Is.EquivalentTo(Enum.GetValues<PartyRole>()));
            Assert.That(new[] { 1, 2, 3 }.Select(line => state.Line(line).Count()), Is.EqualTo(new[] { 3, 2, 3 }));
        }
    }

    [Test]
    public void ThePlayerGetsTheDebuffAndArrowTheyPicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        foreach (var line in new int?[] { null, 1, 2, 3 })
        foreach (var arrow in new DiveArrow?[] { null, DiveArrow.Circle, DiveArrow.Up, DiveArrow.Down })
        for (var seed = 0; seed < 40; seed++)
        {
            var state = new DsrP3NidhoggState(new Rng(seed), new DsrP3NidhoggStateOverrides { PlayerLine = line, PlayerArrow = arrow }, player);
            if (line is { } l) Assert.That(state.Lines[player], Is.EqualTo(l));
            if (arrow is { } a) Assert.That(state.Arrows[player], Is.EqualTo(a));
            Assert.That(new[] { 1, 2, 3 }.Select(n => state.Line(n).Count()), Is.EqualTo(new[] { 3, 2, 3 }));
            var arrowed = new[] { 1, 2, 3 }.Where(state.LineHasArrows).ToList();
            Assert.That(arrowed.Count, Is.InRange(1, 2));
            foreach (var n in arrowed)
                Assert.That(state.Line(n).Select(r => state.Arrows[r]).Where(x => x != DiveArrow.Circle), Is.EquivalentTo(new[] { DiveArrow.Up, DiveArrow.Down }));
        }
    }

    [Test]
    public void OneOrTwoDivesCarryOneUpAndOneDownArrow()
    {
        foreach (var state in Rolls())
        {
            var arrowed = 0;
            foreach (var line in new[] { 1, 2, 3 })
            {
                var arrows = state.Line(line).Select(r => state.Arrows[r]).ToList();
                if (arrows.All(a => a == DiveArrow.Circle)) continue;
                arrowed++;
                Assert.That(arrows.Count(a => a == DiveArrow.Up), Is.EqualTo(1));
                Assert.That(arrows.Count(a => a == DiveArrow.Down), Is.EqualTo(1));
            }
            Assert.That(arrowed, Is.InRange(1, 2));
        }
    }

    [Test]
    public void DarkdragonDiveTowersAlwaysNeedTheWholeParty()
    {
        int[][] sets = [[1, 1, 3, 3], [1, 2, 2, 3], [1, 1, 2, 4], [2, 2, 2, 2]];
        foreach (var state in Rolls())
        {
            Assert.That(state.TowerSoakers.Sum(), Is.EqualTo(8));
            Assert.That(sets.Any(set => set.OrderBy(n => n).SequenceEqual(state.TowerSoakers.OrderBy(n => n))));
            Assert.That(state.QuietTower, Is.InRange(0, 3));
        }
    }

    [Test]
    public void SoulTethersStartOnTwoDifferentNonTanks()
    {
        foreach (var state in Rolls())
        {
            Assert.That(state.SoulTetherTargets.Distinct().Count(), Is.EqualTo(2));
            Assert.That(state.SoulTetherTargets, Has.None.EqualTo(MainTank).And.None.EqualTo(OffTank));
        }
    }

    [Test]
    public void EyeOfTheTyrantNeverTargetsThatRoundsDivers()
    {
        foreach (var state in Rolls())
        {
            Assert.That(state.Lines[state.EyeTargets[0]], Is.Not.EqualTo(1));
            Assert.That(state.Lines[state.EyeTargets[1]], Is.Not.EqualTo(3));
        }
    }

    [Test]
    public void ArrowTowersLandFourteenYalmsAheadOrBehind()
    {
        Assert.That(DsrP3NidhoggState.DiveOffset(DiveArrow.Up), Is.EqualTo(14f));
        Assert.That(DsrP3NidhoggState.DiveOffset(DiveArrow.Down), Is.EqualTo(-14f));
        Assert.That(DsrP3NidhoggState.DiveOffset(DiveArrow.Circle), Is.EqualTo(0f));
    }

    [Test]
    public void NidhoggPullsTheFirstSoulTetherAndTheQuietTowersDiverTheSecond()
    {
        foreach (var state in Rolls())
        {
            Assert.That(state.SoulTetherSource(0), Is.EqualTo(System.Numerics.Vector3.Zero));
            Assert.That(state.SoulTetherSource(1), Is.EqualTo(DsrP3NidhoggState.FourTowerSpot(state.QuietTower)));
        }
    }
}
