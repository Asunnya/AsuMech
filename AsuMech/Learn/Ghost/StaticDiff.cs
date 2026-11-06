using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using AsuMech.Core.Game.Party;
using AsuMech.Learn.Tools;

namespace AsuMech.Learn.Ghost;

// One strat call for one seat against a real pull: where the seat's player was when the strat has
// them settled, and the nearest of the spots the strat gives that seat over the recorded seeds (the
// real pull rolled its own way, so any seed's answer to the same call counts).
// RollDependent: the strat's spot for this call moves with a roll; no recorded seed may have rolled
// what the real pull did, so a miss there is Unsure rather than a difference.
public sealed record StaticDiffRow(PartyRole Role, string Title, float At, Vector2 Real, Vector2 NearestSpot, float Distance, float Tolerance, bool RollDependent, string Key)
{
    public bool Off => Distance > Tolerance + StaticDiff.PoseSlack;
    public bool Differs => Off && !RollDependent;
    public bool Unsure => Off && RollDependent;
}

public static class StaticDiff
{
    // ACT positions trail the player by up to a second; a step is called different only past this
    // much more than its tolerance.
    public const float PoseSlack = 1f;
    private const float DefaultTolerance = 1.5f;
    // Spots this far apart across seeds mean the call follows a roll.
    public const float RollSpread = 3f;
    private const float SettleIfNoNext = 4f;

    public static List<StaticDiffRow> Compare(GhostTrack track, Lesson? lesson, IReadOnlyList<DumpedOrder> orders, IEnumerable<PartyRole> roles)
    {
        var rows = new List<StaticDiffRow>();
        foreach (var role in roles)
        {
            if (track.LaneFor(role) is not { } lane) continue;
            var mine = orders.Where(o => o.Role == role.ToString()).ToList();
            foreach (var call in mine.GroupBy(o => (o.Cls, Time: MathF.Round(o.Time, 2), o.Line)).OrderBy(g => g.Min(o => o.IssuedAt)))
            {
                var step = lesson?.StepFor(call.First().Key);
                if (step is { Skip: true }) continue;
                var settle = Median(call.Select(o => SettledAt(o, mine)));
                if (track.At(lane, settle) is not { } real) continue;
                var spots = call.Select(o => new Vector2(o.X, o.Z)).ToList();
                var nearest = spots.MinBy(s => Vector2.Distance(s, real.Position));
                var distance = Vector2.Distance(nearest, real.Position);
                var tolerance = step?.ToleranceFor(role) ?? DefaultTolerance;
                var title = step?.Title ?? $"{call.Key.Cls}@{call.Key.Time.ToString("0.##", CultureInfo.InvariantCulture)}";
                var spread = spots.Max(a => spots.Max(b => Vector2.Distance(a, b)));
                rows.Add(new StaticDiffRow(role, title, settle, real.Position, nearest, distance, tolerance, spread > RollSpread, $"{call.Key.Cls}:{call.Key.Line}"));
            }
        }
        return rows;
    }

    // When the strat has the seat in place: its deadline, else just before its next order, else a few seconds on.
    private static float SettledAt(DumpedOrder order, IReadOnlyList<DumpedOrder> seat)
    {
        if (order.ArrivalTime is { } arrival) return arrival;
        var next = seat.Where(o => o.Seed == order.Seed && o.IssuedAt > order.IssuedAt + 0.05f).Select(o => o.IssuedAt).DefaultIfEmpty(float.NaN).Min();
        return float.IsNaN(next) ? order.IssuedAt + SettleIfNoNext : MathF.Max(order.IssuedAt, next - 0.1f);
    }

    private static float Median(IEnumerable<float> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted.Count == 0 ? 0f : sorted[sorted.Count / 2];
    }

    public static string Report(IReadOnlyList<StaticDiffRow> rows)
    {
        var sb = new StringBuilder();
        foreach (var byRole in rows.GroupBy(r => r.Role))
        {
            sb.AppendLine($"{byRole.Key}: {byRole.Count(r => r.Differs)} of {byRole.Count()} steps differ, {byRole.Count(r => r.Unsure)} unsure (the spot follows a roll no seed matched)");
            foreach (var r in byRole)
                sb.AppendLine($"  {F(r.At),7}s {(r.Differs ? "DIFF" : r.Unsure ? "ROLL" : " ok "),4} {F(r.Distance),5} y  {r.Title}  real ({F(r.Real.X)}, {F(r.Real.Y)}) strat ({F(r.NearestSpot.X)}, {F(r.NearestSpot.Y)})  [{r.Key}]");
        }
        // A step is the same call at the same moment for every seat.
        var steps = rows.Where(r => r.Differs).GroupBy(r => (r.Key, At: MathF.Round(r.At))).OrderBy(g => g.Key.At).ToList();
        if (steps.Count == 0) sb.AppendLine("Where the strat's spot doesn't depend on a roll, the pull matches it at every step.");
        else
        {
            sb.AppendLine("Your static does these differently:");
            foreach (var step in steps)
                sb.AppendLine($"  {F(step.Key.At)}s {step.First().Title} [{step.Key.Key}]: {string.Join(", ", step.Select(r => $"{r.Role} {F(r.Distance)} y"))}");
        }
        return sb.ToString();
    }

    private static string F(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);
}
