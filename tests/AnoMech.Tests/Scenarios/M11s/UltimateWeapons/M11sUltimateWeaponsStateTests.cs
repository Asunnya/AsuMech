using System.Numerics;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M11s;
using AnoMech.Scenarios.M11s.UltimateWeapons;

namespace AnoMech.Tests;

public class M11sUltimateWeaponsStateTests
{
    [Test]
    public void EachHalfOfTheSixWeaponsHoldsOneOfEvery()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M11sUltimateWeaponsState(new Rng(seed));
            Assert.That(state.Weapons.Take(3), Is.EquivalentTo(Enum.GetValues<Weapon>()));
            Assert.That(state.Weapons.Skip(3), Is.EquivalentTo(Enum.GetValues<Weapon>()));
            Assert.That(state.OpenSide, Is.InRange(0, 3));
        }
    }

    [Test]
    public void MirroringFlipsEveryWeaponAcrossTheNorthSouthLine()
    {
        var east = new M11sUltimateWeaponsState(new Rng(0), new M11sUltimateWeaponsStateOverrides { Mirrored = false });
        var west = new M11sUltimateWeaponsState(new Rng(0), new M11sUltimateWeaponsStateOverrides { Mirrored = true });
        for (var i = 0; i < M11sUltimateWeaponsState.Strikes; i++)
        {
            var e = east.WeaponSpot(i);
            Assert.That(west.WeaponSpot(i), Is.EqualTo(e with { X = -e.X }));
        }
        Assert.That(east.WeaponSpot(0).X, Is.GreaterThan(0f));
        Assert.That(east.TornadoSpots().Count(), Is.EqualTo(4));
    }

    [Test]
    public void SpotsTurnWithTheOpenSide()
    {
        var expected = new[] { new Vector3(-9f, 0f, 0f), new Vector3(0f, 0f, 9f), new Vector3(9f, 0f, 0f), new Vector3(0f, 0f, -9f) };
        for (var side = 0; side < 4; side++)
        {
            var state = new M11sUltimateWeaponsState(new Rng(0), new M11sUltimateWeaponsStateOverrides { OpenSide = side });
            Assert.That(Vector3.Distance(state.FromSouthOpen(new Vector3(-9f, 0f, 0f)), expected[side]), Is.LessThan(0.01f));
        }
    }
}
