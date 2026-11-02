using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn.Tools;
using AnoMech.Tests;

namespace AnoMech.LessonTools;

// Runs a strat headless for every role and seed, recording what it orders the player's slot.
internal static class OrderRecorder
{
    private sealed class Sink(int seed, List<DumpedOrder> into) : IPlayerGuidanceSink
    {
        public void OnMove(GuidanceOrder order) => into.Add(DumpedOrder.Of(order, seed));
    }

    public static OrderDump Record(string scenarioTypeName, int strat, IReadOnlyList<int> seeds, TextWriter log)
    {
        var type = typeof(Game).Assembly.GetTypes().FirstOrDefault(t => t.Name == scenarioTypeName || t.FullName == scenarioTypeName)
                   ?? throw new ArgumentException($"no scenario type named {scenarioTypeName}");
        // The harness expects NUnit's one-time setup to have stood in Dalamud's services.
        new DalamudServices().InstallNullServices();
        var orders = new List<DumpedOrder>();
        string? stratName = null;
        foreach (var seed in seeds)
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var sink = new Sink(seed, orders);
            var run = ScenarioRun.Execute(type, strat, seed, new ScenarioRunOptions
            {
                PlayerRole = role,
                WriteArtifactsOnFailure = false,
                Probe = p =>
                {
                    p.World.Guidance ??= sink;
                    stratName ??= p.Game.ActiveScenario?.AiStrats[strat].Name;
                },
            });
            log.WriteLine($"seed {seed} {role}: {(run.Passed ? "ok" : run.Failure ?? "deaths")}");
        }
        return new OrderDump(type.Name, stratName ?? "?", strat, orders);
    }
}
