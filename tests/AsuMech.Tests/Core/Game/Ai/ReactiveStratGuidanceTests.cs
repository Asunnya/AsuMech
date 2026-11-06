using AsuMech.Core.Game.Ai;
using AsuMech.Scenarios.Fru.P4CrystallizeTime;
using AsuMech.Scenarios.Ucob.P5Exaflares;
using AsuMech.Scenarios.Umad.P5Celestriad;
using AsuMech.Scenarios.Umad.P5Exaflares;

namespace AsuMech.Tests.Core.Game.Ai;

public class ReactiveStratGuidanceTests
{
    private sealed class Collect : IPlayerGuidanceSink
    {
        public readonly List<GuidanceOrder> Orders = [];
        public void OnMove(GuidanceOrder order) => Orders.Add(order);
    }

    // Strats that place each seat as the fight unfolds still tell the player's seat where to go,
    // keyed like a timeline order, so a lesson can guide it.
    [TestCase(typeof(UmadP5CelestriadScenario), "UmadP5CelestriadAi")]
    [TestCase(typeof(UmadP5ExaflaresScenario), "UmadP5ExaflaresAi")]
    [TestCase(typeof(FruP4CrystallizeTimeScenario), "FruP4CrystallizeTimeAi")]
    [TestCase(typeof(UcobP5ExaflaresScenario), "UcobP5ExaflaresAi")]
    public void ThePlayerSeatIsGuided(Type scenario, string stratClass)
    {
        var sink = new Collect();
        var run = ScenarioRun.Execute(scenario, 0, 1, new ScenarioRunOptions
        {
            WriteArtifactsOnFailure = false,
            Probe = p => p.World.Guidance ??= sink,
        });
        Assert.That(run.Passed, run.Failure);
        Assert.That(sink.Orders, Is.Not.Empty);
        Assert.That(sink.Orders.Select(o => o.Key.StratClass).Distinct(), Is.EqualTo(new[] { stratClass }));
        Assert.That(sink.Orders.Select(o => o.Key.Source).Distinct().Count(), Is.GreaterThanOrEqualTo(2), "each phase from its own line");
    }
}
