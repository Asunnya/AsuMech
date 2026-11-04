using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn.Tools;
using AnoMech.Learn.Toolbox;
using AnoMech.Tests;

namespace AnoMech.LessonTools;

// Runs a strat headless for every role and seed, recording what it orders the player's slot, and
// once per seed what the scenario rolled and what the sim showed.
internal static class OrderRecorder
{
    private const int SnapshotEveryFrames = 120;

    private sealed class Sink(int seed, List<DumpedOrder> into) : IPlayerGuidanceSink
    {
        public void OnMove(GuidanceOrder order) => into.Add(DumpedOrder.Of(order, seed));
    }

    // `example` pins rolls on the scenario's overrides for every run (a lesson's toolbox example),
    // so the dump shows the roll the plan draws; mining wants it null.
    public static OrderDump Record(string scenarioTypeName, int strat, IReadOnlyList<int> seeds, TextWriter log, IReadOnlyDictionary<string, string>? example = null)
    {
        var type = typeof(Game).Assembly.GetTypes().FirstOrDefault(t => t.Name == scenarioTypeName || t.FullName == scenarioTypeName)
                   ?? throw new ArgumentException($"no scenario type named {scenarioTypeName}");
        // The harness expects NUnit's one-time setup to have stood in Dalamud's services.
        new DalamudServices().InstallNullServices();
        var data = new DataminingGameData(new FakeRsvFunctions());
        var orders = new List<DumpedOrder>();
        var runs = new List<DumpedRun>();
        string? stratName = null;
        foreach (var seed in seeds)
        {
            var first = true;
            foreach (var role in Enum.GetValues<PartyRole>())
            {
                var sink = new Sink(seed, orders);
                var state = new Dictionary<string, string>();
                var seats = new Dictionary<string, string>();
                var origin = System.Numerics.Vector3.Zero;
                var frames = 0;
                var csv = first ? Path.GetTempFileName() : null;
                var run = ScenarioRun.Execute(type, strat, seed, new ScenarioRunOptions
                {
                    PlayerRole = role,
                    WriteArtifactsOnFailure = false,
                    TimelinePath = csv,
                    Overrides = example is null ? null : o => ExampleOverrides.Apply(o, example),
                    Probe = p =>
                    {
                        p.World.Guidance ??= sink;
                        stratName ??= p.Game.ActiveScenario?.AiStrats[strat].Name;
                        if (!first) return;
                        // Rolls happen at start; a few are filled in mid-run, so keep sampling.
                        if (frames++ % SnapshotEveryFrames == 0 && p.Game.ActiveScenario is { } scenario)
                        {
                            state = StateSnapshot.Of(scenario);
                            origin = scenario.Phase.Zone.Origin;
                        }
                        foreach (var seat in Enum.GetValues<PartyRole>())
                            if (p.Member(seat) is { } member && member.GameObjectId.ObjectId is var id and not 0)
                                seats[id.ToString("X8")] = seat.ToString();
                    },
                });
                log.WriteLine($"seed {seed} {role}: {(run.Passed ? "ok" : run.Failure ?? "deaths")}");
                if (csv != null)
                {
                    var events = File.Exists(csv) ? TimelineEvents.Parse(File.ReadAllText(csv), seats, id => data.Status(id)?.Name, origin) : [];
                    File.Delete(csv);
                    runs.Add(new DumpedRun(seed, state, events));
                    first = false;
                }
            }
        }
        return new OrderDump(type.Name, stratName ?? "?", strat, orders, runs);
    }
}
