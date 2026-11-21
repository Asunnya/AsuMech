using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Tea.P3AlexanderPrime;
using AsuMech.Scenarios.Top.P1ProgramLoop;
using AsuMech.Scenarios.Top.P5Omega;

namespace AsuMech.Tests.Core.Game;

public class FightChainTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    private static RunScenarioParams Params(IScenario scenario, int? strat)
        => new(scenario, new LocalPartyProvider(), strat, 0);

    private static IScenario Of<T>() => Catalog.Scenarios.Single(s => s is T);

    [Test]
    public void AFightStartsAtItsFirstScenarioWhicheverIsPicked()
    {
        var first = FightChain.First(Catalog, Params(Of<TopP5OmegaScenario>(), 0));
        Assert.That(first?.Scenario, Is.InstanceOf<TopP1ProgramLoopScenario>());
        Assert.That(first!.Continued, Is.False);
    }

    [TestCaseSource(nameof(Fights))]
    public void NextWalksTheFightInOrderAndStopsAtItsEnd(IZone zone)
    {
        var fight = Catalog.FightOf(zone);
        var played = new List<IScenario>();
        for (var p = FightChain.First(Catalog, Params(fight[0], 0)); p is not null; p = FightChain.Next(Catalog, p))
        {
            played.Add(p.Scenario);
            Assert.That(p.Continued, Is.EqualTo(played.Count > 1));
            Assert.That(p.SelectedAi, Is.Not.Null);
        }
        Assert.That(played, Is.EqualTo(fight.Where(s => s.AiStrats.Count > 0).ToList()));
    }

    [Test]
    public void SoloSkipsTheScenariosThatNeedAParty()
    {
        foreach (var zone in Catalog.Zones)
        {
            var fight = Catalog.FightOf(zone);
            var played = new List<IScenario>();
            for (var p = FightChain.First(Catalog, Params(fight[0], null)); p is not null; p = FightChain.Next(Catalog, p))
                played.Add(p.Scenario);
            Assert.That(played, Is.EqualTo(fight.Where(s => s.SupportsSolo).ToList()), zone.Name);
        }
    }

    [Test]
    public void TheNextScenarioKeepsTheStratOfTheSameName()
    {
        foreach (var scenario in Catalog.Scenarios)
            for (var i = 0; i < scenario.AiStrats.Count; i++)
                Assert.That(FightChain.StratFor(scenario, scenario.AiStrats[i]), Is.EqualTo(Index(scenario, scenario.AiStrats[i].Name)));
    }

    [Test]
    public void LoopAndRunToEndExcludeEachOther()
    {
        var loop = new PracticeLoop(new AsuMech.Core.Game.Game()) { RunToEnd = true };
        loop.AutoRestart = true;
        Assert.That(loop.RunToEnd, Is.False);
        loop.RunToEnd = true;
        Assert.That(loop.AutoRestart, Is.False);
    }

    [Test]
    public void WholePhaseExcludesLoopAndRunToEnd()
    {
        var loop = new PracticeLoop(new AsuMech.Core.Game.Game()) { RunToEnd = true };
        loop.WholePhase = true;
        Assert.That(loop.RunToEnd, Is.False);
        loop.AutoRestart = true;
        Assert.That(loop.WholePhase, Is.False);
        loop.WholePhase = true;
        loop.RunToEnd = true;
        Assert.That(loop.WholePhase, Is.False);
    }

    [TestCaseSource(nameof(SplitPhases))]
    public void AWholePhasePlaysItsPartsAndStops(IScenario picked)
    {
        var parts = Catalog.PartsOf(picked);
        var started = new List<string>();
        var run = ScenarioRun.Execute(picked.GetType(), 0, 1, new ScenarioRunOptions
        {
            WholePhase = true,
            PlayerRole = PartyRole.MainTank,
            Started = s => started.Add(s.Name),
        });
        Assert.That(run.Passed, run.ToString());
        Assert.That(started, Is.EqualTo(parts.Where(s => s.AiStrats.Count > 0).Select(s => s.Name).ToList()));
    }

    private static IEnumerable<IScenario> SplitPhases()
        => Catalog.Zones.SelectMany(Catalog.PhaseGroupsOf).Where(g => g.Count > 1).Select(g => g.Last(s => s.AiStrats.Count > 0));

    [TestCaseSource(nameof(Fights))]
    public void AWholeFightClearsAsOneRun(IZone zone)
    {
        var fight = Catalog.FightOf(zone);
        var started = new List<string>();
        var run = ScenarioRun.Execute(fight[0].GetType(), 0, 1, new ScenarioRunOptions
        {
            RunToEnd = true,
            PlayerRole = PartyRole.MainTank,
            Started = s => started.Add(s.Name),
        });
        Assert.That(run.Passed, run.ToString());
        Assert.That(started, Is.EqualTo(fight.Where(s => s.AiStrats.Count > 0).Select(s => s.Name).ToList()));
    }

    [Test]
    public void RunToEndPlaysEverySectionedScenarioWhole()
    {
        var started = new List<IScenario>();
        var run = ScenarioRun.Execute(typeof(TeaP3AlexanderPrimeScenario), 0, 1, new ScenarioRunOptions
        {
            RunToEnd = true,
            PlayerRole = PartyRole.MainTank,
            Overrides = o => ((TeaP3AlexanderPrimeStateOverrides)o).Practice = P3Practice.Wormhole,
            Started = started.Add,
            Probe = p => Assert.That(p.Game.ActiveScenario is not ISectionedScenario { SelectedSection: not 0 }, $"{p.Game.ActiveScenario?.Name} not whole"),
        });
        Assert.That(run.Passed, run.ToString());
        Assert.That(started.OfType<ISectionedScenario>().Count(), Is.EqualTo(2));
    }

    private static IEnumerable<IZone> Fights() => Catalog.Zones;

    private static int Index(IScenario scenario, string name)
    {
        for (var i = 0; i < scenario.AiStrats.Count; i++)
            if (scenario.AiStrats[i].Name == name) return i;
        return -1;
    }
}
