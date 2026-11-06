using AsuMech.Learn;
using AsuMech.Learn.Mitigation;

namespace AsuMech.Tests;

public class UpcomingCastsTests
{
    private static readonly CastTimeline Timeline = new("X", [
        new CastEvent(1, "Opener", 5f, 10f, 1), new CastEvent(2, "Cleave", 12f, 15f, 1), new CastEvent(3, "Towers", 20f, 26f, 1),
        new CastEvent(4, "Spread", 30f, 30f, 1), new CastEvent(5, "Stack", 40f, 45f, 1), new CastEvent(6, "Enrage", 60f, 70f, 1),
    ]);

    private static readonly Lesson Lesson = new("X", "strat", [], [
        new LessonStep(new LessonStepKey("Ai", Time: 11f), "Go to the tower"),
        new LessonStep(new LessonStepKey("Ai", From: 28f, To: 31f), "Spread out"),
        new LessonStep(new LessonStepKey("Ai", Time: 99f), "Far away"),
    ]);

    [Test]
    public void TheRunningCastStaysUntilItResolvesAndTheRestFollowInOrder()
    {
        var items = UpcomingCasts.Next(Timeline, null, now: 8f, count: 3);
        Assert.That(items.Select(i => i.Name), Is.EqualTo(new[] { "Opener", "Cleave", "Towers" }));
        Assert.That(items[0].Running(8f), Is.True);
        Assert.That(items[1].Running(8f), Is.False);
        Assert.That(UpcomingCasts.Next(Timeline, null, now: 10.5f, count: 2).Select(i => i.Name), Is.EqualTo(new[] { "Cleave", "Towers" }));
    }

    [Test]
    public void StepsInTheWindowSitAmongTheCastsByTime()
    {
        var items = UpcomingCasts.Next(Timeline, Lesson, now: 8f, count: 3, window: 20f);
        Assert.That(items.Select(i => (i.Name, i.IsStep)), Is.EqualTo(new[]
        {
            ("Opener", false), ("Go to the tower", true), ("Cleave", false), ("Towers", false), ("Spread out", true),
        }), "the window reaches 28 s, where the From/To step starts");
        var later = UpcomingCasts.Next(Timeline, Lesson, now: 26.5f, count: 2, window: 20f);
        Assert.That(later.Select(i => i.Name), Is.EqualTo(new[] { "Spread out", "Spread", "Stack" }), "a From/To step at its From; the far step is past the horizon");
        Assert.That(later[0].Resolve, Is.EqualTo(31f));
    }

    [Test]
    public void NothingLeftIsEmptyAndNoTimelineShowsOnlySteps()
    {
        Assert.That(UpcomingCasts.Next(Timeline, null, now: 80f), Is.Empty);
        Assert.That(UpcomingCasts.Next(null, Lesson, now: 10f, window: 5f).Select(i => i.Name), Is.EqualTo(new[] { "Go to the tower" }));
    }
}
