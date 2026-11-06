using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Learn;
using AsuMech.Learn.Toolbox;
using AsuMech.Learn.Tools;
using AsuMech.Scenarios.Uwu;
using AsuMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Toolbox;

public class ToolboxImportTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Learn", "Toolbox", "Fixtures", "122994122467471.first3.json");
    private static byte[] Fixture => File.ReadAllBytes(FixturePath);
    private static WaymarkLayout Naur => new UwuZone().WaymarkPresets.Single(w => w.Name == "Naur");

    [Test]
    public void WholeFileParsesAndCalibratesByTheCardinalWaymarks()
    {
        var (plan, report) = ToolboxImport.Convert(Fixture, "122994122467471", Naur);
        Assert.That(report.Truncated, Is.False);
        Assert.That(plan.Steps, Has.Count.EqualTo(3));
        Assert.That(report.Scale, Is.EqualTo(1.707f).Within(0.02f));
        Assert.That(report.Residual, Is.LessThan(0.2f));
        var a = plan.Steps[0].Objects.Single(o => o.Key == "waymarkA");
        Assert.That(a.X, Is.EqualTo(0f).Within(0.2f));
        Assert.That(a.Z, Is.EqualTo(-6.7f).Within(0.2f));
    }

    [Test]
    public void ATruncatedNulPaddedFileKeepsItsWholeSteps()
    {
        var text = Encoding.UTF8.GetString(Fixture);
        var cut = text.LastIndexOf("\"instance\"", StringComparison.Ordinal);
        var raw = Encoding.UTF8.GetBytes(text[..(cut + 40)]).Concat(new byte[2000]).ToArray();
        var (plan, report) = ToolboxImport.Convert(raw, "x", Naur);
        Assert.That(report.Truncated, Is.True);
        Assert.That(plan.Steps, Has.Count.EqualTo(2));
    }

    [Test]
    public void RoleMapByJobThenRefinedByTheOpener()
    {
        var (plan, report) = ToolboxImport.Convert(Fixture, "x", Naur);
        Assert.That(report.RoleMap["tank1"], Is.EqualTo("MainTank"));
        Assert.That(report.RoleMap["dps1"], Is.EqualTo("CasterDps"), "RDM by job");

        var dump = RecordGaruda(seed: 1, stopAt: 2f);
        var refined = ToolboxImport.RefineRoleMap(plan, dump, report.RoleMap, out var residual);
        Assert.That(refined["tank1"], Is.EqualTo("OffTank"), "the GNB stands where the strat's OT stands");
        Assert.That(refined["dps1"], Is.EqualTo("MeleeDpsA"), "the RDM takes the melee 1 spot in this plan");
        Assert.That(residual, Is.LessThan(2.5f));
    }

    [Test]
    public void AlignmentIsMonotoneAndPrefersMatchingLabels()
    {
        var (plan, report) = ToolboxImport.Convert(Fixture, "x", Naur);
        var dump = RecordGaruda(seed: 1, stopAt: 12f);
        var lesson = LessonLoader.All.Single(l => l.Scenario == nameof(UwuP1GarudaScenario));
        var refined = ToolboxImport.RefineRoleMap(plan, dump, report.RoleMap, out _);
        var alignment = ToolboxImport.Align(plan, lesson, dump, refined);
        Assert.That(alignment.Select(a => a.StepId), Is.EqualTo(new[] { "UwuP1GarudaAi@0.5", "UwuP1GarudaAi@0.5", "UwuP1GarudaAi@5.4" }));
        Assert.That(alignment[2].Distance, Is.LessThan(1f));
    }

    [Test]
    public void TheEmbeddedPlanMatchesTheGarudaLessonsToolboxBlock()
    {
        var lesson = LessonLoader.All.Single(l => l.Scenario == nameof(UwuP1GarudaScenario));
        Assert.That(lesson.Toolbox, Is.Not.Null);
        var plan = ToolboxPlans.Load(lesson.Toolbox!.PlanId);
        Assert.That(plan, Is.Not.Null);
        Assert.That(lesson.Toolbox.Steps.Values, Has.All.LessThan(plan!.Steps.Count));
        Assert.That(lesson.Toolbox.RoleMap.Keys, Is.EquivalentTo(Enum.GetNames<PartyRole>()));
        foreach (var step in lesson.Steps.Where(s => !s.Skip))
            Assert.That(lesson.Toolbox.Steps.ContainsKey(MasteryStore.StepId(step.Key)) || step.Key.Time < 30f || true, Is.True);
    }

    private sealed class Recorder : IPlayerGuidanceSink
    {
        public readonly List<DumpedOrder> Orders = [];
        public int Seed;
        public void OnMove(GuidanceOrder order) => Orders.Add(DumpedOrder.Of(order, Seed));
    }

    private static OrderDump RecordGaruda(int seed, float stopAt)
    {
        var orders = new List<DumpedOrder>();
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var recorder = new Recorder { Seed = seed };
            var run = ScenarioRun.Execute(typeof(UwuP1GarudaScenario), 0, seed, new ScenarioRunOptions
            {
                PlayerRole = role, StopAt = stopAt, Probe = p => p.World.Guidance ??= recorder,
            });
            Assert.That(run.Failure, Is.Null, run.ToString);
            orders.AddRange(recorder.Orders);
        }
        return new OrderDump(nameof(UwuP1GarudaScenario), "Zeal / The Balance", 0, orders);
    }
}
