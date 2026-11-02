using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;
using AnoMech.Scenarios;
using AnoMech.Windows.Asu;
using NUnit.Framework;

namespace AnoMech.Tests.Windows.Asu;

public class CatalogViewTests
{
    private sealed class Zone(string shortName, string category, string? tier, string expansion) : IZone
    {
        public string Name => shortName;
        public string ShortName => shortName;
        public string Category => category;
        public string? Tier => tier;
        public string? Expansion => expansion;
        public uint TerritoryId => 0;
        public Vector3 Origin => Vector3.Zero;
        public IReadOnlyList<WaymarkLayout> WaymarkPresets => [];
        public DutyInitData DutyInitData => DutyInitData.Unknown(Vector3.Zero);
    }

    private static readonly IZone[] Zones =
    [
        new Zone("M10S", "Savage", "Heavyweight", "Dawntrail"),
        new Zone("M1S", "Savage", "Light-heavyweight", "Dawntrail"),
        new Zone("FRU", "Ultimate", null, "Dawntrail"),
        new Zone("M9S", "Savage", "Heavyweight", "Dawntrail"),
        new Zone("TOP", "Ultimate", null, "Endwalker"),
        new Zone("M5S", "Savage", "Cruiserweight", "Dawntrail"),
    ];

    private static List<(string, string[])> Shape(IReadOnlyList<FightGroup> groups)
        => groups.Select(g => (g.Label, g.Fights.Select(f => f.ShortName).ToArray())).ToList();

    [Test]
    public void GroupsInTierOrderAndSortsNumerically()
        => Assert.That(Shape(CatalogView.Groups(Zones, "Dawntrail", null)), Is.EqualTo(new List<(string, string[])>
        {
            ("Ultimate", ["FRU"]),
            ("Heavyweight", ["M9S", "M10S"]),
            ("Cruiserweight", ["M5S"]),
            ("Light-heavyweight", ["M1S"]),
        }));

    [Test]
    public void NullScopeMeansEverything()
        => Assert.That(CatalogView.Groups(Zones, null, null).Sum(g => g.Fights.Count), Is.EqualTo(Zones.Length));

    [Test]
    public void TypeFilterKeepsOnlyThatCategory()
        => Assert.That(Shape(CatalogView.Groups(Zones, null, "Ultimate")),
            Is.EqualTo(new List<(string, string[])> { ("Ultimate", ["FRU", "TOP"]) }));

    [Test]
    public void ExpansionsAreNewestFirst()
        => Assert.That(CatalogView.Expansions(Zones), Is.EqualTo(new[] { "Dawntrail", "Endwalker" }));

    [Test]
    public void RealCatalogGroupsAreStable()
    {
        var zones = ScenarioCatalog.Create().Zones;
        Assert.That(Shape(CatalogView.Groups(zones, null, null)), Is.EqualTo(Shape(CatalogView.Groups(zones.Reverse(), null, null))));
    }
}
