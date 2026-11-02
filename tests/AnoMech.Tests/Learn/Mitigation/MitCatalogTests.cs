using System.Linq;
using AnoMech.Learn.Mitigation;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Mitigation;

public class MitCatalogTests
{
    private static readonly DataminingGameData Data = new(new FakeRsvFunctions());

    [Test]
    public void EveryIdNamesTheActionTheSheetNames()
    {
        var wrong = MitCatalog.All
            .Select(a => (a, Row: Data.Action(a.Id)))
            .Where(x => x.Row is null || x.Row.Name != x.a.Name)
            .Select(x => $"{x.a.Id} {x.a.Name} -> {x.Row?.Name ?? "missing"}")
            .ToList();
        Assert.That(wrong, Is.Empty);
    }

    [Test]
    public void EveryActionIsAPlayerAction()
    {
        // Follow-ups and pet actions the sheet doesn't flag as player actions but players press.
        string[] followUps = ["Fey Union", "Eukrasian Prognosis II", "Stellar Detonation", "Shield Wall", "Stronghold", "Last Bastion", "Land Waker", "Dark Force", "Gunmetal Soul"];
        var odd = MitCatalog.All.Where(a => !followUps.Contains(a.Name)).Select(a => (a, Row: Data.Action(a.Id)!)).Where(x => !x.Row.IsPlayerAction).Select(x => x.a.Name).ToList();
        Assert.That(odd, Is.Empty);
    }

    [Test]
    public void NamesAreUnique()
        => Assert.That(MitCatalog.All.Select(a => a.Name.ToLowerInvariant()), Is.Unique);
}
