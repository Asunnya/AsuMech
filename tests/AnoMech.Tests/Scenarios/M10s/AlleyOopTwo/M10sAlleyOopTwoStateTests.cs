using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.M10s.AlleyOopTwo;

namespace AnoMech.Tests;

public class M10sAlleyOopTwoStateTests
{
    [Test]
    public void PyrotationFollowsAHealerUnlessPinned()
    {
        for (var seed = 0; seed < 20; seed++)
            Assert.That(new M10sAlleyOopTwoState(new Rng(seed)).PyrotationTarget, Is.AnyOf(PartyRole.RegenHealer, PartyRole.ShieldHealer));
        var pinned = new M10sAlleyOopTwoState(new Rng(1), new M10sAlleyOopTwoStateOverrides { PyrotationTarget = PartyRole.ShieldHealer });
        Assert.That(pinned.PyrotationTarget, Is.EqualTo(PartyRole.ShieldHealer));
    }
}
