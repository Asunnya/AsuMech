using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.Act2;

namespace AsuMech.Tests;

public class M12sAct2StateTests
{
    [Test]
    public void EachOrderChainsOneDpsAlphaToOneSupportBeta()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M12sAct2State(new Rng(seed));
            for (var order = 1; order <= M12sAct2State.Pairs; order++)
            {
                Assert.That(state.Alpha(order).IsDps(), Is.True);
                Assert.That(state.Beta(order).IsDps(), Is.False);
            }
            Assert.That(state.Order.Values.GroupBy(o => o).All(g => g.Count() == 2), Is.True);
        }
    }

    [Test]
    public void ThePlayerGetsTheOrderTheyPicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        for (var order = 1; order <= M12sAct2State.Pairs; order++)
        {
            var state = new M12sAct2State(new Rng(11), new M12sAct2StateOverrides { PlayerOrder = order }, player);
            Assert.That(state.Order[player], Is.EqualTo(order));
            Assert.That(state.Order.Count, Is.EqualTo(8));
        }
    }

    [Test]
    public void TheBossTurnsCounterClockwiseAndTheAlphaLeavesOppositeItsBeta()
    {
        var state = new M12sAct2State(new Rng(0), new M12sAct2StateOverrides { FirstFacing = 0 });
        Assert.That(Vector2.Distance(state.BetaDirection(1), new Vector2(0f, -1f)), Is.LessThan(0.001f));
        Assert.That(Vector2.Distance(state.BetaDirection(2), new Vector2(-1f, 0f)), Is.LessThan(0.001f));
        Assert.That(Vector2.Distance(state.AlphaDirection(1), new Vector2(0f, 1f)), Is.LessThan(0.001f));
        for (var order = 1; order <= M12sAct2State.Pairs; order++)
            Assert.That(Vector2.Distance(state.AlphaDirection(order), -state.BetaDirection(order)), Is.LessThan(0.001f));
    }

    [Test]
    public void TwoBlobsSitInsideTheCoilAndTheOuterOnesMirrorTheFirst()
    {
        var state = new M12sAct2State(new Rng(0), new M12sAct2StateOverrides { FirstBlob = 2 });
        Assert.That(state.BlobSpot(1), Is.EqualTo(new Vector2(-4f, 4f)));
        Assert.That(state.BlobSpot(2), Is.EqualTo(new Vector2(4f, -4f)));
        Assert.That(state.BlobSpot(3), Is.EqualTo(new Vector2(15f, 10f)));
        Assert.That(state.BlobSpot(4), Is.EqualTo(new Vector2(-15f, -10f)));
    }

    [Test]
    public void TowersGoToTheOrderTwoAhead()
        => Assert.That(Enumerable.Range(1, 4).Select(M12sAct2State.SoakerOrder), Is.EqualTo(new[] { 3, 4, 1, 2 }));
}
