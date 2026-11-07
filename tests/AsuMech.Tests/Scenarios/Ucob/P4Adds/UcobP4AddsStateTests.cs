using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P4Adds;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Tests;

public class UcobP4AddsStateTests
{
    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    [Test]
    public void EachGenerateMarksThreeDistinctDps([Range(0, 49)] int seed)
    {
        var state = new UcobP4AddsState(new Rng(seed));
        Assert.That(state.HatchTargets, Has.Count.EqualTo(UcobP4AddsState.Generates));
        foreach (var targets in state.HatchTargets)
        {
            Assert.That(targets, Has.Count.EqualTo(UcobP4AddsState.HatchCount));
            Assert.That(targets, Is.Unique);
            Assert.That(targets, Is.SubsetOf(Dps));
        }
    }

    [Test]
    public void APickedRoleIsAmongThatGeneratesTargets([ValueSource(nameof(Dps))] PartyRole role, [Range(0, 1)] int set)
    {
        var overrides = new UcobP4AddsStateOverrides();
        overrides.HatchIncludes[set] = role;
        for (var seed = 0; seed < 20; seed++)
            Assert.That(new UcobP4AddsState(new Rng(seed), overrides).HatchTargets[set], Does.Contain(role));
    }

    [Test]
    public void TheSecondQuoteOpensTheOtherWay([Range(0, 49)] int seed)
    {
        var state = new UcobP4AddsState(new Rng(seed));
        Assert.That(state.NaelQuotes, Has.Count.EqualTo(UcobP4AddsState.Quotes));
        Assert.That(state.NaelQuotes.Select(q => q.Moves[0]), Is.EquivalentTo(new[] { NaelMove.LunarDynamo, NaelMove.IronChariot }));
    }

    [Test]
    public void PickedQuotesAreKept([Range(0, 3)] int first, [Values(0, 1)] int second)
    {
        var overrides = new UcobP4AddsStateOverrides { FirstQuote = first, SecondQuote = second };
        var state = new UcobP4AddsState(new Rng(5), overrides);
        var other = first < UcobP4AddsState.InQuotes.Length ? UcobP4AddsState.OutQuotes : UcobP4AddsState.InQuotes;
        Assert.That(state.NaelQuotes[0], Is.SameAs(UcobP4AddsState.QuoteChoices[first]));
        Assert.That(state.NaelQuotes[1], Is.SameAs(other[second]));
    }

    [Test]
    public void TheSecondQuotePickNamesWhatItEndsOn([Values(0, 1)] int second)
    {
        var ends = second == 0 ? NaelMove.RavenDive : NaelMove.ThermionicBeam;
        Assert.Multiple(() =>
        {
            Assert.That(UcobP4AddsState.InQuotes[second].Moves[^1], Is.EqualTo(ends));
            Assert.That(UcobP4AddsState.OutQuotes[second].Moves[^1], Is.EqualTo(ends));
        });
    }

    [Test]
    public void EveryQuoteHasThreeMovesAndOneSpreadOnly()
    {
        foreach (var quote in UcobP4AddsState.QuoteChoices)
        {
            Assert.That(quote.Moves, Has.Length.EqualTo(3));
            Assert.That(quote.Moves.Count(m => m == NaelMove.RavenDive), Is.EqualTo(1));
        }
    }

    [Test]
    public void EachTwisterCastDropsFourUnderDifferentPlayers([Range(0, 49)] int seed)
    {
        var state = new UcobP4AddsState(new Rng(seed));
        Assert.That(state.TwisterTargets, Has.Count.EqualTo(UcobP4AddsState.TwisterCasts));
        foreach (var targets in state.TwisterTargets)
        {
            Assert.That(targets, Has.Count.EqualTo(UcobTwisters.Count));
            Assert.That(targets, Is.Unique);
        }
    }

    [Test]
    public void TheChosenTankHoldsTwintaniaAndTheOtherNael([Values(MainTank, OffTank)] PartyRole tank)
    {
        var state = new UcobP4AddsState(new Rng(1), new UcobP4AddsStateOverrides { TwintaniaTank = tank });
        Assert.Multiple(() =>
        {
            Assert.That(state.TwintaniaTank, Is.EqualTo(tank));
            Assert.That(state.NaelTank, Is.Not.EqualTo(tank).And.AnyOf(MainTank, OffTank));
            Assert.That(UcobP4AddsScenario.TwintaniaHolder(state, UcobP4AddsScenario.AggroSwap - 1f), Is.EqualTo(tank));
            Assert.That(UcobP4AddsScenario.TwintaniaHolder(state, UcobP4AddsScenario.AggroSwap + 1f), Is.EqualTo(state.NaelTank));
            Assert.That(UcobP4AddsScenario.NaelHolder(state, UcobP4AddsScenario.AggroSwap + 1f), Is.EqualTo(tank));
        });
    }

    [Test]
    public void ATankByDefaultMainTankHoldsTwintania()
        => Assert.That(new UcobP4AddsState(new Rng(9)).TwintaniaTank, Is.EqualTo(MainTank));

    [Test]
    public void PickedDiveAndBeamTargetsAreKept([Values] PartyRole role)
    {
        var overrides = new UcobP4AddsStateOverrides();
        overrides.RavenDiveTargets[1] = role;
        overrides.BeamTargets[0] = role;
        var state = new UcobP4AddsState(new Rng(3), overrides);
        Assert.That(state.RavenDiveTargets[1], Is.EqualTo(role));
        Assert.That(state.BeamTargets[0], Is.EqualTo(role));
    }
}
