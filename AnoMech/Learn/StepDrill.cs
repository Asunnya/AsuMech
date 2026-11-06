using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn;

// The slice of the timeline a drill keeps, in run seconds.
public sealed record DrillWindow(float From, float To);

// Picks the step worth drilling and the timeline slice around it. The slice starts early enough
// for the set-up the strat needs and ends after the step's deadline.
public static class StepDrillPlanner
{
    public const float LeadSeconds = 12f;
    public const float TailSeconds = 10f;

    public static DrillWindow Window(LessonStep step)
    {
        var start = step.Key.Time ?? step.Key.From ?? 0f;
        var end = step.Key.Time ?? step.Key.To ?? start;
        return new DrillWindow(MathF.Max(0f, start - LeadSeconds), end + TailSeconds);
    }

    // The earliest miss of the last run, else the unmastered step with the shortest streak,
    // earliest first. Null once every step is mastered.
    public static LessonStep? Weakest(Lesson lesson, PartyRole role, MasteryStore mastery, LessonReport? lastReport)
    {
        if (lastReport is { } report && report.Role == role)
        {
            var missed = report.Results.FirstOrDefault(r => r.Result != StepResult.OnTime && r.Step.Step is { Skip: false });
            if (missed.Step?.Step is { } step) return step;
        }
        return lesson.Steps
            .Where(s => !s.Skip && !mastery.Mastered(lesson, role, s.Key))
            .OrderBy(s => mastery.Streak(lesson, role, s.Key))
            .ThenBy(s => s.Key.Time ?? s.Key.From ?? 0f)
            .FirstOrDefault();
    }

    public const int AdaptiveSteps = 3;
    public const int AdaptiveStreak = 3;

    // After an Exam: the steps it missed and the ones that fail most over time, the worst few, in
    // the order the timeline reaches them, so a later step is drilled after what sets it up.
    public static List<LessonStep> Worst(Lesson lesson, PartyRole role, StepStatsStore stats, LessonReport? lastReport, int count = AdaptiveSteps)
    {
        var missed = lastReport is { } report && report.Role == role
            ? report.Results.Where(r => r.Result != StepResult.OnTime && r.Step.Step is { Skip: false }).Select(r => r.Step.Step!).ToHashSet()
            : [];
        return lesson.Steps.Where(s => !s.Skip)
            .Select(s => (Step: s, Trend: stats.Trend(lesson, role, s), Missed: missed.Contains(s)))
            .Where(x => x.Missed || x.Trend is { FailRate: > 0f })
            .OrderByDescending(x => x.Missed)
            .ThenByDescending(x => x.Trend?.FailRate ?? 0f)
            .ThenByDescending(x => x.Trend?.MeanReaction ?? 0f)
            .Take(count)
            .Select(x => x.Step)
            .OrderBy(s => s.Key.Time ?? s.Key.From ?? 0f)
            .ToList();
    }
}

