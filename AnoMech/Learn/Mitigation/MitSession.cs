using System;
using System.Collections.Generic;
using System.Linq;

namespace AnoMech.Learn.Mitigation;

// One run's mitigation training: the plan column resolved to expectations, the uses recorded as
// the player presses them, and a verdict per item once its window (plus a short grace for a late
// press) has passed. Finish() judges everything that's left and is the report's source of truth.
public sealed class MitSession
{
    public const float LateGraceSeconds = 3f;

    private readonly List<MitUse> uses = [];
    private readonly HashSet<int> judged = [];
    private readonly Func<ulong, string?> targetRoleOf;
    private readonly Func<uint, float> recastOf;

    public MitSession(IReadOnlyList<MitExpectation> expectations, Func<ulong, string?> targetRoleOf, Func<uint, float> recastOf)
    {
        Expectations = expectations;
        this.targetRoleOf = targetRoleOf;
        this.recastOf = recastOf;
    }

    public IReadOnlyList<MitExpectation> Expectations { get; }
    public IReadOnlyList<MitUse> Uses => uses;
    public List<MitVerdict> Verdicts { get; } = [];
    public bool Finished { get; private set; }

    public event Action<MitVerdict>? ItemResolved;

    public void Record(uint actionId, ulong targetId, float time)
    {
        if (Finished) return;
        uses.Add(new MitUse(actionId, targetId, time));
    }

    // Items still to come, soonest first.
    public IEnumerable<MitExpectation> Upcoming(float now)
        => Expectations.Where(e => !judged.Contains(e.Index) && e.Closes >= now - LateGraceSeconds).OrderBy(e => e.Closes);

    public void Tick(float now)
    {
        if (Finished) return;
        var due = Expectations.Where(e => !judged.Contains(e.Index) && now > e.Closes + LateGraceSeconds).ToList();
        if (due.Count == 0) return;
        Emit(MitEvaluator.Evaluate(Expectations, uses, targetRoleOf, recastOf).Where(v => due.Any(d => d.Index == v.Expectation.Index)));
    }

    public IReadOnlyList<MitVerdict> Finish()
    {
        if (Finished) return Verdicts;
        Finished = true;
        var all = MitEvaluator.Evaluate(Expectations, uses, targetRoleOf, recastOf);
        Emit(all.Where(v => !judged.Contains(v.Expectation.Index)));
        // The final pass sees every use; it replaces the early verdicts in the report.
        Verdicts.Clear();
        Verdicts.AddRange(all.OrderBy(v => v.Expectation.Closes));
        return Verdicts;
    }

    public IEnumerable<MitUse> OffPlan()
        => uses.Where(u => Verdicts.All(v => v.Use != u) && MitCatalog.Find(u.ActionId) != null);

    private void Emit(IEnumerable<MitVerdict> verdicts)
    {
        foreach (var v in verdicts)
        {
            judged.Add(v.Expectation.Index);
            Verdicts.Add(v);
            ItemResolved?.Invoke(v);
        }
    }
}
