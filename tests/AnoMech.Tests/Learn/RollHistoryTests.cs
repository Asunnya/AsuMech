using AnoMech.Learn;
using AnoMech.Learn.Toolbox;
using AnoMech.Scenarios.M11s.Opener;

namespace AnoMech.Tests;

public class RollHistoryTests
{
    private static RollHistory History(out Dictionary<string, List<RollRun>> data)
    {
        data = [];
        return new RollHistory(data, () => { });
    }

    [Test]
    public void TheWorstIsTheValueFailedMostAmongThoseSeenEnough()
    {
        var history = History(out _);
        void Run(string axe, string stack, bool clean) => history.Record("S|strat", new() { ["AxeTrophyFirst"] = axe, ["StackFirst"] = stack, ["Fixed"] = "1" }, clean);
        Run("True", "True", true); Run("True", "False", true); Run("True", "True", false);
        Run("False", "True", false); Run("False", "False", false); Run("False", "True", true);
        Run("False", "False", false);
        var variants = history.Variants("S|strat");
        var axeFalse = variants.Single(v => v.Field == "AxeTrophyFirst" && v.Value == "False");
        Assert.That((axeFalse.Runs, axeFalse.Fails), Is.EqualTo((4, 3)));
        var worst = history.Worst("S|strat");
        Assert.That((worst!.Field, worst.Value), Is.EqualTo(("AxeTrophyFirst", "False")).Or.EqualTo(("StackFirst", "False")));
        Assert.That(worst.FailRate, Is.EqualTo(0.75f).Or.EqualTo(1f));
        Assert.That(variants.Any(v => v.Field == "Fixed"), Is.True);
        Assert.That(history.Worst("S|strat")!.Field, Is.Not.EqualTo("Fixed"), "a roll that never varied has nothing to practise against");
        Assert.That(history.Worst("other"), Is.Null);
        Assert.That(history.Worst("S|strat", minRuns: 50), Is.Null, "not seen often enough");
    }

    [Test]
    public void APinnedRollStaysWhileTheOthersKeepVarying()
    {
        var seen = new List<Dictionary<string, string>>();
        for (var seed = 1; seed <= 8; seed++)
        {
            Dictionary<string, string>? rolls = null;
            var run = ScenarioRun.Execute(typeof(M11sOpenerScenario), 0, seed, new ScenarioRunOptions
            {
                StopAt = 1f,
                Overrides = o => ExampleOverrides.Apply(o, new Dictionary<string, string> { ["AxeTrophyFirst"] = "false" }),
                Probe = p => rolls ??= p.Game.ActiveScenario is { } s ? RollHistory.Pinnable(s) : null,
            });
            Assert.That(run.Failure, Is.Null);
            seen.Add(rolls!);
        }
        Assert.That(seen.Select(r => r["AxeTrophyFirst"]).Distinct(), Is.EqualTo(new[] { "false" }), "pinned in the spelling the history records");
        Assert.That(seen.Select(r => r["CrushingCometTarget"] + r["StackFirst"]).Distinct().Count(), Is.GreaterThan(1), "the other rolls stay random");
        Assert.That(seen[0].Keys, Is.SupersetOf(new[] { "AxeTrophyFirst", "StackFirst", "CrushingCometTarget" }));
        Assert.That(seen[0].Keys, Has.No.Member("HeavyHitterTarget"), "only what the overrides can pin");
    }
}
