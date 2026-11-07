using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3Quickmarch;
using static AsuMech.Core.Game.Party.PartyRole;
using State = AsuMech.Scenarios.Ucob.P3Quickmarch.UcobP3QuickmarchState;

namespace AsuMech.Tests;

public class UcobP3QuickmarchStateTests
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    [Test]
    public void BothHealersAndOneDpsBaitEarthShakersWhileTheOtherThreeDpsStack([Range(0, 49)] int seed)
    {
        var state = new State(new Rng(seed));
        Assert.That(state.EarthShakerTargets, Is.EquivalentTo(new[] { RegenHealer, ShieldHealer, state.EarthShakerDps }));
        Assert.That(State.Dps, Does.Contain(state.EarthShakerDps));
        Assert.That(state.StackTargets, Is.EquivalentTo(State.Dps.Except([state.EarthShakerDps])));
    }

    [Test]
    public void FourDistinctNonTanksSpread([Range(0, 49)] int seed)
    {
        var state = new State(new Rng(seed));
        Assert.That(state.SpreadTargets, Has.Count.EqualTo(State.SpreadCount).And.Unique);
        Assert.That(state.SpreadTargets, Is.SubsetOf(State.NonTanks));
    }

    [Test]
    public void FourDistinctPlayersGetAPuddleAndATwister([Range(0, 49)] int seed)
    {
        var state = new State(new Rng(seed));
        Assert.That(state.PuddleTargets, Has.Count.EqualTo(State.PuddleCount).And.Unique);
        Assert.That(state.TwisterTargets, Has.Count.EqualTo(State.TwisterCount).And.Unique);
    }

    [Test]
    public void APickedEarthShakerDpsIsKept([ValueSource(typeof(State), nameof(State.Dps))] PartyRole role)
    {
        for (var seed = 0; seed < 10; seed++)
        {
            var state = new State(new Rng(seed), new UcobP3QuickmarchStateOverrides { EarthShakerDps = role });
            Assert.That(state.EarthShakerDps, Is.EqualTo(role));
            Assert.That(state.StackTargets, Does.Not.Contain(role));
        }
    }

    [Test]
    public void ASupportPickedAsTheEarthShakerDpsIsIgnored([Values(MainTank, OffTank, RegenHealer, ShieldHealer)] PartyRole role)
        => Assert.That(State.Dps, Does.Contain(new State(new Rng(4), new UcobP3QuickmarchStateOverrides { EarthShakerDps = role }).EarthShakerDps));

    [Test]
    public void APickedSpreadIsAmongTheSpreads([ValueSource(typeof(State), nameof(State.NonTanks))] PartyRole role)
    {
        for (var seed = 0; seed < 10; seed++)
            Assert.That(new State(new Rng(seed), new UcobP3QuickmarchStateOverrides { SpreadIncludes = role }).SpreadTargets, Does.Contain(role));
    }

    [Test]
    public void APickedPuddleIsAmongThePuddles([ValueSource(nameof(Everyone))] PartyRole role)
    {
        for (var seed = 0; seed < 10; seed++)
            Assert.That(new State(new Rng(seed), new UcobP3QuickmarchStateOverrides { PuddleIncludes = role }).PuddleTargets, Does.Contain(role));
    }

    [Test]
    public void APickedDiveDirectionIsKept([ValueSource(typeof(Direction), nameof(Direction.All))] Direction direction)
        => Assert.That(new State(new Rng(2), new UcobP3QuickmarchStateOverrides { DiveFrom = direction }).DiveFrom, Is.EqualTo(direction));
}
