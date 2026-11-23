using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Uwu.P3Titan;
using static AsuMech.Scenarios.Uwu.UwuConstants;

namespace AsuMech.Tests;

public class UwuP3TitanGaolsScenarioTests
{
    [Test]
    public void TheDrillPlaysTheThreeGaolsAndStopsAtTheAwakening([Values] PartyRole player)
    {
        var firstTime = float.MaxValue;
        var jailed = 0;
        var run = ScenarioRun.Execute(typeof(UwuP3TitanGaolsScenario), 0, (int)player + 1, new ScenarioRunOptions
        {
            PlayerRole = player,
            Probe = p =>
            {
                firstTime = System.Math.Min(firstTime, p.World.Events.Elapsed);
                if (p.Crossed(52f)) jailed = p.World.Party.ActiveMembers().Count(m => m.HasStatus(StatusId.Fetters));
            },
        });
        Assert.Multiple(() =>
        {
            Assert.That(run.Passed, run.ToString);
            Assert.That(firstTime, Is.GreaterThanOrEqualTo(UwuP3TitanScenario.GaolsFrom));
            Assert.That(jailed, Is.EqualTo(3));
            Assert.That(Casts(), Is.Not.Empty);
            Assert.That(Casts().Where(t => t > UwuP3TitanScenario.GaolsTo), Is.Empty);
            Assert.That(Casts().Where(t => t < UwuP3TitanScenario.GaolsFrom), Is.Empty);
        });
    }

    private static IEnumerable<float> Casts()
        => ((FakeBattleCharas)AsuMech.Core.Native.Interfaces.Natives.BattleCharas).Timeline.Rows
            .Where(r => r["kind"] == "cast").Select(r => float.Parse(r["t"], System.Globalization.CultureInfo.InvariantCulture));

    [Test]
    public void TheDrillIsLeftOutOfTheWholePhase()
        => Assert.That(((IScenario)new UwuP3TitanGaolsScenario()).InWholeRun, Is.False);
}
