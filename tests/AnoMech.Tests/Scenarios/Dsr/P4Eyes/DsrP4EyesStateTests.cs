using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P4Eyes;

namespace AnoMech.Tests;

public class DsrP4EyesStateTests
{
    private static IEnumerable<DsrP4EyesState> Rolls(int count = 300)
        => Enumerable.Range(0, count).Select(seed => new DsrP4EyesState(new Rng(seed)));

    [Test]
    public void HateboundPutsFourOnRedAndFourOnBlue()
    {
        foreach (var state in Rolls())
            Assert.That(state.FirstRed.Distinct().Count(), Is.EqualTo(4));
    }

    [Test]
    public void EachMirageDivePicksTwoDifferentRedHolders()
    {
        foreach (var state in Rolls())
        {
            Assert.That(state.DivePicks, Has.Count.EqualTo(DsrP4EyesState.Dives));
            foreach (var (first, second) in state.DivePicks)
            {
                Assert.That(first, Is.Not.EqualTo(second));
                Assert.That(new[] { first, second }, Is.All.InRange(0, 3));
            }
        }
    }

    [Test]
    public void AnyRoleCanBeMarkedForSoulOfFriendship()
    {
        var marked = Rolls().Select(s => s.SoulMarked).ToHashSet();
        Assert.That(marked, Is.EquivalentTo(Enum.GetValues<PartyRole>()));
    }

    [Test]
    public void OverridesPinTheRolls()
    {
        var overrides = new DsrP4EyesStateOverrides
        {
            SoulMarked = PartyRole.CasterDps,
            FirstRed = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB],
            DivePicks = [(0, 1), (2, 3), (0, 3), (1, 2)],
        };
        var state = new DsrP4EyesState(new Rng(7), overrides);
        Assert.That(state.SoulMarked, Is.EqualTo(PartyRole.CasterDps));
        Assert.That(state.FirstRed, Is.EquivalentTo(overrides.FirstRed));
        Assert.That(state.DivePicks, Is.EqualTo(overrides.DivePicks));
    }
}
