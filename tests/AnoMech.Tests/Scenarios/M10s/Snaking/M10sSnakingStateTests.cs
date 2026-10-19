using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M10s.Snaking;

namespace AnoMech.Tests;

public class M10sSnakingStateTests
{
    [Test]
    public void ThePlayerGetsTheElementTheyPicked()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        foreach (var fire in new[] { true, false })
        for (var seed = 0; seed < 20; seed++)
        {
            var state = new M10sSnakingState(new Rng(seed), new M10sSnakingStateOverrides { PlayerFire = fire }, player);
            var byRedHot = M10sSnakingState.StartsByRedHot(player);
            Assert.That(state.FireOnTheNearest == byRedHot, Is.EqualTo(fire), $"{player} fire={fire}");
        }
    }

    [Test]
    public void EachLightPartyHasOneOfEachRole()
    {
        var byRedHot = Enum.GetValues<PartyRole>().Where(M10sSnakingState.StartsByRedHot).ToList();
        Assert.That(byRedHot, Is.EquivalentTo(new[] { PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.MeleeDpsB, PartyRole.CasterDps }));
    }
}
