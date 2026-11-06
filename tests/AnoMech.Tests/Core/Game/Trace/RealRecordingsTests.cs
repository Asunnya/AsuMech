using AnoMech.Scenarios;

namespace AnoMech.Tests;

// Every recorded pull under $ANOMECH_REPLAYS (or the repo's datalogs/replays) against every
// scenario of its territory whose casts it holds: no scenario cast may be missing, and none may
// drift past the slack. Runs on the machine that has the recordings, not in CI.
public class RealRecordingsTests
{
    private static string? ReplaysDir()
    {
        var env = Environment.GetEnvironmentVariable("ANOMECH_REPLAYS");
        if (!string.IsNullOrEmpty(env)) return Directory.Exists(env) ? env : null;
        var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "datalogs", "replays"));
        return Directory.Exists(repo) ? repo : null;
    }

    public static IEnumerable<TestCaseData> Cases()
    {
        if (ReplaysDir() is not { } dir) yield break;
        var catalog = ScenarioCatalog.Create();
        foreach (var file in Directory.EnumerateFiles(dir).Where(f => f.EndsWith(".csv") || f.EndsWith(".csv.gz")).OrderBy(f => f))
        {
            var rows = Recording.Load(file);
            if (Recording.Territory(rows) is not { } territory) continue;
            var pulls = Recording.Pulls(rows);
            for (var pull = 0; pull < pulls.Count; pull++)
            {
                var actions = pulls[pull].Select(CastRow.From).OfType<CastRow>().Where(c => c.IsEnemy).Select(c => c.ActionId).ToHashSet();
                foreach (var scenario in catalog.Scenarios.Where(s => s.Phase.Zone.TerritoryId == territory && s.AiStrats.Count > 0))
                {
                    var sim = CastTimelines.SimRows(scenario.GetType(), 0, 1, out _);
                    var first = sim.Select(CastRow.From).OfType<CastRow>().MinBy(c => c.T);
                    if (first is null || !actions.Contains(first.ActionId)) continue;
                    yield return new TestCaseData(file, pull + 1, scenario.GetType()).SetName($"{Path.GetFileName(file)} pull {pull + 1} vs {scenario.GetType().Name}");
                }
            }
        }
    }

    [Test]
    public void NoRecordingsHereIsASkip()
    {
        if (ReplaysDir() is null) Assert.Ignore("no datalogs/replays and no ANOMECH_REPLAYS");
        Assert.Pass();
    }

    [TestCaseSource(nameof(Cases))]
    public void RecordedPullMatchesTheScenario(string file, int pull, Type scenarioType)
    {
        var real = Recording.Pulls(Recording.Load(file))[pull - 1];
        var sim = CastTimelines.SimRows(scenarioType, 0, 1, out var failure);
        Assert.That(failure, Is.Null);
        var diff = ReplayDiff.Compare(sim, real);
        TestContext.Out.WriteLine(diff.Report());
        Assert.That(diff.SimOnly, Is.Empty, "scenario casts the recording never had");
        Assert.That(diff.Matched.Where(m => Math.Abs(m.Dt) > ReplayDiff.DefaultSlack).Select(m => $"{m.Sim.Name} {m.Dt:+0.00}"), Is.Empty);
    }
}
