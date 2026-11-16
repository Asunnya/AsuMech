using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.M12s.Act1;

namespace AsuMech.Tests;

public class M12sAct1StateTests
{
    [Test]
    public void OneRoleSpreadsTheOtherHoldsTheSharedStackAndTwoOfEachArrowGoOut()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new M12sAct1State(new Rng(seed));
            var spreaders = Enum.GetValues<PartyRole>().Where(state.Spreads).ToList();
            Assert.That(spreaders, Has.Count.EqualTo(4).And.All.Matches<PartyRole>(r => r.IsDps() == state.DpsSpread));
            Assert.That(state.Spreads(state.SharedHolder), Is.False);
            foreach (var arrow in Enum.GetValues<GrotesquerieArrow>())
                Assert.That(state.Arrows.Values.Count(a => a == arrow), Is.EqualTo(2));
            Assert.That(state.StackHealer, Is.EqualTo(PartyRole.RegenHealer).Or.EqualTo(PartyRole.ShieldHealer));
        }
    }

    [Test]
    public void ThePlayersDebuffChoiceHoldsForEveryRole()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            foreach (var debuff in Enum.GetValues<Act1Debuff>())
                for (var seed = 0; seed < 30; seed++)
                {
                    var state = new M12sAct1State(new Rng(seed), new M12sAct1StateOverrides { PlayerDebuff = debuff }, role);
                    Assert.That(state.Spreads(role), Is.EqualTo(debuff == Act1Debuff.Spread), $"{role} {debuff}");
                    Assert.That(state.SharedHolder == role, Is.EqualTo(debuff == Act1Debuff.Shared), $"{role} {debuff}");
                    Assert.That(state.Spreads(state.SharedHolder), Is.False);
                }
    }

    [Test]
    public void ThePlayersArrowChoiceHoldsAndKeepsTwoOfEach()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            foreach (var arrow in Enum.GetValues<GrotesquerieArrow>())
                for (var seed = 0; seed < 20; seed++)
                {
                    var state = new M12sAct1State(new Rng(seed), new M12sAct1StateOverrides { PlayerArrow = arrow }, role);
                    Assert.That(state.Arrows[role], Is.EqualTo(arrow));
                    Assert.That(state.Arrows.Values.Count(a => a == arrow), Is.EqualTo(2));
                }
    }

    [Test]
    public void EachVesselLayoutLeavesBothSafeSpotsOutOfEveryBurst()
    {
        for (var layout = 0; layout < M12sAct1State.VesselLayouts.Length; layout++)
            foreach (var east in new[] { true, false })
            {
                var spot = M12sAct1State.VesselSafeSpot(layout, east);
                Assert.That(spot.X > 0f, Is.EqualTo(east));
                foreach (var vessel in M12sAct1State.VesselLayouts[layout])
                    Assert.That(System.Numerics.Vector3.Distance(spot, vessel), Is.GreaterThan(12.5f), $"layout {layout} east {east}");
            }
    }
}
