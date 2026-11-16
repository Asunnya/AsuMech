using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.CurtainCall;

namespace AsuMech.Tests;

public class M12sCurtainCallStateTests
{
    [Test]
    public void TheMyDebuffChoiceHoldsForEveryRole()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            foreach (var rotting in new[] { true, false })
                for (var seed = 0; seed < 20; seed++)
                {
                    var state = new M12sCurtainCallState(new Rng(seed), new M12sCurtainCallStateOverrides { PlayerRotting = rotting }, role);
                    Assert.That(state.HasRotting(role), Is.EqualTo(rotting), $"{role} seed {seed}");
                    Assert.That(state.Rotting.Concat(state.Bursting), Is.EquivalentTo(Enum.GetValues<PartyRole>()));
                    Assert.That(state.Rotting.Select(r => r.IsDps()).Distinct().Count(), Is.EqualTo(1));
                }
    }

    [Test]
    public void EachSupportIsChainedToOneDps()
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var state = new M12sCurtainCallState(new Rng(seed));
            Assert.That(M12sCurtainCallState.Supports.Select(s => state.ChainPartner[s]), Is.EquivalentTo(M12sCurtainCallState.Dps));
            foreach (var role in Enum.GetValues<PartyRole>())
            {
                Assert.That(state.ChainPartner[state.ChainPartner[role]], Is.EqualTo(role));
                Assert.That(state.ChainPartner[role].IsDps(), Is.Not.EqualTo(role.IsDps()));
            }
        }
    }

    [Test]
    public void TheBloodVesselsSpareTwoOppositeCorners()
    {
        foreach (var northWest in new[] { true, false })
        {
            var state = new M12sCurtainCallState(new Rng(0), new M12sCurtainCallStateOverrides { NorthWestSafe = northWest });
            foreach (var x in new[] { -1, 1 })
                foreach (var z in new[] { -1, 1 })
                {
                    var corner = new Vector3(x * 19f, 0f, z * 14f);
                    var safe = state.Blobs.All(b => Vector3.Distance(b, corner) > 12f);
                    Assert.That(safe, Is.EqualTo(state.SafeCornerZ(x) == z), $"corner ({x},{z}) northWest {northWest}");
                }
        }
    }

    [Test]
    public void TheDragonCleavesTowardTheMiddle()
    {
        var east = new M12sCurtainCallState(new Rng(0), new M12sCurtainCallStateOverrides { DragonEast = true });
        var west = new M12sCurtainCallState(new Rng(0), new M12sCurtainCallStateOverrides { DragonEast = false });
        Assert.That(east.ConeOrigin, Is.EqualTo(new Vector3(10f, 0f, 0f)));
        Assert.That(west.ConeOrigin, Is.EqualTo(new Vector3(-10f, 0f, 0f)));
    }
}
