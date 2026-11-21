using System.Globalization;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;

namespace AsuMech.Tests;

// One row of a Whole phase run's timeline, on one clock across all its parts.
internal sealed record WholePhaseRow(int Part, float At, string Kind, uint Id, string Actor, IReadOnlyDictionary<string, string> Raw)
{
    public float? X => float.TryParse(Raw.GetValueOrDefault("x"), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    public float? Z => float.TryParse(Raw.GetValueOrDefault("z"), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
}

// Plays a phase's parts back to back, as the Whole phase tab does, and puts every row the fakes
// saw on one clock: each part's scenario time plus when that part started.
internal sealed class WholePhaseTimeline
{
    public ScenarioRun Run { get; private init; } = null!;
    public IReadOnlyList<string> Parts { get; private init; } = [];
    // Where each part's scenario t=0 falls, and when the part actually began (they differ after a
    // Delay or Advance).
    public IReadOnlyList<float> PartStarts { get; private init; } = [];
    public IReadOnlyList<float> PartBegan { get; private init; } = [];
    public IReadOnlyList<WholePhaseRow> Rows { get; private init; } = [];
    // Each part's party positions (scenario-local XZ) on its first frame, by role.
    public IReadOnlyList<IReadOnlyDictionary<PartyRole, System.Numerics.Vector3>> PartyAtStart { get; private init; } = [];

    public static WholePhaseTimeline Record(Type picked, int seed = 1, PartyRole player = PartyRole.MainTank)
    {
        new DalamudServices().InstallNullServices();
        var clock = 0f;
        var lastTime = float.MaxValue;
        var starts = new List<float>();
        var began = new List<float>();
        var names = new List<string>();
        var parties = new List<IReadOnlyDictionary<PartyRole, System.Numerics.Vector3>>();
        IReadOnlyList<Dictionary<string, string>> raw = [];
        var run = ScenarioRun.Execute(picked, 0, seed, new ScenarioRunOptions
        {
            WholePhase = true,
            PlayerRole = player,
            Started = s => names.Add(s.Name),
            Probe = p =>
            {
                clock += ScenarioRun.FrameSeconds;
                if (p.Time < lastTime)
                {
                    starts.Add(clock - p.Time);
                    began.Add(clock);
                    parties.Add(Enum.GetValues<PartyRole>().Where(r => p.Member(r) is not null)
                        .ToDictionary(r => r, r => p.Member(r)!.Position));
                }
                lastTime = p.Time;
            },
            Finished = fake => raw = fake.Timeline.Rows.ToList(),
        });
        var rows = new List<WholePhaseRow>();
        var part = -1;
        foreach (var r in raw)
        {
            if (r["kind"] == "commence") part++;
            if (part < 0 || part >= starts.Count) continue;
            var t = float.Parse(r["t"], CultureInfo.InvariantCulture);
            uint.TryParse(r.GetValueOrDefault("id"), out var id);
            rows.Add(new WholePhaseRow(part, starts[part] + t, r["kind"], id, r.GetValueOrDefault("actor_name") ?? "", r));
        }
        return new WholePhaseTimeline { Run = run, Parts = names, PartStarts = starts, PartBegan = began, Rows = rows, PartyAtStart = parties };
    }

    public int PartIndex(string name) => Parts.ToList().IndexOf(name);

    // The last time any of `actionIds` went off in `part`.
    public float LastAbility(string part, params uint[] actionIds)
        => Rows.Where(r => r.Part == PartIndex(part) && r.Kind == "ability" && actionIds.Contains(r.Id)).Max(r => r.At);

    // The first cast bar of any of `actionIds` in `part`.
    public float FirstCast(string part, params uint[] actionIds)
        => Rows.Where(r => r.Part == PartIndex(part) && r.Kind == "cast" && actionIds.Contains(r.Id)).Min(r => r.At);

    // From the last of `resolves` in `from` to the first cast of `casts` in the part after it.
    public float Gap(string from, uint[] resolves, uint[] casts)
        => FirstCast(Parts[PartIndex(from) + 1], casts) - LastAbility(from, resolves);

    public string Describe(Func<WholePhaseRow, bool>? filter = null)
        => string.Join(Environment.NewLine, Rows.Where(r => filter?.Invoke(r) ?? r.Kind is "cast" or "ability")
            .Select(r => $"{Parts[r.Part],-22} {r.At,8:F2} {r.Kind,-8} {r.Id,6} {r.Actor} {r.Raw.GetValueOrDefault("name")} {r.Raw.GetValueOrDefault("detail")}"));
}
