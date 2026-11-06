using System.Linq;
using AsuMech.Learn.Mitigation;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Mitigation;

public class MitSheetImportTests
{
    private static MitScenario Garuda => MitSheetImport.Import(CastTimelineTests.Fixture("ethel-garuda.csv"), CastTimelineTests.Garuda, MitAliases.Default);

    [Test]
    public void RowsAnchorOnCastsByNameAndOccurrence()
    {
        var s = Garuda;
        Assert.That(s.Mechanics.Select(m => m.Label), Does.Contain("Mistral Shriek").And.Contain("Friction #1").And.Contain("Downburst & Spiny"));
        Assert.That(s.Mechanics.Single(m => m.Label == "Friction #2").Anchor, Is.EqualTo(new MitAnchor("Friction", 2)));
        Assert.That(s.Mechanics.Single(m => m.Label == "Downburst & Spiny").Anchor!.Cast, Is.EqualTo("Downburst"));
        Assert.That(s.Mechanics.Single(m => m.Label == "Cleanses").Anchor, Is.Null, "no cast is named like it");
    }

    [Test]
    public void CellsBecomeItemsThroughTheAliases()
    {
        var shriek = Garuda.Mechanics.Single(m => m.Label == "Mistral Shriek");
        Assert.That(shriek.Columns["MT"][0].Accept, Is.EqualTo(new[] { "Reprisal" }));
        Assert.That(shriek.Columns["Scholar"][0].Accept, Is.EqualTo(new[] { "Sacred Soil" }));
        var sage = shriek.Columns["Sage"];
        Assert.That(sage[0].Accept, Is.EqualTo(new[] { "Zoe", "Eukrasian Prognosis II" }));
        Assert.That(sage[1].Accept, Is.EqualTo(new[] { "Physis II" }));
        Assert.That(sage[1].When, Is.EqualTo("afterCastEnd"));
        Assert.That(shriek.Columns["White Mage"][0].When, Is.EqualTo("afterCastEnd"));
        Assert.That(shriek.Columns["Caster"][0].Accept, Is.EqualTo(new[] { "Addle" }));
    }

    [Test]
    public void StatesForbidsTargetsAndRemindersAreRead()
    {
        var s = Garuda;
        var friction2 = s.Mechanics.Single(m => m.Label == "Friction #2");
        Assert.That(friction2.Columns["Sage"][0].ExpectStatus, Is.EqualTo("Kerachole"));
        Assert.That(friction2.Columns["Melee 2"][0].When, Is.EqualTo("afterStatusEnds:Feint"));
        var downburst = s.Mechanics.Single(m => m.Label == "Downburst & Spiny");
        Assert.That(downburst.Columns["Scholar"][0].Target, Is.EqualTo("OT"));
        Assert.That(downburst.Columns["Scholar"][0].Accept, Is.EqualTo(new[] { "Deployment Tactics" }));
        var song = s.Mechanics.Single(m => m.Label == "Mistral Song");
        Assert.That(song.Columns["Scholar"][0].Reminder, Is.Not.Null);
        Assert.That(song.Columns["Astrologian"][0].When, Is.EqualTo("afterAction:Divination"));
    }

    [Test]
    public void ForbiddenLinesBecomeForbidItems()
    {
        var items = MitSheetImport.Items("DO NOT REPRISAL / FEINT / ADDLE", MitAliases.Default);
        Assert.That(items[0].Forbid, Is.EquivalentTo(new[] { "Reprisal", "Feint", "Addle" }));
    }

    [Test]
    public void UnreadableTextStaysUnresolved()
    {
        var items = MitSheetImport.Items("Banana Shield", MitAliases.Default);
        Assert.That(items[0].Unresolved, Is.EqualTo("Banana Shield"));
    }
}
