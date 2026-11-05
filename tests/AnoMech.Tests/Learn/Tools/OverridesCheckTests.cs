using System.Linq;
using AnoMech.Learn.Tools;
using AnoMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Tools;

public class OverridesCheckTests
{
    [Test]
    public void GarudaDrawsEveryRollBeforePinning()
    {
        var report = OverridesCheck.Run(typeof(UwuP1GarudaScenario));
        TestContext.Out.WriteLine(report.Table());
        Assert.That(report.Error, Is.Null);
        Assert.That(report.Rows.Where(r => r.Status == PinStatus.Shifts), Is.Empty);
        Assert.That(report.Rows.Where(r => r.Status == PinStatus.Ok).Select(r => r.Roll), Is.SupersetOf(new[] { "MistralSongTarget", "SatinPlumesFirst", "MesohighTargets" }));
        // The song spots are vectors in the State and bearings in the overrides: not the same name.
        Assert.That(report.Rows.Single(r => r.Roll == "SuparnaSongSpot").Status, Is.EqualTo(PinStatus.NotPinnable));
    }
}
