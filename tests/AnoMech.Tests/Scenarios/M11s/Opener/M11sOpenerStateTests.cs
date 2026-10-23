using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M11s;
using AnoMech.Scenarios.M11s.Opener;

namespace AnoMech.Tests;

public class M11sOpenerStateTests
{
    [Test]
    public void EachSetHoldsOneOfEveryWeaponAndAHealerTakesTheStack()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M11sOpenerState(new Rng(seed));
            foreach (var set in state.Weapons)
                Assert.That(set, Is.EquivalentTo(Enum.GetValues<Weapon>()));
            Assert.That(state.FirstWeaponSteps, Is.All.InRange(0, M11sOpenerState.WeaponSteps - 1));
            Assert.That(state.CrushingCometTarget, Is.EqualTo(PartyRole.RegenHealer).Or.EqualTo(PartyRole.ShieldHealer));
            Assert.That(state.StackOnSet(0), Is.Not.EqualTo(state.StackOnSet(1)));
        }
    }

    [Test]
    public void WeaponsSitOnTheRingAndGoClockwise120Apart()
    {
        var state = new M11sOpenerState(new Rng(0), new M11sOpenerStateOverrides { FirstWeaponStep = 0 });
        Assert.That(Vector3.Distance(state.WeaponSpot(0, 0), new Vector3(0f, 0f, 12f)), Is.LessThan(0.01f));
        Assert.That(Vector3.Distance(state.WeaponSpot(0, 1), new Vector3(-10.392f, 0f, -6f)), Is.LessThan(0.01f));
        Assert.That(Vector3.Distance(state.WeaponSpot(0, 2), new Vector3(10.392f, 0f, -6f)), Is.LessThan(0.01f));
    }

    [Test]
    public void TheFirstWeaponFacesOutwardAndTheRestTurn30BackFromTheirBearing()
    {
        for (var step = 0; step < M11sOpenerState.WeaponSteps; step++)
        {
            var state = new M11sOpenerState(new Rng(0), new M11sOpenerStateOverrides { FirstWeaponStep = step });
            Assert.That(Wrap(state.ArrivalBearing(0, 0) - state.WeaponBearing(0, 0)), Is.EqualTo(0f).Within(0.001f));
            for (var i = 1; i < 3; i++)
                Assert.That(Wrap(state.ArrivalBearing(0, i) - state.WeaponBearing(0, i)), Is.EqualTo(-MathF.PI / 6f).Within(0.001f));
        }
    }

    private static float Wrap(float a) => MathF.IEEERemainder(a, MathF.Tau);

    [Test]
    public void OverridesPinEveryRoll()
    {
        Weapon[] order = [Weapon.Sword, Weapon.Axe, Weapon.Scythe];
        foreach (var axe in new[] { true, false })
        foreach (var stack in new[] { true, false })
        foreach (var healer in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer })
        for (var seed = 0; seed < 20; seed++)
        {
            var state = new M11sOpenerState(new Rng(seed), new M11sOpenerStateOverrides
            {
                AxeTrophyFirst = axe,
                StackFirst = stack,
                CrushingCometTarget = healer,
                FirstWeapons = order,
                SecondWeapons = order,
                FirstWeaponStep = 3,
                SecondWeaponStep = 7,
            });
            Assert.That(state.AxeTrophyFirst, Is.EqualTo(axe));
            Assert.That(state.StackOnSet(0), Is.EqualTo(stack));
            Assert.That(state.CrushingCometTarget, Is.EqualTo(healer));
            Assert.That(state.Weapons[0], Is.EqualTo(order));
            Assert.That(state.Weapons[1], Is.EqualTo(order));
            Assert.That(state.FirstWeaponSteps, Is.EqualTo(new[] { 3, 7 }));
        }
    }
}
