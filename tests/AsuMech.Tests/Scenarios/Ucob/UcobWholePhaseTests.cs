using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Scenarios.Ucob.P3Blackfire;
using AsuMech.Scenarios.Ucob.P3GrandOctet;
using AsuMech.Scenarios.Ucob.P3Quickmarch;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Tests;

// Whole phase P3: from the last thing a part resolves to the next part's trio cast, as the clear
// paces it, Bahamut tanked in between.
public class UcobWholePhaseTests
{
    private sealed record Row(float At, int Part, string Kind, uint Id, string Target);

    private sealed record Handoff(int Part, uint LastResolved, uint Trio, float Gap);

    private static readonly Handoff[] P3Handoffs =
    [
        new(1, ActionId.Flatten, ActionId.QuickmarchTrio, 3.16f),
        new(2, ActionId.FlareBreath, ActionId.BlackfireTrio, 12.25f),
        new(3, ActionId.Gigaflare, ActionId.FellruinTrio, 22.50f),
        new(4, ActionId.Flatten, ActionId.HeavensfallTrio, 9.31f),
        new(5, ActionId.Gigaflare, ActionId.TenstrikeTrio, 21.51f),
        new(6, ActionId.Gigaflare, ActionId.GrandOctet, 18.44f),
    ];

    private static List<Row> Play(System.Type picked, int strat, bool wholePhase, out ScenarioRun run)
    {
        var rows = new List<Row>();
        var global = 0f;
        var read = 0;
        var part = -1;
        run = ScenarioRun.Execute(picked, strat, 1, new ScenarioRunOptions
        {
            WholePhase = wholePhase,
            PlayerRole = PartyRole.MainTank,
            Started = _ => part++,
            Probe = _ =>
            {
                global += ScenarioRun.FrameSeconds;
                var timeline = ((FakeBattleCharas)Natives.BattleCharas).Timeline.Rows;
                for (; read < timeline.Count; read++)
                {
                    var r = timeline[read];
                    if (r["kind"] is not ("cast" or "impact") || !uint.TryParse(r["id"], out var id)) continue;
                    rows.Add(new Row(global, part, r["kind"], id, r.GetValueOrDefault("target_name", "")));
                }
            },
        });
        return rows;
    }

    [TestCase(0)]
    [TestCase(1)]
    public void EachTrioFollowsThePreviousPartAsTheFightPacesIt(int strat)
    {
        var rows = Play(typeof(UcobP3GrandOctetScenario), strat, wholePhase: true, out var run);
        Assert.That(run.Passed, run.ToString());
        foreach (var handoff in P3Handoffs)
        {
            var resolved = rows.Last(r => r.Part == handoff.Part - 1 && r.Kind == "impact" && r.Id == handoff.LastResolved).At;
            var trio = rows.First(r => r.Part == handoff.Part && r.Kind == "cast" && r.Id == handoff.Trio).At;
            Assert.That(trio - resolved, Is.EqualTo(handoff.Gap).Within(1f), $"part {handoff.Part}");
        }
    }

    [Test]
    public void BahamutBustsHisTankBetweenTheTrios()
    {
        var rows = Play(typeof(UcobP3GrandOctetScenario), 0, wholePhase: true, out var run);
        Assert.That(run.Passed, run.ToString());
        int Busters(int part, uint trio) => rows.Count(r => r.Part == part && r.Kind == "impact" && r.Id is ActionId.FlareBreath or ActionId.Flatten
            && r.At < rows.First(t => t.Part == part && t.Kind == "cast" && t.Id == trio).At);
        Assert.That(Busters(2, ActionId.BlackfireTrio), Is.EqualTo(1));
        Assert.That(Busters(3, ActionId.FellruinTrio), Is.EqualTo(3));
        Assert.That(Busters(4, ActionId.HeavensfallTrio), Is.EqualTo(0));
        Assert.That(Busters(5, ActionId.TenstrikeTrio), Is.EqualTo(3));
        Assert.That(Busters(6, ActionId.GrandOctet), Is.EqualTo(2));
    }

    [Test]
    public void APartPlayedOnItsOwnStartsAtItsTrioAsBefore()
    {
        var quickmarch = Play(typeof(UcobP3QuickmarchScenario), 0, wholePhase: false, out var run);
        Assert.That(run.Passed, run.ToString());
        Assert.That(quickmarch.First(r => r.Kind == "cast" && r.Id == ActionId.QuickmarchTrio).At,
            Is.EqualTo(UcobP3QuickmarchScenario.TrioCast).Within(0.05f));
        var blackfire = Play(typeof(UcobP3BlackfireScenario), 0, wholePhase: false, out run);
        Assert.That(run.Passed, run.ToString());
        Assert.That(blackfire.First(r => r.Kind == "cast" && r.Id == ActionId.BlackfireTrio).At,
            Is.EqualTo(UcobP3BlackfireScenario.TrioCast).Within(0.05f));
        Assert.That(blackfire.Any(r => r.At < UcobP3BlackfireScenario.TrioCast && r.Id == ActionId.Flatten), Is.False);
    }
}
