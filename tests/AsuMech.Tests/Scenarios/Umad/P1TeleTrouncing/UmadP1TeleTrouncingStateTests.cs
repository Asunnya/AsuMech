using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Umad.P1TeleTrouncing;

namespace AsuMech.Tests;

public class UmadP1TeleTrouncingStateTests
{
    [Test]
    public void FireStackGoesToAHealerAndADps()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var state = new UmadP1TeleTrouncingState(new Rng(seed), new UmadP1TeleTrouncingStateOverrides());
            Assert.That(state.FireStackSupport, Is.AnyOf(PartyRole.RegenHealer, PartyRole.ShieldHealer));
            Assert.That(UmadP1TeleTrouncingState.IsDps(state.FireStackDps), Is.True);
        }
    }
}
