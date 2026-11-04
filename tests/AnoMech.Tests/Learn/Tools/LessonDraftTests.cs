using System.Collections.Generic;
using System.Linq;
using AnoMech.Learn;
using AnoMech.Learn.Tools;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Tools;

public class LessonDraftTests
{
    private static readonly string[] Roles = ["MainTank", "OffTank", "RegenHealer", "ShieldHealer", "MeleeDpsA", "MeleeDpsB", "PhysRangedDps", "CasterDps"];

    private static DumpedOrder Order(int seed, string role, float time, int line, float x, float z)
        => new("Ai", time, $"Ai.cs:{line}", null, role, seed, time, null, x, z);

    private static DumpedEvent Ev(float t, string kind, string name, string? target = null) => new(t, kind, name, "Boss", target, null, null, null);

    // Two steps: a stack at the centre, then only the tank moves out; a cast precedes each.
    private static OrderDump Dump()
    {
        var orders = new List<DumpedOrder>();
        foreach (var seed in new[] { 1, 2 })
        foreach (var role in Roles)
        {
            orders.Add(Order(seed, role, 10f, 40, 0.5f, 0.5f));
            orders.Add(Order(seed, role, 20f, 41, role == "MainTank" ? 12f : 0.5f, role == "MainTank" ? 0f : 0.5f));
            orders.Add(Order(seed, role, 30f, 42, role == "MainTank" ? 12f : 0.5f, role == "MainTank" ? 0f : 0.5f));
        }
        var events = new List<DumpedEvent> { Ev(7f, "cast", "Slipstream"), Ev(18f, "cast", "Mistral Song"), Ev(19.5f, "marker", "marker 0x10", "MainTank") };
        return new OrderDump("S", "A", 0, orders, [new DumpedRun(1, new(), events), new DumpedRun(2, new(), events)]);
    }

    private static Lesson Drafted()
    {
        var dump = Dump();
        return LessonDraft.Draft(LessonSkeleton.Generate(dump), dump, RuleMiner.Mine(dump), CueExtractor.Extract(dump));
    }

    [Test]
    public void AStackGetsOneRuleATightToleranceAndTheLastCastAsCue()
    {
        var step = Drafted().Steps[0];
        Assert.That(step.Rule, Is.EqualTo("Stand at the centre."));
        Assert.That(step.Roles, Is.Null);
        Assert.That(step.Tolerance, Is.EqualTo(1.5f));
        Assert.That(step.Cue, Is.EqualTo("Slipstream cast starts"));
        Assert.That(step.Title, Is.EqualTo("After slipstream cast starts"));
    }

    [Test]
    public void TheOddRoleOutGetsItsOwnLineAndItsOwnMarkerAsCue()
    {
        var step = Drafted().Steps[1];
        Assert.That(step.Rule, Is.EqualTo("Stand at the centre."));
        Assert.That(step.Roles!.Keys, Is.EqualTo(new[] { "MainTank" }));
        Assert.That(step.Roles["MainTank"].Rule, Is.EqualTo("Go to 12m east of the centre."));
        Assert.That(step.Cue, Is.EqualTo("A marker lands on you"));
    }

    [Test]
    public void AnOrderThatMovesNobodyIsSkipped()
    {
        var step = Drafted().Steps[2];
        Assert.That(step.Skip, Is.True);
        Assert.That(step.Rule, Is.Null);
    }

    [Test]
    public void SentencesFollowTheRulesShape()
    {
        var spot = new MinedSpot("N wall", 0f, -18f, [1]);
        Assert.That(LessonDraft.Sentence(new MinedRule("Ai", 1, 1f, "MainTank", [spot], [], new(), 1)), Is.EqualTo("Go to the north wall."));
        Assert.That(LessonDraft.Sentence(new MinedRule("Ai", 1, 1f, "MainTank", [spot, spot], [], new(), 2, Relation: "opposite SafeSpot, 17m out")), Is.EqualTo("Go opposite SafeSpot, 17m out."));
        Assert.That(LessonDraft.Sentence(new MinedRule("Ai", 1, 1f, "MainTank", [spot, spot], ["Side"], new() { ["East"] = "E wall", ["West"] = "W wall" }, 2)),
            Is.EqualTo("Depends on Side: East → E wall; West → W wall."));
    }
}
