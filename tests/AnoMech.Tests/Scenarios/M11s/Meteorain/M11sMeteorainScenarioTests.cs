using System.Numerics;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M11s.Meteorain;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M11s.M11sConstants;
using static AnoMech.Tests.NegativeRun;
using Scenario = AnoMech.Scenarios.M11s.Meteorain.M11sMeteorainScenario;

namespace AnoMech.Tests;

// Times are the scenario's absolute timeline; the player follows the strat until taken over.
public class M11sMeteorainScenarioTests
{
    // North and east shades every set: the north one tethers the north-east meteor.
    private static void Pinned(M11sMeteorainStateOverrides o) => o.ShadeSides = [(0, 1), (0, 1), (0, 1)];

    private static float FirstFall => Scenario.MeteorMarkedAt[0] + Scenario.MeteorFallsAfter;

    [Test]
    public void ShadesTetherTheTwoMeteorsNearestTheBoss()
    {
        var state = new M11sMeteorainState(new Rng(0), new M11sMeteorainStateOverrides { ShadeSides = [(0, 1), (0, 1), (0, 1)] });
        state.Meteors.AddRange([new Vector3(5f, 0f, -5f), new Vector3(-5f, 0f, 5f), new Vector3(-15f, 0f, 15f)]);
        var tethered = state.TetheredMeteors(0);
        Assert.That(tethered, Has.Count.EqualTo(2));
        Assert.That(tethered.Single(t => t.Side == 0).Meteor, Is.EqualTo(new Vector3(5f, 0f, -5f)));
        Assert.That(tethered.Single(t => t.Side == 1).Meteor, Is.EqualTo(new Vector3(-5f, 0f, 5f)));
    }

    [Test]
    public void AMeteorBehindAnotherHidesFromTheShockwave()
    {
        var meteors = new[] { new Vector2(-5f, 5f) };
        Assert.That(Scenario.Hidden(Vector2.Zero, new Vector2(-10f, 10f), meteors), Is.True);
        Assert.That(Scenario.Hidden(Vector2.Zero, new Vector2(10f, 10f), meteors), Is.False);
        Assert.That(Scenario.Hidden(Vector2.Zero, new Vector2(-2f, 2f), meteors), Is.False);
    }

    [Test]
    public void MeteorsDroppedTooCloseWipeTheParty()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sMeteorainStateOverrides>(Pinned)
            .TeleportAt(FirstFall - 1f, to: new Vector2(-3f, 5f))
            .ShouldKill(ActionId.UnmitigatedExplosion, AllBut());

    [Test]
    public void StayingOutInTheOpenForTheShockwavesKills()
        => Negative<Scenario>(MeleeDpsA)
            .Overrides<M11sMeteorainStateOverrides>(Pinned)
            .TeleportAt(Scenario.ShockwaveAt[0] - 1f, to: new Vector2(10f, 10f))
            .ShouldKill(ActionId.Shockwave, MeleeDpsA);

    [Test]
    public void AHealerBetweenTheShadeAndTheTankTakesTheTether()
        => Negative<Scenario>(RegenHealer)
            .Overrides<M11sMeteorainStateOverrides>(Pinned)
            .TeleportAt(FirstFall + 2f, to: new Vector2(1.75f, -18f))
            .ShouldKill(ActionId.ForegoneFatality, RegenHealer);
}
