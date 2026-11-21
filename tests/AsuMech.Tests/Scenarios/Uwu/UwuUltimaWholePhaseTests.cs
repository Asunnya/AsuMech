using System.Globalization;
using AsuMech.Scenarios.Uwu.PrimalRoulette;
using NUnit.Framework;
using static AsuMech.Scenarios.Uwu.UwuConstants;
using static AsuMech.Tests.WholePhaseTrace;

namespace AsuMech.Tests;

// The Ultima phase played whole: each part ends on the cast that opens the next one, so the next
// part picks up after it instead of casting it again. Gaps are the fight's, from the cast or hit
// that ends one part to the first cast of the next part's own.
public class UwuUltimaWholePhaseTests
{
    private const string Predation = "Ultimate Predation";
    private const string Annihilation = "Ultimate Annihilation";
    private const string Suppression = "Ultimate Suppression";
    private const string Roulette = "Primal Roulette";
    private const float Tolerance = 1f;

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void EachPartPicksUpWhereThePreviousOneLeftOff(int seed)
    {
        var trace = Execute(typeof(PrimalRouletteScenario), seed);
        Assert.That(trace.Run.Passed, trace.Run.ToString());
        Assert.That(trace.Parts.Select(p => p.Name), Is.EqualTo(new[] { Predation, Annihilation, Suppression, Roulette }));

        Assert.That(trace.Rows.Count(Cast(ActionId.UltimateAnnihilation)), Is.EqualTo(1));
        Assert.That(trace.Rows.Count(Cast(ActionId.UltimateSuppression)), Is.EqualTo(1));
        Assert.That(trace.Rows.Where(r => r.Part == Roulette).Count(Cast(ActionId.TankPurge)), Is.Zero);

        var homingLasers = trace.Last(Predation, Ability(ActionId.HomingLasers));
        Assert.That(trace.First(Annihilation, Cast(ActionId.WeightOfTheLandTitan)) - homingLasers, Is.EqualTo(16.0f).Within(Tolerance));

        var suppression = trace.First(Annihilation, Cast(ActionId.UltimateSuppression));
        Assert.That(trace.First(Suppression, Cast(ActionId.EruptionIfrit)) - suppression, Is.EqualTo(11.81f).Within(Tolerance));

        // The fight casts Ultima 10.38 s after Tank Purge, but Suppression's razor plumes keep that
        // part open until 13.9 s after it; Roulette can only start once it has ended.
        var tankPurge = trace.Last(Suppression, Cast(ActionId.TankPurge));
        Assert.That(trace.First(Roulette, Cast(ActionId.Ultima)) - tankPurge, Is.InRange(10.38f, 16.5f));
    }

    // The music restarts with each part at its own place on the track. Annihilation's long ending,
    // with Ultima above half, puts the fight 19.92 s further on than its short one, which is the
    // one the track times come from; Roulette starts late (see above), so its track starts behind.
    [Test]
    public void TheMusicCarriesOnFromPartToPart()
    {
        var trace = Execute(typeof(PrimalRouletteScenario), 0);
        var starts = trace.Rows.Where(r => r.Kind == "bgm").ToList();
        Assert.That(starts.Select(r => r.Part).ToList(), Is.EqualTo(trace.Parts.Select(p => p.Name).ToList()));
        Assert.That(SecondsIn(starts[0]), Is.EqualTo(67.9f).Within(0.1f));
        float Behind(int i) => starts[i].At - starts[i - 1].At - (SecondsIn(starts[i]) - SecondsIn(starts[i - 1]));
        Assert.That(Behind(1), Is.EqualTo(0f).Within(0.5f));
        Assert.That(Behind(2), Is.EqualTo(19.92f).Within(0.5f));
        Assert.That(Behind(3), Is.InRange(0f, 6.5f));
    }

    internal static float SecondsIn(Row bgm)
        => float.Parse(bgm.Fields["detail"].Split('=')[1], CultureInfo.InvariantCulture);
}
