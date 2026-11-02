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
        Console.Error.WriteLine("usage: record|skeleton|rekey|check (see Cli.cs)");
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
}
