using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Learn;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AsuMech.Tests.Learn;

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

    [Test]
    public void TableRulesAreOnlyAllowedInADraft()
    {
        const string steps = "\"steps\": [ { \"key\": { \"cls\": \"UwuP1GarudaAi\", \"time\": 1 }, \"rule\": \"Depends on NailBearing: 270 → N wall; 90 → S wall.\" } ]";
        var reviewed = LessonLoader.Parse("{ \"scenario\": \"UwuP1GarudaScenario\", \"strat\": \"Zeal / The Balance\", \"sources\": [], " + steps + " }");
        Assert.That(LessonLoader.Validate(reviewed, Catalog.Scenarios), Has.Some.Contains("still a table"));
        var draft = LessonLoader.Parse("{ \"scenario\": \"UwuP1GarudaScenario\", \"strat\": \"Zeal / The Balance\", \"sources\": [], \"draft\": true, " + steps + " }");
        Assert.That(draft.Draft, Is.True);
        Assert.That(LessonLoader.Validate(draft, Catalog.Scenarios), Is.Empty);
    }

    [Test]
    public void TheGlossaryIsNotALesson()
    {
        Assert.That(LessonLoader.All.Select(l => l.Scenario), Has.None.Empty);
        Assert.That(Glossary.Load("Uwu").Fields, Is.Not.Empty);
        Assert.That(Glossary.Load("NoSuchFamily").Fields, Is.Empty);
    }

    [Test]
    public void ADevFolderShadowsEmbeddedLessonsAndReloadsOnChange()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "asumech-lessons-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "Uwu"));
        var scenario = Catalog.Scenarios.OfType<UwuP1GarudaScenario>().Single();
        var embedded = LessonLoader.For(scenario, scenario.AiStrats[0])!;
        var embeddedCount = LessonLoader.All.Count;
        try
        {
            var file = System.IO.Path.Combine(dir, "Uwu", "garuda.json");
            System.IO.File.WriteAllText(file, """{ "scenario": "UwuP1GarudaScenario", "strat": "Zeal / The Balance", "sources": [], "draft": true, "steps": [ { "key": { "cls": "UwuP1GarudaAi", "time": 0.5 }, "rule": "from the file" } ] }""");
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Uwu", "glossary.json"), """{ "fields": {} }""");
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Uwu", "broken.json"), "{ not json");
            LessonLoader.DevDirectory = dir;
            Assert.That(LessonLoader.DevCount, Is.EqualTo(1));
            Assert.That(LessonLoader.DevErrors, Has.Count.EqualTo(1).And.Some.Contains("broken.json"));
            var shadowed = LessonLoader.For(scenario, scenario.AiStrats[0])!;
            Assert.That(shadowed.Steps.Single().Rule, Is.EqualTo("from the file"));
            Assert.That(LessonLoader.All.Count(l => l.Scenario == embedded.Scenario && l.Strat == embedded.Strat), Is.EqualTo(1));
            Assert.That(LessonLoader.All.Count, Is.EqualTo(embeddedCount));

            System.IO.File.WriteAllText(file, System.IO.File.ReadAllText(file).Replace("from the file", "edited"));
            System.IO.File.SetLastWriteTimeUtc(file, System.DateTime.UtcNow.AddSeconds(5));
            Assert.That(LessonLoader.Refresh(force: true), Is.True);
            Assert.That(LessonLoader.For(scenario, scenario.AiStrats[0])!.Steps.Single().Rule, Is.EqualTo("edited"));
            Assert.That(LessonLoader.Refresh(force: true), Is.False);
        }
        finally
        {
            LessonLoader.DevDirectory = null;
            System.IO.Directory.Delete(dir, true);
        }
        Assert.That(LessonLoader.For(scenario, scenario.AiStrats[0]), Is.SameAs(embedded));
    }

    private sealed class Recorder : IPlayerGuidanceSink
    {
        public readonly List<GuidanceOrder> Orders = [];
        public void OnMove(GuidanceOrder order) => Orders.Add(order);
    }

    public static IEnumerable<TestCaseData> EmbeddedLessonRoles()
    {
        foreach (var lesson in LessonLoader.All)
        foreach (var (role, seed) in new[] { (PartyRole.MainTank, 1), (PartyRole.ShieldHealer, 2), (PartyRole.MeleeDpsB, 3), (PartyRole.CasterDps, 4) })
            yield return new TestCaseData(lesson.Scenario, lesson.Strat, role, seed).SetName($"{{m}}({lesson.Scenario},{role})");
    }

    // Every order the strat gives the player has a step, and every step is triggered, per role.
    [TestCaseSource(nameof(EmbeddedLessonRoles))]
    public void EmbeddedLessonCoversEveryOrder(string scenarioName, string strat, PartyRole role, int seed)
    {
        var scenario = Catalog.Scenarios.Single(s => s.GetType().Name == scenarioName);
        var stratIndex = scenario.AiStrats.ToList().FindIndex(a => a.Name == strat);
        var lesson = LessonLoader.For(scenario, scenario.AiStrats[stratIndex])!;
        var recorder = new Recorder();
        var run = ScenarioRun.Execute(scenario.GetType(), stratIndex, seed, new ScenarioRunOptions
        {
            PlayerRole = role,
            Probe = p => p.World.Guidance ??= recorder,
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        Assert.That(recorder.Orders, Is.Not.Empty);
        var uncovered = recorder.Orders.Where(o => lesson.StepFor(o.Key) is null).Select(o => $"{o.Key.Time} ({o.Key.Source})").Distinct().ToList();
        Assert.That(uncovered, Is.Empty);
    }

    [Test]
    public void EveryLessonStepIsTriggeredBySomeRole()
    {
        foreach (var lesson in LessonLoader.All)
        {
            var scenario = Catalog.Scenarios.Single(s => s.GetType().Name == lesson.Scenario);
            var stratIndex = scenario.AiStrats.ToList().FindIndex(a => a.Name == lesson.Strat);
            var recorder = new Recorder();
            List<string> Untriggered() => lesson.Steps.Where(s => !s.Skip && !recorder.Orders.Any(o => s.Key.Matches(o.Key))).Select(s => s.Title ?? MasteryStore.StepId(s.Key)).ToList();
            // A step a roll decides may need more seeds than one to show up for some role.
            foreach (var seed in new[] { 1, 2, 3, 4, 5, 6, 7, 8 })
            {
                foreach (var role in System.Enum.GetValues<PartyRole>())
                    ScenarioRun.Execute(scenario.GetType(), stratIndex, seed, new ScenarioRunOptions { PlayerRole = role, Probe = p => p.World.Guidance ??= recorder });
                if (Untriggered().Count == 0) break;
            }
            Assert.That(Untriggered(), Is.Empty, $"{lesson.Scenario} / {lesson.Strat}");
        }
    }
}
