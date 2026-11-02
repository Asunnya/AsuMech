using System;
using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Learn;

// Consecutive in-place results per lesson step and role. Assisted shows the hint later the more
// of them there are, and not at all once a step is mastered.
public sealed class MasteryStore(IDictionary<string, int> hits, Action save)
{
    public const int HitsToMaster = 3;

    public static string StepId(LessonStepKey key)
        => key.Time is { } t ? $"{key.Cls}@{t:0.##}" : $"{key.Cls}@{key.From:0.##}-{key.To:0.##}";

    public static string Id(Lesson lesson, PartyRole role, LessonStepKey key) => $"{lesson.Scenario}|{lesson.Strat}|{role}|{StepId(key)}";

    public int Streak(Lesson lesson, PartyRole role, LessonStepKey key) => hits.TryGetValue(Id(lesson, role, key), out var n) ? n : 0;

    public bool Mastered(Lesson lesson, PartyRole role, LessonStepKey key) => Streak(lesson, role, key) >= HitsToMaster;

    // 0 = show the hint at once, 1 = never.
    public float HintDelayFraction(Lesson lesson, PartyRole role, LessonStepKey key)
        => Math.Min(Streak(lesson, role, key), HitsToMaster) / (float)HitsToMaster;

    public void Record(Lesson lesson, PartyRole role, LessonStepKey key, StepResult result)
    {
        var id = Id(lesson, role, key);
        var before = hits.TryGetValue(id, out var n) ? n : 0;
        var after = result == StepResult.OnTime ? before + 1 : 0;
        if (after == before) return;
        hits[id] = after;
        save();
    }

    public int MasteredCount(Lesson lesson, PartyRole role)
    {
        var count = 0;
        foreach (var step in lesson.Steps)
            if (!step.Skip && Mastered(lesson, role, step.Key)) count++;
        return count;
    }
}
