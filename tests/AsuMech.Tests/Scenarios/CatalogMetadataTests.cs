using System.Linq;
using AsuMech.Scenarios;
using NUnit.Framework;

namespace AsuMech.Tests.Scenarios;

public class CatalogMetadataTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    [Test]
    public void EveryZoneHasAUniqueShortName()
    {
        var names = Catalog.Zones.Select(z => z.ShortName).ToList();
        Assert.That(names, Is.Unique);
        Assert.That(names, Has.None.Matches<string>(n => string.IsNullOrWhiteSpace(n) || n.Length > 6));
    }

    [Test]
    public void EveryZoneHasAnExpansion()
        => Assert.That(Catalog.Zones.Where(z => z.Expansion == null).Select(z => z.Name), Is.Empty);

    [Test]
    public void EverySavageZoneHasATier()
        => Assert.That(Catalog.Zones.Where(z => z.Category == "Savage" && z.Tier == null).Select(z => z.Name), Is.Empty);

    [Test]
    public void UltimatesHaveNoTier()
        => Assert.That(Catalog.Zones.Where(z => z.Category == "Ultimate" && z.Tier != null).Select(z => z.Name), Is.Empty);
}
