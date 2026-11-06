using System;
using System.Collections.Generic;
using System.Linq;

namespace AsuMech.Learn.Mitigation;

// One line of the timeline HUD: a cast about to start or still running, or a lesson step whose
// time falls in the window.
public sealed record UpcomingItem(float At, float? Resolve, string Name, bool IsStep)
{
    public bool Running(float now) => !IsStep && At <= now && Resolve is { } r && now < r;
}

// The next casts of a scenario's embedded timeline from a moment of the run, with the lesson's
// steps laid among them. Read-only over data the lesson already has.
public static class UpcomingCasts
{
    public const int DefaultCount = 5;
    public const float DefaultWindowSeconds = 20f;

    public static List<UpcomingItem> Next(CastTimeline? timeline, Lesson? lesson, float now, int count = DefaultCount, float window = DefaultWindowSeconds)
    {
        var items = new List<UpcomingItem>();
        if (timeline != null)
            items.AddRange(timeline.Casts.Where(c => c.Resolve >= now).OrderBy(c => c.Start).Take(count)
                .Select(c => new UpcomingItem(c.Start, c.Resolve, c.Name, false)));
        var horizon = items.Count > 0 ? MathF.Max(items[^1].At, now + window) : now + window;
        if (lesson != null)
            foreach (var step in lesson.Steps)
            {
                var at = step.Key.Time ?? step.Key.From;
                if (at is not { } t || t < now || t > horizon) continue;
                items.Add(new UpcomingItem(t, step.Key.To, step.Title ?? "Move", true));
            }
        return items.OrderBy(i => i.At).ThenBy(i => i.IsStep ? 1 : 0).ToList();
    }
}
