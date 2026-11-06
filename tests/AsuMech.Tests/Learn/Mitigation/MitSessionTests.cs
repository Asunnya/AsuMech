using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn.Mitigation;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Mitigation;

public class MitSessionTests
{
    private const uint Reprisal = 7535;

    private static MitExpectation Exp(int index, float opens, float closes)
        => new($"m{index}", $"mech {index}", new MitItem(Accept: ["Reprisal"]), [Reprisal], opens, closes, null, index);

    private static MitSession New(params MitExpectation[] exps) => new(exps, _ => null, _ => 60f);

    [Test]
    public void AnItemIsJudgedOnceItsWindowAndGraceHavePassed()
    {
        var session = New(Exp(0, 20f, 35f), Exp(1, 80f, 95f));
        var resolved = new List<MitVerdict>();
        session.ItemResolved += resolved.Add;
        session.Record(Reprisal, 0, 30f);
        session.Tick(36f);
        Assert.That(resolved, Is.Empty, "still inside the late grace");
        session.Tick(35f + MitSession.LateGraceSeconds + 0.1f);
        Assert.That(resolved.Select(v => (v.Expectation.Index, v.Result)), Is.EqualTo(new[] { (0, MitResult.OnTime) }));
        Assert.That(session.Upcoming(40f).Select(e => e.Index), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void FinishJudgesTheRestAndIsTheReport()
    {
        var session = New(Exp(0, 20f, 35f), Exp(1, 80f, 95f));
        session.Record(Reprisal, 0, 30f);
        var report = session.Finish();
        Assert.That(report.Select(v => v.Result), Is.EqualTo(new[] { MitResult.OnTime, MitResult.Missed }));
        Assert.That(session.Finished, Is.True);
        session.Record(Reprisal, 0, 90f);
        Assert.That(session.Uses, Has.Count.EqualTo(1), "nothing is recorded after the end");
    }

    [Test]
    public void OffPlanUsesAreListed()
    {
        var session = New(Exp(0, 20f, 35f));
        session.Record(Reprisal, 0, 30f);
        session.Record(188, 0, 50f);
        session.Finish();
        Assert.That(session.OffPlan().Select(u => u.ActionId), Is.EqualTo(new[] { 188u }));
    }
}
