using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios.Umad.P3BlackHole;

namespace AnoMech.Tests;

// The result marks come from an invisible helper aimed at the player, as retail's do: one the
// player casts on itself ends the client's own cast bar mid-cast and leaves Casting on.
public class ScenarioSessionTests
{
    private const string MarkHelperBNpcBase = "6358";
    private const string SuccessMark = "9450";
    private const string FailureMark = "9451";

    [Test]
    public void AReportedDeathMarksThePlayerFromAHelper()
    {
        var (run, timeline, player) = Run(marks: true, stopAt: 2f, ReportDeathAt1s);
        Assert.That(run.Failure, Is.Null);
        AssertMarkedFromHelper(timeline, FailureMark, player);
    }

    [Test]
    public void ACleanRunMarksThePlayerFromAHelper()
    {
        var (run, timeline, player) = Run(marks: true, stopAt: null, _ => { });
        Assert.That(run.Passed, run.ToString());
        AssertMarkedFromHelper(timeline, SuccessMark, player);
    }

    [Test]
    public void NoHelperSpawnsWithTheMarksOff()
    {
        var (run, timeline, _) = Run(marks: false, stopAt: 2f, ReportDeathAt1s);
        Assert.That(run.Failure, Is.Null);
        Assert.That(Rows(timeline, "spawn", MarkHelperBNpcBase), Is.Empty);
        Assert.That(Rows(timeline, "ability", FailureMark), Is.Empty);
    }

    private static void ReportDeathAt1s(ScenarioProbe p)
    {
        if (!p.Crossed(1f)) return;
        p.Game.GodMode = true;
        p.Game.Kill(p.World.Party.Player!, "test");
    }

    private static (ScenarioRun Run, SimTimeline Timeline, string Player) Run(bool marks, float? stopAt, Action<ScenarioProbe> probe)
    {
        SimTimeline? timeline = null;
        var player = "";
        var run = ScenarioRun.Execute(typeof(UmadP3BlackHoleScenario), 0, 1, new ScenarioRunOptions
        {
            Configure = c => c.EnableMechanicResultMarks = marks,
            StopAt = stopAt,
            Probe = p =>
            {
                timeline ??= ((FakeBattleCharas)Natives.BattleCharas).Timeline;
                player = p.World.Party.Player!.EntityId.ToString("X8");
                probe(p);
            },
        });
        return (run, timeline!, player);
    }

    private static void AssertMarkedFromHelper(SimTimeline timeline, string mark, string player)
    {
        var helper = Rows(timeline, "spawn", MarkHelperBNpcBase).Single()["actor"];
        var marks = Rows(timeline, "ability", mark).ToList();
        Assert.That(marks, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(marks[0]["actor"], Is.EqualTo(helper));
            Assert.That(marks[0]["target"], Is.EqualTo(player));
        });
    }

    private static IEnumerable<Dictionary<string, string>> Rows(SimTimeline timeline, string kind, string id)
        => timeline.Rows.Where(row => row["kind"] == kind && row["id"] == id);
}
