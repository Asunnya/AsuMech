using AnoMech.Learn;
using AnoMech.Learn.Tools;
using AnoMech.Learn.Toolbox;
using AnoMech.Learn.Mitigation;
using AnoMech.Core.Game;
using AnoMech.Scenarios;

namespace AnoMech.LessonTools;

// record   --scenario <TypeName> [--strat N] [--seeds 1,2,3] --out dump.json
// skeleton --dump dump.json --out lesson.json
// rekey    --lesson lesson.json --dump dump.json [--out lesson.json]
// check    --lesson lesson.json --dump dump.json
// toolbox-import --plan <id|file.json> --zone <ZoneType> [--layout <name>] --lesson lesson.json --dump dump.json --out-plan plans/<id>.json [--write]
// mit-timeline --scenario <TypeName> --out Timelines/<TypeName>.json         cast timeline from a headless run
// mit-import --plan <name> --sheet-id <google id> --tabs "Garuda=UwuP1GarudaScenario,Ifrit=UwuP2IfritScenario" --out Plans/<name>.json [--csv-dir dir]
//   a tab with sections: "The Ultima Weapon=DefaultScenario[Section A=ScenarioA;Section B=ScenarioB]"
// skeleton-all --out <dir> [--seeds 1] [--scenario <substring>] [--force]   every catalog strat without a lesson
// mine     --dump dump.json [--zone <ZoneType> --layout <name>]              which rolls pick each step's spot, per role
// cues     --dump dump.json [--window 8]                                     what preceded each order
// oracle   --plan plans/<id>.json --lesson lesson.json --dump dump.json [--write]   example seed + toolbox vs strat residuals
// author   --scenario <TypeName> [--strat N] [--seeds 1..8] --out <dir> [--plan <id|file> --zone <ZoneType> [--layout <name>]]
//          record -> skeleton -> mine -> cues [-> toolbox-import -> oracle] -> <dir>/<Scenario>.<strat>.context.md
// context  --dump dump.json --lesson lesson.json --out context.md [--plan plans/<id>.json] [--zone <ZoneType> --layout <name>]
//          the context file again from an existing dump (after a miner change or a lesson edit), no recording
internal static class Cli
{
    public static int Run(string[] args)
    {
        if (args.Length == 0) return Usage();
        var opts = Parse(args.Skip(1));
        try
        {
            return args[0] switch
            {
                "record" => Record(opts),
                "skeleton" => Skeleton(opts),
                "rekey" => Rekey(opts),
                "check" => Check(opts),
                "skeleton-all" => SkeletonAll(opts),
                "toolbox-import" => ToolboxImportCmd(opts),
                "mit-timeline" => MitTimeline(opts),
                "mit-import" => MitImport(opts),
                "mine" => Mine(opts),
                "cues" => Cues(opts),
                "oracle" => Oracle(opts),
                "author" => Author(opts),
                "context" => Context(opts),
                _ => Usage(),
            };
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            return 2;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("usage: author|record|skeleton|rekey|check|mine|cues|oracle|toolbox-import|mit-timeline|mit-import (see Cli.cs)");
        return 1;
    }

    private static Dictionary<string, string> Parse(IEnumerable<string> args)
    {
        var opts = new Dictionary<string, string>();
        string? key = null;
        foreach (var a in args)
        {
            if (a.StartsWith("--")) { key = a[2..]; opts[key] = ""; }
            else if (key != null) { opts[key] = a; key = null; }
        }
        return opts;
    }

    private static string Need(Dictionary<string, string> opts, string name)
        => opts.TryGetValue(name, out var v) && v.Length > 0 ? v : throw new ArgumentException($"--{name} is required");

    private static int Record(Dictionary<string, string> opts)
    {
        var scenario = Need(opts, "scenario");
        var strat = opts.TryGetValue("strat", out var s) ? int.Parse(s) : 0;
        var seeds = (opts.TryGetValue("seeds", out var sd) ? sd : "1,2,3").Split(',').Select(int.Parse).ToList();
        var dump = OrderRecorder.Record(scenario, strat, seeds, Console.Error);
        File.WriteAllText(Need(opts, "out"), dump.ToJson());
        Console.WriteLine($"{dump.Orders.Count} orders from {seeds.Count} seeds × 8 roles -> {opts["out"]}");
        return 0;
    }

    private static int Skeleton(Dictionary<string, string> opts)
    {
        var dump = OrderDump.Parse(File.ReadAllText(Need(opts, "dump")));
        var lesson = LessonSkeleton.Generate(dump);
        File.WriteAllText(Need(opts, "out"), LessonSkeleton.ToJson(lesson));
        Console.WriteLine($"{lesson.Steps.Count} steps -> {opts["out"]}; write a rule or skip for each.");
        return 0;
    }

    private static int Rekey(Dictionary<string, string> opts)
    {
        var path = Need(opts, "lesson");
        var lesson = LessonLoader.Parse(File.ReadAllText(path));
        var dump = OrderDump.Parse(File.ReadAllText(Need(opts, "dump")));
        var (rekeyed, orphans) = LessonSkeleton.Rekey(lesson, dump);
        File.WriteAllText(opts.TryGetValue("out", out var o) && o.Length > 0 ? o : path, LessonSkeleton.ToJson(rekeyed));
        foreach (var step in orphans) Console.WriteLine($"orphan: {step.Title} ({step.Key.Time ?? step.Key.From})");
        return orphans.Count == 0 ? 0 : 3;
    }

    // Deterministic part of a lesson for every strat in the catalog: dump + skeleton per strat,
    // laid out as Learn/Lessons expects. The text still has to be written.
    private static int SkeletonAll(Dictionary<string, string> opts)
    {
        var outDir = Need(opts, "out");
        var seeds = (opts.TryGetValue("seeds", out var sd) && sd.Length > 0 ? sd : "1").Split(',').Select(int.Parse).ToList();
        var filter = opts.TryGetValue("scenario", out var f) ? f : "";
        var force = opts.ContainsKey("force");
        var written = 0;
        foreach (var scenario in ScenarioCatalog.Create().Scenarios)
        {
            var typeName = scenario.GetType().Name;
            if (filter.Length > 0 && !typeName.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
            for (var i = 0; i < scenario.AiStrats.Count; i++)
            {
                var strat = scenario.AiStrats[i];
                if (!force && LessonLoader.For(scenario, strat) != null) continue;
                var family = scenario.GetType().Namespace!.Split('.')[2];
                var dir = Path.Combine(outDir, family);
                Directory.CreateDirectory(dir);
                var stem = Path.Combine(dir, $"{typeName.Replace("Scenario", "")}.{Slug(strat.Name)}");
                Console.Error.WriteLine($"{typeName} / {strat.Name}");
                var dump = OrderRecorder.Record(typeName, i, seeds, TextWriter.Null);
                File.WriteAllText(stem + ".dump.json", dump.ToJson());
                File.WriteAllText(stem + ".json", LessonSkeleton.ToJson(LessonSkeleton.Generate(dump)));
                written++;
            }
        }
        Console.WriteLine($"{written} skeleton(s) -> {outDir}");
        return 0;
    }

    private static int ToolboxImportCmd(Dictionary<string, string> opts)
    {
        var planArg = Need(opts, "plan");
        var id = Path.GetFileName(planArg).Split('.').FirstOrDefault(p => p.All(char.IsDigit) && p.Length >= 6) ?? planArg;
        byte[] raw;
        if (File.Exists(planArg)) raw = File.ReadAllBytes(planArg);
        else
        {
            using var http = new HttpClient();
            raw = http.GetByteArrayAsync($"https://ff14.toolboxgaming.space/strats/{id}.json").GetAwaiter().GetResult();
        }
        var zoneName = Need(opts, "zone");
        var zone = ScenarioCatalog.Create().Zones.FirstOrDefault(z => z.GetType().Name == zoneName)
                   ?? throw new ArgumentException($"no zone type named {zoneName}");
        var layout = opts.TryGetValue("layout", out var l) && l.Length > 0
            ? zone.WaymarkPresets.FirstOrDefault(w => w.Name.Equals(l, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException($"no layout {l}")
            : zone.WaymarkPresets[0];
        var (plan, report) = ToolboxImport.Convert(raw, id, layout);
        Console.WriteLine($"plan {id} \"{plan.Label}\" by {plan.Author}: {report.StepsRecovered} steps{(report.Truncated ? " (recovered from a truncated file)" : "")}");
        Console.WriteLine($"calibration: {report.Scale:F3} yalm/unit against layout {layout.Name}, residual {report.Residual:F2} m");
        Console.WriteLine($"roles: {string.Join(", ", report.RoleMap.Select(kv => $"{kv.Key}={kv.Value}"))}");
        foreach (var w in report.Warnings) Console.WriteLine($"warning: {w}");

        var outPlan = Need(opts, "out-plan");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPlan))!);
        File.WriteAllText(outPlan, plan.ToJson());
        Console.WriteLine($"compact plan -> {outPlan} ({new FileInfo(outPlan).Length / 1024} KiB)");

        if (!opts.TryGetValue("lesson", out var lessonPath) || !opts.TryGetValue("dump", out var dumpPath)) return 0;
        var lesson = LessonLoader.Parse(File.ReadAllText(lessonPath));
        var dump = OrderDump.Parse(File.ReadAllText(dumpPath));
        var refined = ToolboxImport.RefineRoleMap(plan, dump, report.RoleMap, out var openerResidual);
        if (!refined.OrderBy(kv => kv.Key).SequenceEqual(report.RoleMap.OrderBy(kv => kv.Key)))
            Console.WriteLine($"roles by opener: {string.Join(", ", refined.Select(kv => $"{kv.Key}={kv.Value}"))} (mean {openerResidual:F1} m; confirm against the plan's notes)");
        var alignment = ToolboxImport.Align(plan, lesson, dump, refined);
        var steps = new Dictionary<string, int>();
        foreach (var (tb, stepId, distance) in alignment)
        {
            var label = plan.Steps[tb].Label;
            Console.WriteLine(stepId is null ? $"  {tb,2} {label,-40} unmatched ({distance:F1} m)" : $"  {tb,2} {label,-40} -> {stepId} ({distance:F1} m)");
            if (stepId != null) steps.TryAdd(stepId, tb);
        }
        var roleMap = refined.ToDictionary(kv => kv.Value, kv => kv.Key);
        var linked = lesson with { Toolbox = new LessonToolbox(id, roleMap, steps) };
        if (opts.ContainsKey("write"))
        {
            File.WriteAllText(lessonPath, LessonSkeleton.ToJson(linked));
            Console.WriteLine($"toolbox block written to {lessonPath}");
        }
        else Console.WriteLine("(add --write to store the toolbox block in the lesson)");
        return 0;
    }

    private static int MitTimeline(Dictionary<string, string> opts)
    {
        var scenario = Need(opts, "scenario");
        var type = typeof(Game).Assembly.GetTypes().FirstOrDefault(t => t.Name == scenario) ?? throw new ArgumentException($"no scenario type named {scenario}");
        new AnoMech.Tests.DalamudServices().InstallNullServices();
        var csvPath = Path.GetTempFileName();
        var run = AnoMech.Tests.ScenarioRun.Execute(type, 0, 1, new AnoMech.Tests.ScenarioRunOptions { TimelinePath = csvPath, WriteArtifactsOnFailure = false });
        if (run.Failure != null) Console.Error.WriteLine($"run: {run.Failure}");
        var timeline = CastTimeline.FromTimelineCsv(type.Name, File.ReadAllText(csvPath));
        File.Delete(csvPath);
        var outPath = Need(opts, "out");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, timeline.ToJson());
        Console.WriteLine($"{timeline.Casts.Count} casts ({string.Join(", ", timeline.Names)}) -> {outPath}");
        return 0;
    }

