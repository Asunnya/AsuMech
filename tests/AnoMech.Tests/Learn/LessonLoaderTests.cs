using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AnoMech.Tests.Learn;

public class LessonLoaderTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    [Test]
    public void EveryEmbeddedLessonIsValid()
    {
        Assert.That(LessonLoader.All, Is.Not.Empty);
        foreach (var lesson in LessonLoader.All)
            Assert.That(LessonLoader.Validate(lesson, Catalog.Scenarios), Is.Empty, $"{lesson.Scenario} / {lesson.Strat}");
    }

    [Test]
    public void GarudaLessonLoadsForItsStrat()
    {
        var scenario = Catalog.Scenarios.OfType<UwuP1GarudaScenario>().Single();
        var lesson = LessonLoader.For(scenario, scenario.AiStrats[0]);
        Assert.That(lesson, Is.Not.Null);
        Assert.That(lesson!.Sources, Is.Not.Empty);
    }

    [Test]
    public void LessonForAMissingStratIsRefused()
    {
        var lesson = LessonLoader.Parse("""{ "scenario": "UwuP1GarudaScenario", "strat": "Nope", "sources": [], "steps": [] }""");
        Assert.That(LessonLoader.Validate(lesson, Catalog.Scenarios), Has.Some.Contains("no strat named"));
        var unknown = LessonLoader.Parse("""{ "scenario": "Nope", "strat": "x", "sources": [], "steps": [] }""");
        Assert.That(LessonLoader.Validate(unknown, Catalog.Scenarios), Has.Some.Contains("no scenario"));
    }

    [Test]
    public void StepsWithoutRuleOrSkipAreRefused()
    {
        var lesson = LessonLoader.Parse("""{ "scenario": "UwuP1GarudaScenario", "strat": "Zeal / The Balance", "sources": [], "steps": [ { "key": { "cls": "UwuP1GarudaAi", "time": 1 } }, { "key": { "cls": "UwuP1GarudaAi" }, "skip": true } ] }""");
        var errors = LessonLoader.Validate(lesson, Catalog.Scenarios);
        Assert.That(errors, Has.Some.Contains("needs a rule"));
        Assert.That(errors, Has.Some.Contains("needs a time"));
    }

    private sealed class Recorder : IPlayerGuidanceSink
    {
        public readonly List<GuidanceOrder> Orders = [];
        public void OnMove(GuidanceOrder order) => Orders.Add(order);
    }

    // Every order the strat gives the player has a step, for every role, over a few seeds.
    [TestCase(PartyRole.MainTank, 1)]
    [TestCase(PartyRole.OffTank, 2)]
    [TestCase(PartyRole.MeleeDpsB, 3)]
    [TestCase(PartyRole.CasterDps, 4)]
    public void GarudaLessonCoversEveryOrder(PartyRole role, int seed)
    {
        var scenario = Catalog.Scenarios.OfType<UwuP1GarudaScenario>().Single();
        var lesson = LessonLoader.For(scenario, scenario.AiStrats[0])!;
        var recorder = new Recorder();
        var run = ScenarioRun.Execute(typeof(UwuP1GarudaScenario), 0, seed, new ScenarioRunOptions
        {
            PlayerRole = role,
            Probe = p => p.World.Guidance ??= recorder,
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(recorder.Orders, Is.Not.Empty);
        var uncovered = recorder.Orders.Where(o => lesson.StepFor(o.Key) is null).Select(o => $"{o.Key.Time} ({o.Key.Source})").Distinct().ToList();
        Assert.That(uncovered, Is.Empty);
        Assert.That(lesson.Steps.Where(s => !s.Skip && !recorder.Orders.Any(o => s.Key.Matches(o.Key))).Select(s => s.Title), Is.Empty, "steps nothing triggers");
    }
}
