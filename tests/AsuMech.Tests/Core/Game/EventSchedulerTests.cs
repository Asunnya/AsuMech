using AsuMech.Core.Game;

namespace AsuMech.Tests;

public class EventSchedulerTests
{
    [Test]
    public void KeepWindowFiresOnlyTheEntriesInsideItFromItsStart()
    {
        var events = new EventScheduler();
        var fired = new List<float>();
        foreach (var t in new[] { 1f, 10f, 15f, 20f, 30f })
            events.Add(t, () => fired.Add(t));

        events.KeepWindow(10f, 20f);
        Assert.That(events.Elapsed, Is.EqualTo(10f));

        events.Tick(0.01f);
        Assert.That(fired, Is.EqualTo(new[] { 10f }));

        events.Tick(20f);
        Assert.That(fired, Is.EqualTo(new[] { 10f, 15f, 20f }));
        Assert.That(events.IsEmpty);
    }

    [Test]
    public void DelayStartsTheTimelineLaterWithItsTimesUnchanged()
    {
        var events = new EventScheduler();
        var fired = new List<float>();
        events.Add(2f, () => fired.Add(events.Elapsed));
        events.Delay(5f);
        Assert.That(events.Elapsed, Is.EqualTo(-5f));
        events.Tick(6.9f);
        Assert.That(fired, Is.Empty);
        events.Tick(0.2f);
        Assert.That(fired, Has.Count.EqualTo(1));
        Assert.That(fired[0], Is.EqualTo(2.1f).Within(1e-4f));
    }
}
