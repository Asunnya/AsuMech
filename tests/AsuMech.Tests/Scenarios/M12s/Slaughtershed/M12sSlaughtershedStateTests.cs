using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.Slaughtershed;

namespace AsuMech.Tests;

public class M12sSlaughtershedStateTests
{
    [Test]
    public void FourOrbsFallOnEachSideWithBothPurplesTogether()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M12sSlaughtershedState(new Rng(seed));
            var all = state.Orbs.SelectMany(p => p).ToList();
            Assert.That(all.Count(o => o.East), Is.EqualTo(4));
            Assert.That(all.Where(o => o.Purple).Select(o => o.East), Is.EqualTo(new[] { state.PurpleEast, state.PurpleEast }));
            foreach (var pair in state.Orbs)
                if (pair[0].East == pair[1].East)
                    Assert.That(pair[0].Purple, Is.EqualTo(pair[1].Purple), $"seed {seed}");
        }
    }

    [Test]
    public void OnlyOneSlaughtershedCleaves()
    {
        for (var seed = 0; seed < 100; seed++)
            Assert.That(new M12sSlaughtershedState(new Rng(seed)).ArmsOf.Count(a => a.Serpentine), Is.EqualTo(1));
        for (var round = 0; round < M12sSlaughtershedState.Rounds; round++)
        {
            var state = new M12sSlaughtershedState(new Rng(0), new M12sSlaughtershedStateOverrides { SerpentineRound = round });
            Assert.That(state.ArmsOf[round].Serpentine, Is.True);
        }
    }

    [Test]
    public void TheMyRoleChoiceHoldsEveryRound()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            foreach (var spreads in new[] { true, false })
                for (var seed = 0; seed < 20; seed++)
                {
                    var state = new M12sSlaughtershedState(new Rng(seed), new M12sSlaughtershedStateOverrides { PlayerSpreads = spreads }, role);
                    for (var r = 0; r < M12sSlaughtershedState.Rounds; r++)
                    {
                        Assert.That(state.Spreads(r, role), Is.EqualTo(spreads), $"{role} seed {seed} round {r}");
                        Assert.That(state.Spreads(r, state.StackTargets[r]), Is.False);
                    }
                }
    }

    [Test]
    public void TheOuterOrbOfAPairOnOneSideFallsFirst()
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var state = new M12sSlaughtershedState(new Rng(seed));
            for (var pair = 0; pair < M12sSlaughtershedState.Pairs; pair++)
                foreach (var east in new[] { false, true })
                {
                    var falling = state.FallingOn(pair, east).Select(i => MathF.Abs(state.OrbSpot(pair, i).X)).ToList();
                    Assert.That(falling, Is.Ordered.Descending);
                }
        }
    }
}
