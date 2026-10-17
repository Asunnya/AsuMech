using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M10s.Opener;

namespace AnoMech.Tests;

public class M10sOpenerStateTests
{
    [Test]
    public void FourDifferentPlayersAreTetheredOnACardinalWall()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M10sOpenerState(new Rng(seed));
            Assert.That(state.FloaterOrder.Distinct().Count(), Is.EqualTo(4));
            Assert.That(state.Wall, Is.InRange(0, 3));
            Assert.That(state.PyrotationTarget, Is.EqualTo(PartyRole.RegenHealer).Or.EqualTo(PartyRole.ShieldHealer));
        }
    }

    [Test]
    public void ThePlayerGetsTheFlameFloaterNumberTheyPicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        for (var number = 0; number <= 4; number++)
        for (var seed = 0; seed < 40; seed++)
        {
            var state = new M10sOpenerState(new Rng(seed), new M10sOpenerStateOverrides { PlayerFloaterNumber = number }, player);
            Assert.That(state.FloaterNumber(player), Is.EqualTo(number));
            Assert.That(state.FloaterOrder.Distinct().Count(), Is.EqualTo(4));
        }
    }

    [Test]
    public void TheWestFrameTurnsOntoTheRolledWall()
    {
        var expected = new[] { new Vector3(0f, 0f, -20f), new Vector3(20f, 0f, 0f), new Vector3(0f, 0f, 20f), new Vector3(-20f, 0f, 0f) };
        for (var wall = 0; wall < 4; wall++)
        {
            var state = new M10sOpenerState(new Rng(0), new M10sOpenerStateOverrides { Wall = wall });
            var p = state.FromWestFrame(new Vector2(-20f, 0f));
            Assert.That(Vector3.Distance(p, expected[wall]), Is.LessThan(0.01f));
        }
    }

    [Test]
    public void TheFirstFloaterStandsInTheCornerCounterClockwiseOfRedHot()
    {
        var state = new M10sOpenerState(new Rng(0), new M10sOpenerStateOverrides { Wall = 0 });
        var corner = state.FromWestFrame(new Vector2(-18.8f, 18.8f));
        Assert.That(corner.X, Is.LessThan(0f));
        Assert.That(corner.Z, Is.LessThan(0f));
    }
}
