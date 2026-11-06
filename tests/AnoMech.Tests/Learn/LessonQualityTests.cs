using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Learn;
using AnoMech.Learn.Toolbox;
using AnoMech.Learn.Tools;
using AnoMech.Scenarios;
using NUnit.Framework;

namespace AnoMech.Tests.Learn;

// The gates that keep the Learn content from regressing: no lesson disappears, step keys still
// point at the strat lines they were cut from, a toolbox plan still agrees with its strat about as
// well as when the lesson was authored, and pinning a roll never shifts another.
public class LessonQualityTests
{
    private static readonly ScenarioCatalog Catalog = ScenarioCatalog.Create();

    // The repo root, for the strat sources and the baseline files beside this test.
    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AnoMech.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("AnoMech.sln not found above the test binaries");
        }
    }

    private static string Baseline(string name) => Path.Combine(RepoRoot, "tests", "AnoMech.Tests", "Learn", name);

    [Test]
    public void NoLessonInTheBaselineDisappeared()
    {
        var baseline = File.ReadAllLines(Baseline("lessons-baseline.txt")).Where(l => l.Length > 0 && !l.StartsWith('#')).Select(l => l.Split('\t')).ToList();
        var missing = baseline.Where(b => !LessonLoader.All.Any(l => l.Scenario == b[0] && l.Strat == b[1])).Select(b => $"{b[0]} / {b[1]}").ToList();
        Assert.That(missing, Is.Empty, "a lesson in lessons-baseline.txt is gone; restore it or run `status --write-baseline` on purpose");
        Assert.That(baseline, Is.Not.Empty);
    }

    // The key's line in the strat source holds the call the step was cut from (its time literal);
    // a strat edit that moves lines shows up here, and `rekey` follows the lines.
    [Test]
    public void StepKeysStillPointAtTheirStratLines()
    {
        var sources = Directory.EnumerateFiles(Path.Combine(RepoRoot, "AnoMech", "Scenarios"), "*.cs", SearchOption.AllDirectories)
            .ToDictionary(Path.GetFileNameWithoutExtension, f => f);
        var stale = new List<string>();
        foreach (var lesson in LessonLoader.All)
        foreach (var step in lesson.Steps)
        {
            if (step.Key.Line is not { } line) continue;
            if (!sources.TryGetValue(step.Key.Cls, out var file)) { stale.Add($"{lesson.Scenario}: no source file named {step.Key.Cls}.cs"); continue; }
            var lines = File.ReadAllLines(file);
            if (line < 1 || line > lines.Length) { stale.Add($"{lesson.Scenario}: {step.Key.Cls}.cs has no line {line}"); continue; }
            var text = lines[line - 1];
            var time = step.Key.Time ?? step.Key.From;
            var literal = time?.ToString("0.##", CultureInfo.InvariantCulture);
            // A call whose time is computed (bait - 3f) cannot be checked for its literal; it still has to be a call.
            var holdsLiteral = time is { } && literal != null && System.Text.RegularExpressions.Regex.IsMatch(text, $@"(?<![\d.]){System.Text.RegularExpressions.Regex.Escape(literal)}(?![\d])");
            var isCall = System.Text.RegularExpressions.Regex.IsMatch(text, @"\b(Move|Automarker|MoveTo|Loop)\s*\(") || text.Contains("ai.", StringComparison.Ordinal);
            if (!holdsLiteral && !isCall)
                stale.Add($"{lesson.Scenario} / {lesson.Strat}: {step.Key.Cls}.cs:{line} no longer holds {literal} ({text.Trim()})");
        }
        Assert.That(stale, Is.Empty, "run: rekey-all --write");
    }

    public static IEnumerable<TestCaseData> LessonsWithAResidual()
        => LessonLoader.All.Where(l => l.Toolbox is { ExampleSeed: not null, Residual: not null })
            .Select(l => new TestCaseData(l.Scenario, l.Strat).SetName($"ToolboxStillAgrees({l.Scenario},{l.Strat})"));

    // The plan's example seed, every role: the mean residual over the aligned steps may not grow by
    // more than a metre past what the author saw.
    [TestCaseSource(nameof(LessonsWithAResidual))]
    public void ToolboxStillAgreesAsWellAsWhenAuthored(string scenarioName, string strat)
    {
        var scenario = Catalog.Scenarios.Single(s => s.GetType().Name == scenarioName);
        var stratIndex = scenario.AiStrats.ToList().FindIndex(a => a.Name == strat);
        var lesson = LessonLoader.For(scenario, scenario.AiStrats[stratIndex])!;
        var link = lesson.Toolbox!;
        var plan = ToolboxPlans.Load(link.PlanId);
        Assert.That(plan, Is.Not.Null, $"plan {link.PlanId} is not embedded");
        var seed = link.ExampleSeed!.Value;
        var orders = new List<DumpedOrder>();
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            var sink = new Sink(seed, orders);
            ScenarioRun.Execute(scenario.GetType(), stratIndex, seed, new ScenarioRunOptions
            {
                PlayerRole = role, WriteArtifactsOnFailure = false,
                Overrides = link.Example is { Count: > 0 } example ? o => ExampleOverrides.Apply(o, example) : null,
                Probe = p => p.World.Guidance ??= sink,
            });
        }
        var dump = new OrderDump(scenarioName, strat, stratIndex, orders);
        var report = ToolboxOracle.Check(plan!, lesson, link, dump);
        var mean = report.Steps.Count == 0 ? 0f : report.TotalResidual / report.Steps.Count;
        Assert.That(mean, Is.LessThanOrEqualTo(link.Residual!.Value + 1f), $"authored at {link.Residual:F1} m, now {mean:F1} m: {string.Join("; ", report.Disagreements.Select(d => $"{d.Label} {d.Residual:F1} m"))}");
    }

    private sealed class Sink(int seed, List<DumpedOrder> into) : IPlayerGuidanceSink
    {
        public void OnMove(GuidanceOrder order) => into.Add(DumpedOrder.Of(order, seed));
    }

    public static IEnumerable<TestCaseData> ScenariosWithOverrides()
        => Catalog.Scenarios.Where(s => s.AiStrats.Count > 0 && s.SettingsOverrides != null)
            .Select(s => new TestCaseData(s.GetType()).SetName($"PinsDoNotShiftOtherRolls({s.GetType().Name})"));

    // A State that short-circuits its draw on a pin shifts the seed's other rolls; the ones known to
    // (overrides-known-shifts.txt) wait for their owner, any other is a regression.
    [TestCaseSource(nameof(ScenariosWithOverrides))]
    public void PinsDoNotShiftOtherRolls(Type scenarioType)
    {
        var known = File.ReadAllLines(Baseline("overrides-known-shifts.txt")).Where(l => l.Length > 0 && !l.StartsWith('#')).Select(l => l.Split('\t')).ToList();
        var report = OverridesCheck.Run(scenarioType);
        var shifting = report.Rows.Where(r => r.Status == PinStatus.Shifts).Select(r => r.Roll).ToList();
        var knownHere = known.Where(k => k[0] == scenarioType.Name).Select(k => k[1]).ToList();
        Assert.That(shifting.Except(knownHere), Is.Empty, report.Table());
        Assert.That(knownHere.Except(shifting), Is.Empty, $"{scenarioType.Name}: a known shift is gone; drop it from overrides-known-shifts.txt");
    }

    [Test]
    public void MetricsPrint()
    {
        TestContext.Out.WriteLine(LearnStatus.Metrics(LessonLoader.All));
    }
}
