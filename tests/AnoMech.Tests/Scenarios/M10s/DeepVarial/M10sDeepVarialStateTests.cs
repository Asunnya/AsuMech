using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M10s.DeepVarial;

namespace AnoMech.Tests;

public class M10sDeepVarialStateTests
{
    [Test]
    public void ThePlayerGetsTheElementTheyPickedAndEachElementIsALightParty()
    {
        foreach (var player in Enum.GetValues<PartyRole>())
        foreach (var fire in new[] { true, false })
        for (var seed = 0; seed < 20; seed++)
        {
            var state = new M10sDeepVarialState(new Rng(seed), new M10sDeepVarialStateOverrides { PlayerFire = fire }, player);
            Assert.That(state.IsFire(player), Is.EqualTo(fire));
            var fireRoles = Enum.GetValues<PartyRole>().Where(state.IsFire).ToList();
            Assert.That(fireRoles, Has.Count.EqualTo(4));
            Assert.That(fireRoles.Count(r => r is PartyRole.MainTank or PartyRole.OffTank), Is.EqualTo(1));
            Assert.That(state.Lane, Is.InRange(-1, 1));
        }
    }
}
