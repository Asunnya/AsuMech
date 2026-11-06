using System.Linq;
using AsuMech.Scenarios;
using AsuMech.Windows.Asu;
using NUnit.Framework;

namespace AsuMech.Tests.Windows.Asu;

public class ScenarioSearchTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    [Test]
    public void TitanFindsUwuP3()
    {
        var hits = ScenarioSearch.Matches(Catalog.Scenarios, "titan");
        Assert.That(hits, Is.Not.Empty);
        Assert.That(hits[0].Phase.Zone.ShortName, Is.EqualTo("UWU"));
    }

    [Test]
    public void EveryTermMustMatchAnywhere()
    {
        var hits = ScenarioSearch.Matches(Catalog.Scenarios, "dsr p6");
        Assert.That(hits, Is.Not.Empty);
        Assert.That(hits.All(s => s.Phase.Zone.ShortName == "DSR"), Is.True);
    }

    [Test]
    public void EmptyQueryListsEverythingUpToTheCap()
        => Assert.That(ScenarioSearch.Matches(Catalog.Scenarios, " ").Count,
            Is.EqualTo(System.Math.Min(Catalog.Scenarios.Count, ScenarioSearch.MaxResults)));

    [Test]
    public void RecentIsNewestFirstDedupedAndCapped()
    {
        var recent = ScenarioSearch.PushRecent([], "a");
        recent = ScenarioSearch.PushRecent(recent, "b");
        recent = ScenarioSearch.PushRecent(recent, "a");
        Assert.That(recent, Is.EqualTo(new[] { "a", "b" }));
        recent = ScenarioSearch.PushRecent(recent, "c");
        recent = ScenarioSearch.PushRecent(recent, "d");
        Assert.That(recent, Is.EqualTo(new[] { "d", "c", "a" }));
    }

    [Test]
    public void RecentSkipsScenariosThatNoLongerExist()
    {
        var first = Catalog.Scenarios[0];
        Assert.That(ScenarioSearch.Recent(Catalog.Scenarios, ["gone", first.FullName()]), Is.EqualTo(new[] { first }));
    }
}
