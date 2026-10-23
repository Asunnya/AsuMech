using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.M11s.Opener;
using static AnoMech.Core.Game.Party.PartyRole;
using static AnoMech.Scenarios.M11s.M11sConstants;

namespace AnoMech.Tests;

public class M11sOpenerAiTests
{
    [Test]
    public void TheFirstCometSpreadsAreTheRawSteelAxeSpreadsWithTheTanksSplit()
    {
        Assert.That(M11sOpenerAi.CometSpread(MainTank, Vector2.Zero), Is.EqualTo(new Vector2(-5f, -5f)));
        Assert.That(M11sOpenerAi.CometSpread(OffTank, Vector2.Zero), Is.EqualTo(new Vector2(5f, -5f)));
        foreach (var role in Enum.GetValues<PartyRole>().Where(r => !r.IsTank()))
            Assert.That(M11sOpenerAi.CometSpread(role, Vector2.Zero), Is.EqualTo(M11sOpenerAi.RawSteelSpot(role, axe: true)), $"{role}");
    }

    [TestCaseSource(nameof(BossSteps))]
    public void CometSpreadsStayApartAndInsideTheArenaWhereverTheBossStops(int step)
    {
        var boss = Geometry.WeaponRing * new Vector2(MathF.Sin(step * MathF.PI / 12f), MathF.Cos(step * MathF.PI / 12f));
        var spots = Enum.GetValues<PartyRole>().ToDictionary(r => r, r => M11sOpenerAi.CometSpread(r, boss));
        foreach (var (role, spot) in spots)
        {
            Assert.That(MathF.Max(MathF.Abs(spot.X), MathF.Abs(spot.Y)), Is.LessThan(Geometry.ArenaHalfWidth - 0.5f), $"{role} off the arena");
            foreach (var (other, at) in spots.Where(o => o.Key > role))
                Assert.That(Vector2.Distance(spot, at), Is.GreaterThan(7f), $"{role} and {other} overlap");
        }
        Assert.That(Vector2.Distance(spots[MeleeDpsA], boss), Is.LessThan(8f));
        Assert.That(Vector2.Distance(spots[RegenHealer], boss), Is.GreaterThan(20f));
    }

    private static IEnumerable<int> BossSteps() => Enumerable.Range(0, 24);
}
