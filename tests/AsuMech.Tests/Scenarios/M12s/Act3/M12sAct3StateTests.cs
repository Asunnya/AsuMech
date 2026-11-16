using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.Act3;

namespace AsuMech.Tests;

public class M12sAct3StateTests
{
    [Test]
    public void EachGrowthPointsOneWayForOneSupportAndOneDps()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M12sAct3State(new Rng(seed));
            foreach (var growth in Enum.GetValues<Growth>())
            {
                var holders = state.Growths.Where(kv => kv.Value == growth).Select(kv => kv.Key).ToList();
                Assert.That(holders.Count(r => r.IsDps()), Is.EqualTo(1));
                Assert.That(holders.Count(r => !r.IsDps()), Is.EqualTo(1));
            }
        }
    }

    [Test]
    public void ThePlayersGrowthPointsTheWayTheyPicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        foreach (var growth in Enum.GetValues<Growth>())
        {
            var state = new M12sAct3State(new Rng(3), new M12sAct3StateOverrides { PlayerGrowth = (int)growth }, player);
            Assert.That(state.Growths[player], Is.EqualTo(growth));
        }
    }

    // Each spot's tower lands on the spot of the next growth round, so whoever stands there soaks it,
    // and the two on a platform stand far enough apart for their bursts.
    [Test]
    public void EverySpotThrowsItsTowerOntoAnotherAndStaysClearOfItsPlatformMate()
    {
        foreach (var cardinal in new[] { true, false })
        {
            var state = new M12sAct3State(new Rng(0), new M12sAct3StateOverrides { CardinalPlatforms = cardinal });
            foreach (var dps in new[] { true, false })
            {
                var spots = Enum.GetValues<Growth>().Select(g => M12sAct3Ai.PlatformSpot(g, dps, cardinal)).ToList();
                foreach (var growth in Enum.GetValues<Growth>())
                {
                    var spot = M12sAct3Ai.PlatformSpot(growth, dps, cardinal);
                    Assert.That(M12sAct3State.OnSquare(state.Platforms, spot), Is.True, $"{growth} {dps} {cardinal}");
                    var tower = M12sAct3State.Projected(spot, growth);
                    Assert.That(spots.Any(s => s != spot && Vector2.Distance(s, tower) < 0.5f), Is.True, $"{growth} {dps} {cardinal}");
                    var mate = M12sAct3Ai.PlatformSpot(growth, !dps, cardinal);
                    Assert.That(Vector2.Distance(spot, mate), Is.GreaterThan(9.5f));
                }
            }
        }
    }
}
