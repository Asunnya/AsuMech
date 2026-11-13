using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Learn;
using AsuMech.Scenarios;
using NUnit.Framework;

namespace AsuMech.Tests.Learn;

// The lesson judged against a player who does exactly what the strat says: the bot in the player's
// seat. A step it fails is a tolerance or deadline a human can't meet either.
public class LessonWalkthroughTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    public static IEnumerable<TestCaseData> Cases() => LessonLoaderTests.EmbeddedLessonRoles();

    [TestCaseSource(nameof(Cases))]
    public void TheStratsOwnBotPassesEveryStep(string scenarioName, string strat, PartyRole role, int seed)
    {
        var scenario = Catalog.Scenarios.Single(s => s.GetType().Name == scenarioName);
        var stratIndex = scenario.AiStrats.ToList().FindIndex(a => a.Name == strat);
        var lesson = LessonLoader.For(scenario, scenario.AiStrats[stratIndex])!;
        var director = new LessonDirector(lesson, role, LearnLevel.Exam, _ => { });
        var run = ScenarioRun.Execute(scenario.GetType(), stratIndex, seed, new ScenarioRunOptions
        {
            PlayerRole = role,
            Probe = p =>
            {
                p.World.Guidance ??= director;
                if (p.World.Party.Player is { } player) director.Tick(p.Time, player.Position, player.SpeedFactor());
            },
        });
        Assert.That(run.Failure, Is.Null, run.ToString);
        director.End();
        // A step still open when the run ended was never due.
        var failed = director.Results
            .Where(r => r.Result != StepResult.OnTime && !(r.Result == StepResult.WrongPlace && r.Step.Evaluation.Deadline > run.Elapsed))
            .Select(r => $"{r.Step.Title} (@{r.Step.Order.Key.Time:0.##}, {r.Step.Order.Key.Source}): {r.Result}, {r.Step.Evaluation.ResolvedDistance:F1} m from the spot, tolerance {r.Step.Evaluation.Tolerance:F1} m, arrived {(r.Step.Evaluation.ArrivedAt is { } at ? $"{at:F1}s" : "never")}, due {r.Step.Evaluation.Deadline:F1}s")
            .ToList();
        Assert.That(director.Results, Is.Not.Empty);
        Assert.That(failed, Is.Empty, string.Join("\n", failed));
    }
}
