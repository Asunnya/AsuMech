using AsuMech.Core.Native.Implementations;
using NUnit.Framework;

namespace AsuMech.Tests;

public class LaunchRoomTests
{
    private const uint Housing = 14;

    [TestCase(177u)]
    [TestCase(1205u)]
    public void AnInnRoomAlwaysCounts(uint territory)
    {
        Assert.That(LaunchRoom.Allows(territory, LaunchRoom.InnIntendedUse, allowFcChambers: false), Is.True);
        Assert.That(LaunchRoom.Allows(territory, LaunchRoom.InnIntendedUse, allowFcChambers: true), Is.True);
    }

    [TestCase(384u)]
    [TestCase(385u)]
    [TestCase(386u)]
    [TestCase(652u)]
    [TestCase(983u)]
    public void FcPrivateChambersCountOnlyWhenAllowed(uint territory)
    {
        Assert.That(LaunchRoom.Allows(territory, Housing, allowFcChambers: false), Is.False);
        Assert.That(LaunchRoom.Allows(territory, Housing, allowFcChambers: true), Is.True);
    }

    // The house itself, the workshop and the apartments let other players in.
    [TestCase(282u)]
    [TestCase(423u)]
    [TestCase(608u)]
    [TestCase(129u)]
    public void OtherPlacesNeverCount(uint territory)
        => Assert.That(LaunchRoom.Allows(territory, Housing, allowFcChambers: true), Is.False);

    [Test]
    public void TheChamberIdsAreThePrivateChambersRowsOfTheSheet()
    {
        foreach (var id in LaunchRoom.FcPrivateChambers)
        {
            var row = DataminingGameData.TerritoryTypeRaw(id);
            Assert.That(row, Is.Not.Null, $"territory {id}");
            Assert.That(row!.Value.Name, Does.EndWith("1i4"), $"territory {id}");
            Assert.That(row.Value.IntendedUse, Is.EqualTo(Housing), $"territory {id}");
        }
    }
}
