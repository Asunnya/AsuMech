using AsuMech.Windows.Asu;
using NUnit.Framework;

namespace AsuMech.Tests.Windows.Asu;

public class WatchSessionTests
{
    private sealed class Target : IWatchTarget
    {
        public bool BotControl { get; set; }
        public float Speed { get; set; } = 1.5f;
        public bool Counting { get; set; } = true;
    }

    [Test]
    public void BeginningBorrowsAllThree()
    {
        var target = new Target();
        using var watch = new WatchSession(target);
        Assert.That(target.BotControl, Is.True);
        Assert.That(target.Speed, Is.EqualTo(WatchSession.WatchSpeed));
        Assert.That(target.Counting, Is.False);
    }

    [Test]
    public void StoppingMidRunRestoresEverything()
    {
        var target = new Target();
        var watch = new WatchSession(target);
        watch.Tick(runLive: false, 0.1f);
        watch.Tick(runLive: true, 0.1f);
        Assert.That(watch.Ended, Is.False);
        watch.Tick(runLive: false, 0.1f);
        Assert.That(watch.Ended, Is.True);
        Assert.That(target.BotControl, Is.False);
        Assert.That(target.Speed, Is.EqualTo(1.5f));
        Assert.That(target.Counting, Is.True);
    }

    [Test]
    public void ARunThatNeverStartsGivesUp()
    {
        var target = new Target();
        var watch = new WatchSession(target);
        watch.Tick(false, WatchSession.MaxSecondsToStart / 2);
        Assert.That(watch.Ended, Is.False);
        watch.Tick(false, WatchSession.MaxSecondsToStart);
        Assert.That(watch.Ended, Is.True);
        Assert.That(target.BotControl, Is.False);
    }

    [Test]
    public void DisposingTwiceRestoresOnce()
    {
        var target = new Target();
        var watch = new WatchSession(target);
        watch.Dispose();
        target.Speed = 3f;
        watch.Dispose();
        Assert.That(target.Speed, Is.EqualTo(3f));
    }
}
