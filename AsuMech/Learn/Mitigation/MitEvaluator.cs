using System;
using System.Collections.Generic;
using System.Linq;

namespace AsuMech.Learn.Mitigation;

public enum MitResult { OnTime, Missed, Late, TooEarly, WrongTarget, Forbidden, CooldownConflict, Reminder }

public sealed record MitUse(uint ActionId, ulong TargetId, float Time);

public sealed record MitVerdict(MitExpectation Expectation, MitResult Result, MitUse? Use, string? Cause)
{
    public bool IsError => Result is not (MitResult.OnTime or MitResult.Reminder);
}

// Pairs recorded uses with expectations: a use satisfies at most one item, the nearest in time of
// the same column. Uses that match nothing are "off plan" and explain later cooldown conflicts.
public static class MitEvaluator
{
    // A use this close before the window was meant for it; earlier is an unrelated press.
    public const float EarlySlackSeconds = 15f;

    public static List<MitVerdict> Evaluate(IReadOnlyList<MitExpectation> expectations, IReadOnlyList<MitUse> uses, Func<ulong, string?> targetRoleOf, Func<uint, float> recastOf)
    {
        var verdicts = new List<MitVerdict>();
        var taken = new HashSet<MitUse>();
        var ordered = expectations.OrderBy(e => e.Closes).ToList();
        foreach (var exp in ordered)
        {
            var item = exp.Item;
            if (item.Reminder != null || item.ExpectStatus != null)
            {
                verdicts.Add(new MitVerdict(exp, MitResult.Reminder, null, null));
                continue;
            }
            if (item.Forbid is { Count: > 0 })
            {
                var forbiddenIds = item.Forbid.Select(MitCatalog.Find).OfType<MitAction>().Select(a => a.Id).ToHashSet();
                var broken = uses.FirstOrDefault(u => forbiddenIds.Contains(u.ActionId) && u.Time >= exp.Opens && u.Time <= exp.Closes);
                verdicts.Add(new MitVerdict(exp, broken is null ? MitResult.OnTime : MitResult.Forbidden, broken, broken is null ? null : MitCatalog.Find(broken.ActionId)?.Name));
                continue;
            }
            if (exp.AcceptIds.Count == 0)
            {
                verdicts.Add(new MitVerdict(exp, MitResult.Reminder, null, "no action of this job accepted"));
                continue;
            }
            var next = ordered.FirstOrDefault(o => o.Index != exp.Index && o.Closes > exp.Closes && o.AcceptIds.Intersect(exp.AcceptIds).Any());
            var lateUntil = next?.Opens ?? float.MaxValue;
            var candidates = uses.Where(u => !taken.Contains(u) && exp.AcceptIds.Contains(u.ActionId)).OrderBy(u => Math.Abs(u.Time - exp.Closes)).ToList();
            var inWindow = candidates.FirstOrDefault(u => u.Time >= exp.Opens && u.Time <= exp.Closes);
            if (inWindow != null)
            {
                taken.Add(inWindow);
                var wrongTarget = exp.Target != null && exp.Target is not ("self" or "party") && targetRoleOf(inWindow.TargetId) is { } role && !RoleMatches(exp.Target, role);
                verdicts.Add(new MitVerdict(exp, wrongTarget ? MitResult.WrongTarget : MitResult.OnTime, inWindow, wrongTarget ? targetRoleOf(inWindow.TargetId) : null));
                continue;
            }
            var late = candidates.FirstOrDefault(u => u.Time > exp.Closes && u.Time < lateUntil);
            if (late != null)
            {
                taken.Add(late);
                verdicts.Add(new MitVerdict(exp, MitResult.Late, late, $"{late.Time - exp.Closes:F1}s after the hit"));
                continue;
            }
            var early = candidates.FirstOrDefault(u => u.Time < exp.Opens && u.Time >= exp.Opens - EarlySlackSeconds);
            if (early != null)
            {
                taken.Add(early);
                verdicts.Add(new MitVerdict(exp, MitResult.TooEarly, early, $"{exp.Opens - early.Time:F1}s before the effect could still cover the hit"));
                continue;
            }
            // Nothing usable: was the button burned off plan within its recast?
            var burned = uses.Where(u => !taken.Contains(u) && exp.AcceptIds.Contains(u.ActionId) && u.Time < exp.Opens && u.Time + recastOf(u.ActionId) > exp.Closes).OrderByDescending(u => u.Time).FirstOrDefault();
            verdicts.Add(burned is null
                ? new MitVerdict(exp, MitResult.Missed, null, null)
                : new MitVerdict(exp, MitResult.CooldownConflict, burned, $"{MitCatalog.Find(burned.ActionId)?.Name} used at {burned.Time:F1}s"));
        }
        return verdicts;
    }

    private static bool RoleMatches(string target, string role) => target switch
    {
        "MT" => role == "MainTank",
        "OT" => role == "OffTank",
        "tank" => role is "MainTank" or "OffTank",
        "healer" => role is "RegenHealer" or "ShieldHealer",
        _ => true,
    };
}
