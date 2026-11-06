using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AsuMech.Network;

// A player's thumbs up or down on one lesson step: its text (rule, cue) or its toolbox picture.
// Ids are the lesson's "scenario|strat" and the step's MasteryStore.StepId (the toolbox step's
// index and label for Target == Toolbox). No character, world or name ever goes in.
public sealed record LessonFeedback(string Lesson, string Step, string Target, int Vote, string Comment, string Version)
{
    public const string Text = "text";
    public const string Toolbox = "toolbox";
    public const int MaxIdChars = 200;
    public const int MaxCommentChars = 500;
    public const int MaxBodyBytes = 4096;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public bool IsValid =>
        Id(Lesson) && Id(Step) && Target is Text or Toolbox && Vote is 1 or -1
        && Comment is { Length: <= MaxCommentChars } && !Comment.Any(char.IsControl)
        && Version is { Length: <= 40 } && !Version.Any(char.IsControl);

    private static bool Id(string? s) => s is { Length: > 0 and <= MaxIdChars } && !s.Any(char.IsControl);
}

public sealed record StoredLessonFeedback(DateTime At, string Peer, LessonFeedback Feedback);

public static class LessonFeedbackSummary
{
    // Each player's last vote per step and target counts once; comments are listed newest first.
    public static IEnumerable<string> Lines(IEnumerable<StoredLessonFeedback> entries)
    {
        var latest = entries
            .GroupBy(e => (e.Peer, e.Feedback.Lesson, e.Feedback.Step, e.Feedback.Target))
            .Select(g => g.MaxBy(e => e.At)!)
            .ToList();
        foreach (var lesson in latest.GroupBy(e => e.Feedback.Lesson).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            yield return lesson.Key;
            var steps = lesson.GroupBy(e => (e.Feedback.Step, e.Feedback.Target))
                .OrderBy(g => g.Sum(e => e.Feedback.Vote))
                .ThenBy(g => g.Key.Step, StringComparer.Ordinal);
            foreach (var step in steps)
            {
                var up = step.Count(e => e.Feedback.Vote > 0);
                var down = step.Count(e => e.Feedback.Vote < 0);
                yield return $"  {step.Key.Target,-7} {step.Key.Step}  +{up} -{down}";
                foreach (var e in step.Where(e => e.Feedback.Comment.Length > 0).OrderByDescending(e => e.At))
                    yield return $"      {(e.Feedback.Vote > 0 ? "+" : "-")} {e.Feedback.Comment}";
            }
        }
    }
}
