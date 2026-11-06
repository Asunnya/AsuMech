using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn;
using AsuMech.Learn.Toolbox;
using AsuMech.Learn.Tools;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Toolbox;

public class ExampleExtractorTests
{
    [Test]
    public void GarudaPlumesSistersAndNothingElseComeOffTheNaurPlan()
    {
        var scenario = ScenarioCatalog.Create().Scenarios.OfType<UwuP1GarudaScenario>().Single();
        var lesson = LessonLoader.For(scenario, scenario.AiStrats[0])!;
        var plan = ToolboxPlans.Load(lesson.Toolbox!.PlanId)!;
        var state = new Dictionary<string, string>
        {
            ["SuparnaSongSpot"] = "(0,-19.5)", ["ChiradaSongSpot"] = "(19.5,0)", ["MistralSongTarget"] = "RegenHealer",
            ["SistersSongTargets"] = "[CasterDps,MeleeDpsA]", ["FrictionTargets"] = "[MeleeDpsB,RegenHealer]",
            ["SatinPlumesFirst"] = "[(4,-16),(16,4),(-4,16),(-16,-4)]", ["SatinPlumesSecond"] = "[(12,-12),(12,12),(-12,12),(-12,-12)]",
            ["MesohighTargets"] = "[MainTank,OffTank]",
        };
        var dump = new OrderDump(lesson.Scenario, lesson.Strat, 0, [], [new DumpedRun(lesson.Toolbox.ExampleSeed ?? 1, state, [])]);
        var extraction = ExampleExtractor.Extract(plan, lesson, lesson.Toolbox, dump, typeof(UwuP1GarudaStateOverrides), typeof(UwuP1GarudaState));
        TestContext.Out.WriteLine(string.Join("\n", extraction.Pins.Select(p => $"{p.Property} = {p.Value} ({p.How})").Concat(extraction.Skipped.Select(s => "skip " + s))));
        Assert.That(extraction.Example["SatinPlumesFirst"], Is.EqualTo("[(16,-4),(-4,16),(-12,-12),(-16,4)]"));
        Assert.That(extraction.Example["SatinPlumesSecond"], Is.EqualTo("[(12,12),(-12,12),(-12,-12),(4,-16)]"));
        Assert.That(extraction.Example["SuparnaSongBearing"], Is.EqualTo("270"));
        Assert.That(extraction.Example["ChiradaSongBearing"], Is.EqualTo("180"));
        Assert.That(extraction.Pins.Single(p => p.Property == "SatinPlumesFirst").How, Does.Contain("SatinPlumeSpots"));
        // The hand-written example in the embedded lesson is what the extractor finds.
        foreach (var (key, value) in lesson.Toolbox.Example!)
            Assert.That(extraction.Example.GetValueOrDefault(key), Is.EqualTo(value), key);
        Assert.That(extraction.Example["MesohighTargets"], Is.EqualTo("[OffTank,MeleeDpsA]"));
        Assert.That(extraction.Pins.Single(p => p.Property == "MesohighTargets").How, Does.StartWith("the buff icon changed on"));
        // Friction One draws the tank's buff again beside the two targets: three players, not two, so it stays out.
        Assert.That(extraction.Skipped, Has.Some.StartsWith("FrictionTargets"));
        Assert.That(extraction.Skipped, Has.Some.StartsWith("SistersSongTargets"));
        Assert.That(extraction.Example.Keys, Is.EquivalentTo(new[] { "SatinPlumesFirst", "SatinPlumesSecond", "SuparnaSongBearing", "ChiradaSongBearing", "MesohighTargets" }));
    }

    [Test]
    public void TokensDropOrdinalsAndPlurals()
    {
        Assert.That(ExampleExtractor.Tokens("SatinPlumesFirst"), Is.EqualTo(new[] { "Satin", "Plume" }));
        Assert.That(ExampleExtractor.Tokens("SuparnaSongBearing"), Is.EqualTo(new[] { "Suparna", "Song" }));
        Assert.That(ExampleExtractor.Tokens("FrictionTargets"), Is.EqualTo(new[] { "Friction" }));
    }
}