    // Each tab is fetched as CSV from the sheet (or read from --csv-dir/<Tab>.csv) and anchored on
    // the embedded timeline of its scenario. Everything the importer couldn't read is listed; a plan
    // with leftovers is written anyway so the aliases can be grown and the import rerun.
    private static int MitImport(Dictionary<string, string> opts)
    {
        var name = Need(opts, "plan");
        var tabs = Need(opts, "tabs").Split(',').Select(t => t.Split('=', 2)).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
        var sheetId = opts.TryGetValue("sheet-id", out var sid) ? sid : "";
        var csvDir = opts.TryGetValue("csv-dir", out var dir) ? dir : "";
        var title = opts.TryGetValue("title", out var t) && t.Length > 0 ? t : name;
        var credits = (opts.TryGetValue("credits", out var c) ? c : "").Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        var scenarios = new Dictionary<string, MitScenario>();
        var unresolved = new List<string>();
        using var http = new HttpClient();
        foreach (var (tab, scenarioType) in tabs)
        {
            var csvPath = Path.Combine(csvDir, tab.Replace(' ', '_') + ".csv");
            string csv;
            if (csvDir.Length > 0 && File.Exists(csvPath)) csv = File.ReadAllText(csvPath);
            else if (sheetId.Length > 0)
                csv = http.GetStringAsync($"https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq?tqx=out:csv&sheet={Uri.EscapeDataString(tab)}").GetAwaiter().GetResult();
            else throw new ArgumentException($"no CSV for tab {tab}: pass --sheet-id or --csv-dir");
            var bracket = scenarioType.IndexOf('[');
            if (bracket > 0)
            {
                var defaultScenario = scenarioType[..bracket].Trim();
                var sections = scenarioType[(bracket + 1)..].TrimEnd(']').Split(';').Select(s => s.Split('=', 2)).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
                foreach (var (scenario, imported) in MitSheetImport.ImportSections(csv, defaultScenario, sections, MitTimelines.Load, MitAliases.Default))
                {
                    scenarios[scenario] = imported;
                    Console.WriteLine($"{tab} -> {scenario}: {imported.Mechanics.Count} mechanics, {imported.Mechanics.Count(m => m.Anchor != null)} anchored");
                }
                continue;
            }
            var timeline = MitTimelines.Load(scenarioType) ?? throw new ArgumentException($"no embedded timeline for {scenarioType}; run mit-timeline first");
            var scenario1 = MitSheetImport.Import(csv, timeline, MitAliases.Default);
            scenarios[scenarioType] = scenario1;
            Console.WriteLine($"{tab} -> {scenarioType}: {scenario1.Mechanics.Count} mechanics, {scenario1.Mechanics.Count(m => m.Anchor != null)} anchored");
        }
        var plan = new MitPlan(name, new MitSource(title, sheetId.Length > 0 ? $"https://docs.google.com/spreadsheets/d/{sheetId}" : "", credits, DateTime.UtcNow.ToString("yyyy-MM-dd")), scenarios);
        foreach (var u in plan.UnresolvedTexts()) Console.WriteLine($"unresolved: {u}");
        var outPath = Need(opts, "out");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, plan.ToJson());
        Console.WriteLine($"plan -> {outPath}; {plan.UnresolvedTexts().Count()} unresolved");
        return 0;
    }

    private static string Slug(string name)
        => string.Join('-', name.ToLowerInvariant().Split(Path.GetInvalidFileNameChars().Concat([' ', '/', '(', ')', ',', '.']).ToArray(), StringSplitOptions.RemoveEmptyEntries));

    private static int Check(Dictionary<string, string> opts)
    {
        var lesson = LessonLoader.Parse(File.ReadAllText(Need(opts, "lesson")));
        var dump = OrderDump.Parse(File.ReadAllText(Need(opts, "dump")));
        var errors = LessonLoader.Validate(lesson, ScenarioCatalog.Create().Scenarios).ToList();
        var (uncovered, untriggered) = LessonSkeleton.Coverage(lesson, dump);
        errors.AddRange(uncovered.Select(o => $"no step for {o.Cls} @{o.Time} ({o.Source})"));
        errors.AddRange(untriggered.Select(s => $"nothing triggers step \"{s.Title}\" ({s.Key.Time ?? s.Key.From})"));
        foreach (var e in errors) Console.WriteLine(e);
        Console.WriteLine(errors.Count == 0 ? "ok" : $"{errors.Count} problem(s)");
        return errors.Count == 0 ? 0 : 3;
    }

    private static List<int> Seeds(Dictionary<string, string> opts, string fallback)
    {
        var text = opts.TryGetValue("seeds", out var sd) && sd.Length > 0 ? sd : fallback;
        var range = text.Split("..");
        return range.Length == 2 ? Enumerable.Range(int.Parse(range[0]), int.Parse(range[1]) - int.Parse(range[0]) + 1).ToList() : text.Split(',').Select(int.Parse).ToList();
    }

    private static WaymarkLayout? Layout(Dictionary<string, string> opts, IZone? zone = null)
    {
        if (zone is null)
        {
            if (!opts.TryGetValue("zone", out var zoneName) || zoneName.Length == 0) return null;
            zone = ScenarioCatalog.Create().Zones.FirstOrDefault(z => z.GetType().Name == zoneName) ?? throw new ArgumentException($"no zone type named {zoneName}");
        }
        return opts.TryGetValue("layout", out var l) && l.Length > 0
            ? zone.WaymarkPresets.FirstOrDefault(w => w.Name.Equals(l, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException($"no layout {l}")
            : zone.WaymarkPresets.FirstOrDefault();
    }

    private static int Mine(Dictionary<string, string> opts)
    {
        var dump = OrderDump.Parse(File.ReadAllText(Need(opts, "dump")));
        var rules = RuleMiner.Mine(dump, Layout(opts));
        foreach (var r in rules)
        {
            var spots = r.Fixed ? r.Spots[0].Label
                : r.Fields.Count > 0 ? $"by {string.Join(" & ", r.Fields)}: {string.Join("; ", r.SpotByValue.Select(kv => $"{kv.Key} -> {kv.Value}"))}"
                : $"unexplained: {string.Join("; ", r.Spots.Select(s => $"{s.Label} (seeds {string.Join(",", s.Seeds)})"))}";
            Console.WriteLine($"{r.Cls}@{r.Time:0.##} L{r.Line} {r.Role,-14} {spots}");
        }
        return 0;
    }

    private static int Cues(Dictionary<string, string> opts)
    {
        var dump = OrderDump.Parse(File.ReadAllText(Need(opts, "dump")));
        var window = opts.TryGetValue("window", out var w) && w.Length > 0 ? float.Parse(w, System.Globalization.CultureInfo.InvariantCulture) : CueExtractor.WindowSeconds;
        foreach (var ((cls, line, role), list) in CueExtractor.Extract(dump, window).OrderBy(kv => kv.Key.Line))
        {
            Console.WriteLine($"{cls} L{line} {role}");
            foreach (var c in list.Take(6))
                Console.WriteLine($"   {c.Kind} {c.Name}{(c.Target is { } t ? " on " + t : "")} {c.SecondsBefore:0.0}s before{(c.Stable ? "" : $" ({c.Seeds}/{c.OfSeeds})")}");
        }
        return 0;
    }

    private static int Oracle(Dictionary<string, string> opts)
    {
        var plan = ToolboxPlan.Parse(File.ReadAllText(Need(opts, "plan")));
        var lessonPath = Need(opts, "lesson");
        var lesson = LessonLoader.Parse(File.ReadAllText(lessonPath));
        var dump = OrderDump.Parse(File.ReadAllText(Need(opts, "dump")));
        if (lesson.Toolbox is not { } link) throw new ArgumentException("the lesson has no toolbox block; run toolbox-import --write first");
        var report = ToolboxOracle.Check(plan, lesson, link, dump);
        PrintOracle(report);
        if (opts.ContainsKey("write"))
        {
            File.WriteAllText(lessonPath, LessonSkeleton.ToJson(lesson with { Toolbox = link with { ExampleSeed = report.ExampleSeed } }));
            Console.WriteLine($"exampleSeed {report.ExampleSeed} written to {lessonPath}");
        }
        return report.Disagreements.Any() ? 1 : 0;
    }

    private static void PrintOracle(OracleReport report)
    {
        Console.WriteLine($"example seed {report.ExampleSeed}: {string.Join(", ", report.ResidualBySeed.OrderBy(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value:F1}"))} (total m)");
        foreach (var s in report.Steps)
            Console.WriteLine($"  {s.ToolboxStep,2} {s.Label,-40} {s.StepId,-28} {s.Residual,5:F1} m {(s.Agrees ? "" : "DISAGREES: " + string.Join(", ", s.RolesOff.Select(kv => $"{kv.Key} {kv.Value:F1}")))}");
    }

    // The whole deterministic chain for one strat; the context file is what the author reads.
    private static int Author(Dictionary<string, string> opts)
    {
        var scenario = Need(opts, "scenario");
        var strat = opts.TryGetValue("strat", out var st) && st.Length > 0 ? int.Parse(st) : 0;
        var outDir = Need(opts, "out");
        Directory.CreateDirectory(outDir);
        var dump = OrderRecorder.Record(scenario, strat, Seeds(opts, "1..8"), Console.Error);
        var slug = $"{dump.Scenario}.{strat}";
        var dumpPath = Path.Combine(outDir, $"{slug}.dump.json");
        File.WriteAllText(dumpPath, dump.ToJson());
        var lesson = LessonSkeleton.Generate(dump);
        var lessonPath = Path.Combine(outDir, $"{slug}.lesson.json");

        var type = typeof(Game).Assembly.GetTypes().First(t => t.Name == dump.Scenario);
        var catalogScenario = ScenarioCatalog.Create().Scenarios.FirstOrDefault(s => s.GetType() == type);
        var zone = catalogScenario?.Phase.Zone;
        var layout = Layout(opts, zone);
        var rules = RuleMiner.Mine(dump, layout);
        var cues = CueExtractor.Extract(dump);

        ToolboxPlan? plan = null;
        OracleReport? oracle = null;
        if (opts.TryGetValue("plan", out var planArg) && planArg.Length > 0 && layout != null)
        {
            var id = Path.GetFileName(planArg).Split('.').FirstOrDefault(p => p.All(char.IsDigit) && p.Length >= 6) ?? planArg;
            byte[] raw;
            if (File.Exists(planArg)) raw = File.ReadAllBytes(planArg);
            else
            {
                using var http = new HttpClient();
                raw = http.GetByteArrayAsync($"https://ff14.toolboxgaming.space/strats/{id}.json").GetAwaiter().GetResult();
            }
            var (converted, report) = ToolboxImport.Convert(raw, id, layout);
            plan = converted;
            File.WriteAllText(Path.Combine(outDir, $"{id}.json"), plan.ToJson());
            var refined = ToolboxImport.RefineRoleMap(plan, dump, report.RoleMap, out _);
            var steps = new Dictionary<string, int>();
            foreach (var (tb, stepId, _) in ToolboxImport.Align(plan, lesson, dump, refined))
                if (stepId != null) steps.TryAdd(stepId, tb);
            var link = new LessonToolbox(id, refined.ToDictionary(kv => kv.Value, kv => kv.Key), steps);
            oracle = ToolboxOracle.Check(plan, lesson, link, dump);
            lesson = lesson with { Toolbox = link with { ExampleSeed = oracle.ExampleSeed } };
            Console.Error.WriteLine($"toolbox {id}: {plan.Steps.Count} steps, {steps.Count} aligned, example seed {oracle.ExampleSeed}, {oracle.Disagreements.Count()} disagree");
        }
        File.WriteAllText(lessonPath, LessonSkeleton.ToJson(lesson));
        var contextPath = Path.Combine(outDir, $"{slug}.context.md");
        File.WriteAllText(contextPath, LessonContext.Write(dump, lesson, rules, cues, oracle, plan, layout));
        Console.WriteLine($"dump     {dumpPath}");
        Console.WriteLine($"skeleton {lessonPath}");
        Console.WriteLine($"context  {contextPath}");
        return 0;
    }

    private static int Context(Dictionary<string, string> opts)
    {
        var dump = OrderDump.Parse(File.ReadAllText(Need(opts, "dump")));
        var lesson = LessonLoader.Parse(File.ReadAllText(Need(opts, "lesson")));
        var type = typeof(Game).Assembly.GetTypes().FirstOrDefault(t => t.Name == dump.Scenario);
        var zone = ScenarioCatalog.Create().Scenarios.FirstOrDefault(s => s.GetType() == type)?.Phase.Zone;
        var layout = Layout(opts, zone);
        var plan = opts.TryGetValue("plan", out var planPath) && planPath.Length > 0 ? ToolboxPlan.Parse(File.ReadAllText(planPath)) : null;
        var oracle = plan != null && lesson.Toolbox is { } link ? ToolboxOracle.Check(plan, lesson, link, dump) : null;
        var outPath = Need(opts, "out");
        File.WriteAllText(outPath, LessonContext.Write(dump, lesson, RuleMiner.Mine(dump, layout), CueExtractor.Extract(dump), oracle, plan, layout));
        Console.WriteLine($"context  {outPath}");
        return 0;
    }
}
