using AsuMech.Core.Game.Party;
using AsuMech.Core.Game.Trace;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Scenarios;

namespace AsuMech.Tests;

// A Whole phase run on one clock: each part's own clock restarts at 0, so rows and part bounds are
// stamped with the time since the run began instead. Music plays, so its restarts show.
internal sealed class WholePhaseTrace
{
    public sealed record Part(string Name, float StartedAt, float EndedAt);

    public sealed record Row(float At, string Part, IReadOnlyDictionary<string, string> Fields)
    {
        public string Kind => Fields["kind"];
        public string Id => Fields["id"];
        public string Actor => Fields["actor_name"];
        public override string ToString() => $"{At,8:F2} [{Part}] {Kind} {Actor} {Fields["name"]} ({Id})";
    }

    public ScenarioRun Run { get; private init; } = null!;
    public IReadOnlyList<Part> Parts { get; private init; } = [];
    public IReadOnlyList<Row> Rows { get; private init; } = [];

    public static WholePhaseTrace Execute(Type scenario, int seed, PartyRole player = PartyRole.MainTank, int strat = 0)
    {
        var parts = new List<(string Name, float Start, float End)>();
        var rows = new List<Row>();
        var wall = 0f;
        var read = 0;
        var run = ScenarioRun.Execute(scenario, strat, seed, new ScenarioRunOptions
        {
            WholePhase = true,
            PlayerRole = player,
            Configure = c => c.SuppressBgm = false,
            Started = s => parts.Add((s.Name, wall, wall)),
            Probe = probe =>
            {
                wall += ScenarioRun.FrameSeconds;
                if (parts.Count == 0) return;
                var timeline = ((FakeBattleCharas)Natives.BattleCharas).Timeline;
                for (; read < timeline.Rows.Count; read++)
                    rows.Add(new Row(wall, parts[^1].Name, timeline.Rows[read]));
                parts[^1] = (parts[^1].Name, parts[^1].Start, wall);
            },
        });
        return new WholePhaseTrace
        {
            Run = run,
            Parts = parts.Select(p => new Part(p.Name, p.Start, p.End)).ToList(),
            Rows = rows,
        };
    }

    // When the first row matching `match` landed in `part`.
    public float First(string part, Func<Row, bool> match)
        => Rows.First(r => r.Part == part && match(r)).At;

    public float Last(string part, Func<Row, bool> match)
        => Rows.Last(r => r.Part == part && match(r)).At;

    public static Func<Row, bool> Cast(uint actionId) => r => r.Kind == "cast" && r.Id == actionId.ToString();

    public static Func<Row, bool> Ability(uint actionId) => r => r.Kind == "ability" && r.Id == actionId.ToString();

    public string Describe(Func<Row, bool>? filter = null)
        => string.Join(Environment.NewLine,
            Parts.Select(p => $"part {p.Name}: {p.StartedAt:F2} - {p.EndedAt:F2}")
                .Concat(Rows.Where(filter ?? (r => r.Kind is "cast" or "ability")).Select(r => r.ToString())));
}
