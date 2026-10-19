using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M10s.ArenaSplit;

namespace AnoMech.Tests;

public class M10sArenaSplitStateTests
{
    [Test]
    public void PyrotationMarksOnePlayerOfEachPairAndHonoursThePlayersPick()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        foreach (var marked in new bool?[] { null, true, false })
        for (var seed = 0; seed < 20; seed++)
        {
            var state = new M10sArenaSplitState(new Rng(seed), new M10sArenaSplitStateOverrides { PlayerPyrotation = marked }, player);
            Assert.That(state.PyrotationTargets, Has.Count.EqualTo(4));
            foreach (var (first, second) in M10sArenaSplitState.Pairs)
                Assert.That(state.PyrotationTargets.Contains(first), Is.Not.EqualTo(state.PyrotationTargets.Contains(second)));
            if (marked is { } m)
            {
                Assert.That(state.PyrotationTargets.Contains(player), Is.EqualTo(m));
                Assert.That(state.PyrotationTargets.Contains(M10sArenaSplitState.Partner(player)), Is.EqualTo(!m));
            }
        }
    }

    [Test]
    public void TheNorthWaveTakesTheMiddleLaneAndASideWaveASideLane()
    {
        foreach (var wall in M10sArenaSplitState.WaveWalls)
        for (var seed = 0; seed < 20; seed++)
        {
            var state = new M10sArenaSplitState(new Rng(seed), new M10sArenaSplitStateOverrides { WaveFrom = wall });
            Assert.That(state.WaveFrom, Is.EqualTo(wall));
            if (wall == 0) Assert.That(state.Lane, Is.EqualTo(0));
            else Assert.That(Math.Abs(state.Lane), Is.EqualTo(1));
        }
    }

    [Test]
    public void EveryPairStaysInOneLightParty()
    {
        foreach (var (first, second) in M10sArenaSplitState.Pairs)
            Assert.That(M10sArenaSplitState.GroupSide(first), Is.EqualTo(M10sArenaSplitState.GroupSide(second)));
    }
}
