using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn;

// One judged try at a lesson step: the result, how long after the order the player started toward
// the spot, how far off they ended, and what caught them when something did.
public sealed class StepAttempt
{
    public StepResult Result { get; set; }
    public float? Reaction { get; set; }
    public float? Distance { get; set; }
    public string? Failure { get; set; }
    public DateTime When { get; set; }
}

public sealed record StepTrend(LessonStep Step, int Attempts, float FailRate, float? MeanReaction, float? MeanDistance, float? RecentFailRate, float? EarlierFailRate, string? CommonFailure)
{
    // Positive: failing more lately than before.
    public float? Trend => RecentFailRate is { } recent && EarlierFailRate is { } earlier ? recent - earlier : null;
}

// Every try at every step, per lesson, role and step, the last few kept. The map in the lesson
// report and the adaptive drill read it; mastery (hint timing) stays in MasteryStore.
public sealed class StepStatsStore(IDictionary<string, List<StepAttempt>> attempts, Action save)
{
    public const int Kept = 20;
    public const int MinAttemptsForMap = 5;
    public const int Recent = 5;

    public void Record(Lesson lesson, PartyRole role, LessonStepKey key, StepAttempt attempt)
    {
        var id = MasteryStore.Id(lesson, role, key);
        if (!attempts.TryGetValue(id, out var list)) attempts[id] = list = [];
        list.Add(attempt);
        if (list.Count > Kept) list.RemoveRange(0, list.Count - Kept);
        save();
    }

    public IReadOnlyList<StepAttempt> For(Lesson lesson, PartyRole role, LessonStepKey key)
        => attempts.TryGetValue(MasteryStore.Id(lesson, role, key), out var list) ? list : [];

    public StepTrend? Trend(Lesson lesson, PartyRole role, LessonStep step)
    {
        var list = For(lesson, role, step.Key);
        if (list.Count == 0) return null;
        static float FailRate(IEnumerable<StepAttempt> xs) { var a = xs.ToList(); return a.Count == 0 ? 0f : a.Count(x => x.Result != StepResult.OnTime) / (float)a.Count; }
        var reactions = list.Where(a => a.Reaction is not null).Select(a => a.Reaction!.Value).ToList();
        var distances = list.Where(a => a.Distance is not null).Select(a => a.Distance!.Value).ToList();
        var recent = list.Skip(Math.Max(0, list.Count - Recent)).ToList();
        var earlier = list.Take(Math.Max(0, list.Count - Recent)).ToList();
        var failure = list.Where(a => a.Failure is { Length: > 0 }).GroupBy(a => a.Failure!).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
        return new StepTrend(step, list.Count, FailRate(list), reactions.Count == 0 ? null : reactions.Average(), distances.Count == 0 ? null : distances.Average(),
            recent.Count == 0 ? null : FailRate(recent), earlier.Count == 0 ? null : FailRate(earlier), failure);
    }

    // The steps with enough tries to judge, worst first: fail rate, then slower reaction.
    public List<StepTrend> Map(Lesson lesson, PartyRole role, int minAttempts = MinAttemptsForMap)
        => lesson.Steps.Where(s => !s.Skip).Select(s => Trend(lesson, role, s)).OfType<StepTrend>()
            .Where(t => t.Attempts >= minAttempts)
            .OrderByDescending(t => t.FailRate).ThenByDescending(t => t.MeanReaction ?? 0f).ToList();
}
