using AsuMech.Core.Game.Party;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Fru.P5ParadiseLost;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Tests.NegativeRun;

namespace AsuMech.Tests;

// Paradise Lost starts at 1.5s; Pandora falls at 12.89s, before the enrage under it goes off.
public class FruP5ParadiseLostScenarioTests
{
    [Test]
    public void PandoraFallsBeforeTheEnrage()
    {
        var run = ScenarioRun.Execute(typeof(FruP5ParadiseLostScenario), 0, Random.Shared.Next(), new ScenarioRunOptions { PlayerRole = MeleeDpsA });
        Assert.Multiple(() =>
        {
            Assert.That(run.Passed, run.ToString);
            Assert.That(Rows().Where(r => r["id"] == ActionId.ParadiseLostEnrage.ToString()), Is.Empty);
        });
    }

    [Test]
    public void PlayedOutTheEnrageWipesTheParty()
        => Negative<FruP5ParadiseLostScenario>(MeleeDpsA)
            .Overrides<FruP5ParadiseLostStateOverrides>(o => o.PlayOutEnrage = true)
            .ShouldKill(ActionId.ParadiseLostEnrage, PerRole.All);

    private static IReadOnlyList<Dictionary<string, string>> Rows() => ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows;
}
