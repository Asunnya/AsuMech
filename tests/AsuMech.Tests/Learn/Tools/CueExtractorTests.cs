using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn.Tools;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Tools;

public class CueExtractorTests
{
    private static DumpedOrder Order(int seed, string role, float time = 20f)
        => new("Ai", time, "Ai.cs:40", null, role, seed, time, null, 0f, 0f);

    private static DumpedEvent Ev(float t, string kind, string name, string? actor = null, string? target = null)
        => new(t, kind, name, actor, target, null, null, null);

    [Test]
    public void EventsInTheWindowAreRankedOwnTargetThenStableThenLatest()
    {
        var dump = new OrderDump("S", "A", 0,
            [Order(1, "MainTank"), Order(2, "MainTank")],
            [
                new DumpedRun(1, new(), [Ev(5f, "cast", "Too early", "Boss"), Ev(14f, "cast", "Slipstream", "Boss"), Ev(18f, "marker", "marker 0x1", null, "MainTank"), Ev(19f, "spawn", "plume")]),
                new DumpedRun(2, new(), [Ev(14.2f, "cast", "Slipstream", "Boss"), Ev(18.5f, "marker", "marker 0x1", null, "OffTank"), Ev(19f, "spawn", "plume")]),
            ]);
        var cues = CueExtractor.Extract(dump)[("Ai", 40, "MainTank")];
        Assert.That(cues.Select(c => c.Name), Is.EqualTo(new[] { "marker 0x1", "plume", "Slipstream", "marker 0x1" }));
        Assert.That(cues[0].Target, Is.EqualTo("MainTank"));
        Assert.That(cues[0].Stable, Is.False);
        Assert.That(cues[1].Stable, Is.True);
        Assert.That(cues[1].SecondsBefore, Is.EqualTo(1f).Within(0.01f));
        Assert.That(cues[3].Target, Is.EqualTo("another player"));
        Assert.That(cues.Any(c => c.Name == "Too early"), Is.False);
    }

    [Test]
    public void TheWindowIsPerSeedOrderTime()
    {
        var dump = new OrderDump("S", "A", 0,
            [Order(1, "MainTank", 20f), Order(2, "MainTank", 30f)],
            [new DumpedRun(1, new(), [Ev(15f, "cast", "X")]), new DumpedRun(2, new(), [Ev(25f, "cast", "X")])]);
        var cue = CueExtractor.Extract(dump)[("Ai", 40, "MainTank")].Single();
        Assert.That(cue.Stable, Is.True);
        Assert.That(cue.SecondsBefore, Is.EqualTo(5f).Within(0.01f));
    }
}
