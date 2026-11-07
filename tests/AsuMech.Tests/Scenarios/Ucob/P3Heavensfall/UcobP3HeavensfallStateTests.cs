using System;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob.P3Heavensfall;

namespace AsuMech.Tests;

public class UcobP3HeavensfallStateTests
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    [Test]
    public void EightTowersAlwaysIncludingTheOneUnderNael([Range(0, 99)] int seed)
    {
        var towers = new UcobP3HeavensfallState(new Rng(seed)).TowerSlotsTaken;
        Assert.That(towers, Has.Count.EqualTo(UcobP3HeavensfallState.Towers).And.Unique.And.Contains(0));
        Assert.That(towers.All(s => s is >= 0 and < UcobP3HeavensfallState.TowerSlots));
    }

    [Test]
    public void TheBossesStandAStepApartWithBahamutCounterClockwiseOfTwintania([Values] HeavensfallFormation formation, [Range(0, 7)] int step)
    {
        var state = new UcobP3HeavensfallState(new Rng(1), new UcobP3HeavensfallStateOverrides { Formation = formation, NaelStep = step });
        var bearings = new[] { state.BahamutBearing, state.NaelBearing, state.TwintaniaBearing }.OrderBy(b => b).ToList();
        Assert.That(bearings[1] - bearings[0], Is.EqualTo(45f));
        Assert.That(bearings[2] - bearings[1], Is.EqualTo(45f));
        Assert.That(state.TwintaniaBearing - state.BahamutBearing, Is.GreaterThan(0f));
    }

    [Test]
    public void EachMechanicMarksDistinctPlayers([Range(0, 49)] int seed)
    {
        var state = new UcobP3HeavensfallState(new Rng(seed));
        Assert.That(state.MegaflareDiveTarget, Is.Not.EqualTo(state.TwistingDiveTarget));
        Assert.That(state.TwisterTargets, Has.Count.EqualTo(UcobP3HeavensfallState.Twisters).And.Unique);
        Assert.That(state.MegaflarePuddleTargets, Has.Count.EqualTo(UcobP3HeavensfallState.MegaflarePuddles).And.Unique);
    }

    [Test]
    public void PickedRolesAreAmongTheirTargets([ValueSource(nameof(Everyone))] PartyRole role)
    {
        var state = new UcobP3HeavensfallState(new Rng(9), new UcobP3HeavensfallStateOverrides
        {
            DiveIncludes = role,
            TwisterIncludes = role,
            MegaflarePuddleIncludes = role,
            FireballTarget = role,
            HypernovaTarget = role,
        });
        Assert.That(new[] { state.MegaflareDiveTarget, state.TwistingDiveTarget }, Does.Contain(role));
        Assert.That(state.TwisterTargets, Does.Contain(role));
        Assert.That(state.MegaflarePuddleTargets, Does.Contain(role));
        Assert.That(state.FireballTarget, Is.EqualTo(role));
        Assert.That(state.HypernovaTarget, Is.EqualTo(role));
    }

    [Test]
    public void TheFirstSlicesStartBesideNaelOnTheSweepsSide([Values] bool clockwise)
    {
        var state = new UcobP3HeavensfallState(new Rng(2), new UcobP3HeavensfallStateOverrides { NaelStep = 0, BurstsClockwise = clockwise });
        Assert.That(state.FirstBurstBearing, Is.EqualTo(clockwise ? 45f : -45f));
    }
}
