using System.Collections.Generic;
using System.Linq;
using AnoMech.Learn;
using AnoMech.Learn.Toolbox;
using AnoMech.Learn.Tools;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Toolbox;

public class ToolboxOracleTests
{
    private static ToolboxObject Player(string slot, float x, float z) => new("player", slot, slot, x, z, 1f, 1f, 0f, "circle");

    private static DumpedOrder Order(int seed, string role, float time, float x, float z)
        => new("Ai", time, $"Ai.cs:{(int)time}", null, role, seed, time, null, x, z);

    private static readonly Lesson Lesson = new("S", "A", [], [
        new LessonStep(new LessonStepKey("Ai", Time: 10f), Title: "open", Tolerance: 2f),
        new LessonStep(new LessonStepKey("Ai", Time: 20f), Title: "spread", Tolerance: 1.5f),
    ]);

    private static readonly ToolboxPlan Plan = new("p", "plan", "me", "Naur", 1f, [
        new ToolboxStep(0, "Opener", "", [Player("tank1", 0f, -5f), Player("healer1", 0f, 5f)]),
        new ToolboxStep(1, "Spread", "", [Player("tank1", 10f, 0f), Player("healer1", -10f, 0f)]),
    ]);

    private static readonly LessonToolbox Link = new("p", new() { ["MainTank"] = "tank1", ["RegenHealer"] = "healer1" }, new() { ["Ai@10"] = 0, ["Ai@20"] = 1 });

    [Test]
    public void TheSeedClosestToThePlanIsTheExampleAndFarStepsDisagree()
    {
        // Seed 2 plays the plan's roll; seed 1 sends the tank the other way at the spread.
        var dump = new OrderDump("S", "A", 0, [
            Order(1, "MainTank", 10f, 0f, -5f), Order(1, "RegenHealer", 10f, 0f, 5f), Order(1, "MainTank", 20f, -10f, 0f), Order(1, "RegenHealer", 20f, 10f, 0f),
            Order(2, "MainTank", 10f, 0f, -5.5f), Order(2, "RegenHealer", 10f, 0f, 5f), Order(2, "MainTank", 20f, 10f, 0.5f), Order(2, "RegenHealer", 20f, -10f, 0f),
        ]);
        var report = ToolboxOracle.Check(Plan, Lesson, Link, dump);
        Assert.That(report.ExampleSeed, Is.EqualTo(2));
        Assert.That(report.Disagreements, Is.Empty);
        Assert.That(report.ResidualBySeed[1], Is.GreaterThan(report.ResidualBySeed[2]));
        Assert.That(report.Steps.Select(s => s.StepId), Is.EqualTo(new[] { "Ai@10", "Ai@20" }));
    }

    [Test]
    public void AStepNoSeedReproducesIsReportedWithTheRolesOff()
    {
        var dump = new OrderDump("S", "A", 0, [
            Order(1, "MainTank", 10f, 0f, -5f), Order(1, "RegenHealer", 10f, 0f, 5f), Order(1, "MainTank", 20f, 0f, 0f), Order(1, "RegenHealer", 20f, -10f, 0f),
        ]);
        var report = ToolboxOracle.Check(Plan, Lesson, Link, dump);
        var off = report.Disagreements.Single();
        Assert.That(off.StepId, Is.EqualTo("Ai@20"));
        Assert.That(off.ByRole["MainTank"], Is.EqualTo(10f).Within(0.01f));
        Assert.That(off.ByRole["RegenHealer"], Is.EqualTo(0f).Within(0.01f));
    }
}
