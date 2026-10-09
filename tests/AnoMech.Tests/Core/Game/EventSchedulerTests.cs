using AnoMech.Core.Game;

namespace AnoMech.Tests;

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
}
