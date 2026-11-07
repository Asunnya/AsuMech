using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3Blackfire;
using State = AsuMech.Scenarios.Ucob.P3Blackfire.UcobP3BlackfireState;

namespace AsuMech.Tests;

public class UcobP3BlackfireStateTests
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    [Test]
    public void TwoSupportsAndTwoDpsStack([Range(0, 49)] int seed)
    {
        var state = new State(new Rng(seed));
        Assert.That(state.StackTargets, Is.Unique);
        Assert.That(state.StackTargets.Count(State.Supports.Contains), Is.EqualTo(2));
        Assert.That(state.StackTargets.Count(State.Dps.Contains), Is.EqualTo(2));
    }

    [Test]
    public void FourHypernovasFall([Range(0, 19)] int seed)
        => Assert.That(new State(new Rng(seed)).HypernovaTargets, Has.Count.EqualTo(State.HypernovaCount));

    [Test]
    public void APickedStackRoleIsMarked([ValueSource(nameof(Everyone))] PartyRole role)
    {
        for (var seed = 0; seed < 10; seed++)
        {
            var state = new State(new Rng(seed), new UcobP3BlackfireStateOverrides { StackIncludes = role });
            Assert.That(state.StackTargets, Does.Contain(role));
            Assert.That(state.StackTargets, Has.Count.EqualTo(4));
        }
    }

    [Test]
    public void APickedTowerRoleIsNotMarked([ValueSource(nameof(Everyone))] PartyRole role)
    {
        for (var seed = 0; seed < 10; seed++)
        {
            var state = new State(new Rng(seed), new UcobP3BlackfireStateOverrides { TowerIncludes = role });
            Assert.That(state.StackTargets, Does.Not.Contain(role));
            Assert.That(state.StackTargets, Has.Count.EqualTo(4));
        }
    }

    [Test]
    public void ARolePickedForBothStacks([ValueSource(nameof(Everyone))] PartyRole role)
        => Assert.That(new State(new Rng(5), new UcobP3BlackfireStateOverrides { StackIncludes = role, TowerIncludes = role }).StackTargets, Does.Contain(role));

    [Test]
    public void AStackAndATowerPickedTogetherAreBothKept([ValueSource(nameof(Everyone))] PartyRole stack, [ValueSource(nameof(Everyone))] PartyRole tower)
    {
        if (stack == tower) return;
        var state = new State(new Rng(9), new UcobP3BlackfireStateOverrides { StackIncludes = stack, TowerIncludes = tower });
        Assert.That(state.StackTargets, Does.Contain(stack).And.Not.Contain(tower));
    }

    [Test]
    public void APickedLiquidHellTargetIsKept([ValueSource(nameof(Everyone))] PartyRole role)
        => Assert.That(new State(new Rng(1), new UcobP3BlackfireStateOverrides { LiquidHellTarget = role }).LiquidHellTarget, Is.EqualTo(role));

    [Test]
    public void APickedNaelDirectionIsKept([ValueSource(typeof(Direction), nameof(Direction.All))] Direction direction)
        => Assert.That(new State(new Rng(2), new UcobP3BlackfireStateOverrides { NaelFrom = direction }).NaelFrom, Is.EqualTo(direction));
}
