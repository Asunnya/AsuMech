using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M10s.InsaneAir;
using AnoMech.Scenarios.M10s.XtremeSnaking;

namespace AnoMech.Tests;

public class M10sXtremeSnakingStateTests
{
    [Test]
    public void EveryPatternHasTwoSnapsOneReentryAndOneVertical()
    {
        foreach (var moves in M10sXtremeSnakingState.Patterns)
        {
            Assert.That(moves.Count(m => m == SurfMove.Snap), Is.EqualTo(2));
            Assert.That(moves.Count(m => m == SurfMove.Reentry), Is.EqualTo(1));
            Assert.That(moves.Count(m => m == SurfMove.Vertical), Is.EqualTo(1));
        }
    }

    [Test]
    public void ThePlayersElementPicksNearestOrFurthest()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        foreach (var fire in new[] { true, false })
        for (var seed = 0; seed < 10; seed++)
        {
            var state = new M10sXtremeSnakingState(new Rng(seed), new M10sXtremeSnakingStateOverrides { PlayerFire = fire }, player);
            Assert.That(state.FireOnTheNearest, Is.EqualTo(fire == M10sXtremeSnakingState.StartsByRedHot(player)));
        }
    }
}
