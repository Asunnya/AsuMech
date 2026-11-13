using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn;
using AsuMech.Learn.Tools;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Tools;

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
    public void TheGlossaryWordsFieldsAndValuesAndMembershipReadsAsIfYouAreIn()
    {
        var glossary = Glossary.Parse("""{ "fields": { "NailBearing": "the nails' close pair", "FrictionTargets": "the Friction targets" }, "values": { "*": { "270": "west" }, "NailBearing": { "90": "east" } } }""");
        var table = new MinedRule("Ai", 40, 10f, "MainTank", [new MinedSpot("N wall", 0, -19, [1]), new MinedSpot("S wall", 0, 19, [2])], ["NailBearing"], new() { ["270"] = "N wall", ["90"] = "S wall" }, 2);
        Assert.That(LessonDraft.Sentence(table, glossary), Is.EqualTo("When the nails' close pair is west, go to the north wall; when the nails' close pair is east, go to the south wall."));
        Assert.That(LessonDraft.Sentence(table, Glossary.Empty), Is.EqualTo("When NailBearing is west, go to the north wall; when NailBearing is east, go to the south wall."));
        var opaque = table with { Fields = ["AwakenedDash"], SpotByValue = new() { ["0"] = "N wall", ["1"] = "S wall" } };
        Assert.That(LessonDraft.Sentence(opaque, glossary), Is.EqualTo("Depends on AwakenedDash: 0 → N wall; 1 → S wall."));
        Assert.That(Lesson.IsTable(LessonDraft.Sentence(opaque, glossary)), Is.True);
        var relation = table with { Fields = [], SpotByValue = new(), Relation = "with NailBearing as north: NW wall" };
        Assert.That(LessonDraft.Sentence(relation, glossary), Is.EqualTo("with the nails' close pair as north: the north-west wall."));
        var membership = table with { Fields = ["FrictionTargets has MainTank"], SpotByValue = new() { ["yes"] = "N wall", ["no"] = "centre" } };
        Assert.That(LessonDraft.Sentence(membership, glossary), Is.EqualTo("If you are in the Friction targets: Go to the north wall; otherwise: Stand at the centre."));
        Assert.That(Lesson.IsTable(LessonDraft.Sentence(membership, glossary)), Is.False);
        Assert.That(Drafted().Draft, Is.True);
    }

    [Test]
    public void TablesBecomeSentencesWhenEveryValueHasWordsAndGeometryNamesTheMethod()
    {
        var glossary = Glossary.Parse("""{ "fields": { "NailBearing": "the nails' close pair", "SafeSide": "the safe side" }, "values": { "SafeSide": { "1": "right", "-1": "left" } } }""");
        var bearing = new MinedRule("Ai", 40, 10f, "MainTank", [new MinedSpot("N wall", 0, -19, [1]), new MinedSpot("S wall", 0, 19, [2])], ["NailBearing"], new() { ["270"] = "N wall", ["90"] = "S wall" }, 2);
        Assert.That(LessonDraft.Sentence(bearing, glossary), Is.EqualTo("When the nails' close pair is west, go to the north wall; when the nails' close pair is east, go to the south wall."));
        var pair = bearing with { Fields = ["NailBearing", "SafeSide"], SpotByValue = new() { ["270 & 1"] = "N wall", ["90 & -1"] = "at B" } };
        Assert.That(LessonDraft.Sentence(pair, glossary), Is.EqualTo("When the nails' close pair is west and the safe side is right, go to the north wall; when the nails' close pair is east and the safe side is left, stand on waymark B."));
        var role = bearing with { Fields = ["HowlFirst"], SpotByValue = new() { ["RegenHealer"] = "N wall", ["ShieldHealer"] = "S wall" } };
        Assert.That(LessonDraft.Sentence(role, glossary), Does.StartWith("When HowlFirst is H1, go to the north wall"));
        var unworded = bearing with { Fields = ["AwakenedDash"], SpotByValue = new() { ["0"] = "N wall", ["1"] = "S wall" } };
        Assert.That(LessonDraft.Sentence(unworded, glossary), Does.StartWith("Depends on AwakenedDash"));
        Assert.That(Lesson.IsTable(LessonDraft.Sentence(unworded, glossary)), Is.True);
        Assert.That(Lesson.IsTable(LessonDraft.Sentence(bearing, glossary)), Is.False);
        var geometric = bearing with { Fields = [], SpotByValue = new(), NearestField = "SatinPlumesFirst", Purity = 0.8f };
        Assert.That(LessonDraft.Sentence(geometric, glossary, "DodgeFeathers"), Is.EqualTo("Read the strat's DodgeFeathers: the spot follows a rule the rolls alone don't show (closest roll: SatinPlumesFirst)."));
        Assert.That(Lesson.IsTable(LessonDraft.Sentence(geometric, glossary, "DodgeFeathers")), Is.True);
    }

    [Test]
    public void AStackGetsOneRuleATightToleranceAndTheLastCastAsCue()
    {
        var step = Drafted().Steps[0];
        Assert.That(step.Rule, Is.EqualTo("Stand at the centre."));
        Assert.That(step.Roles, Is.Null);
        Assert.That(step.Tolerance, Is.EqualTo(1.5f));
        Assert.That(step.Cue, Is.EqualTo("Slipstream cast starts"));
        Assert.That(step.Title, Is.EqualTo("After Slipstream cast starts"));
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

    [Test]
    public void ARollPrintedAsARecordNeverReachesTheText()
    {
        const string record = "SawCycle { CastAt = 16.19, Boss = Placement { Position = <12, 0, 0>, Rotation = 0 }, Spin = 1 }";
        var two = new List<MinedSpot> { new("N wall", 0, -19, [1]), new("S wall", 0, 19, [2]) };
        var membership = new MinedRule("Ai", 1, 1f, "MainTank", two, [$"Cycles has {record}"], new() { ["yes"] = "N wall", ["no"] = "S wall" }, 2);
        var byValue = new MinedRule("Ai", 1, 1f, "MainTank", two, ["Cycle"], new() { [record] = "N wall", ["SawCycle { Spin = -1 }"] = "S wall" }, 2);
        foreach (var rule in new[] { membership, byValue })
        {
            var text = LessonDraft.Sentence(rule);
            Assert.That(text, Does.Not.Contain("{"));
            Assert.That(text, Does.Contain("read the strat"));
        }
        Assert.That(LessonDraft.Sentence(membership), Does.Contain("(closest roll: Cycles)"));
    }
}
