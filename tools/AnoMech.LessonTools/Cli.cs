using AnoMech.Learn;
using AnoMech.Learn.Tools;
using AnoMech.Learn.Toolbox;
using AnoMech.Learn.Mitigation;
using AnoMech.Learn.Sources;
using AnoMech.Core.Game;
using AnoMech.Scenarios;
using System.Text.Json;

namespace AnoMech.LessonTools;

// record   --scenario <TypeName> [--strat N] [--seeds 1,2,3] --out dump.json [--example "Prop=value;Prop=value" | --lesson lesson.json]   --example/--lesson pin the rolls
// skeleton --dump dump.json --out lesson.json
// rekey    --lesson lesson.json --dump dump.json [--out lesson.json]
// check    --lesson lesson.json --dump dump.json
// toolbox-import --plan <id|file.json> --zone <ZoneType> [--layout <name>] --lesson lesson.json --dump dump.json --out-plan plans/<id>.json [--write]
// mit-timeline --scenario <TypeName> --out Timelines/<TypeName>.json         cast timeline from a headless run
// mit-import --plan <name> --sheet-id <google id> --tabs "Garuda=UwuP1GarudaScenario,Ifrit=UwuP2IfritScenario" --out Plans/<name>.json [--csv-dir dir]
//   a tab with sections: "The Ultima Weapon=DefaultScenario[Section A=ScenarioA;Section B=ScenarioB]"
//   one exported CSV instead of a sheet: --csv <file> --scenario <TypeName or TypeName[sections]> (--tabs not needed)
// skeleton-all --out <dir> [--seeds 1] [--scenario <substring>] [--force]   every catalog strat without a lesson
// mine     --dump dump.json [--zone <ZoneType> --layout <name>]              which rolls pick each step's spot, per role
// cues     --dump dump.json [--window 8]                                     what preceded each order
// oracle   --plan plans/<id>.json --lesson lesson.json --dump dump.json [--write] [--pinned-dump dump.json]   example seed + toolbox vs strat residuals; the pinned dump scores the lesson's example
// author   --scenario <TypeName> [--strat N] [--seeds 1..8] --out <dir> [--plan <id|url|file> [--layout <name>]] [--lesson existing.json]   --lesson lends its toolbox example
//          record -> skeleton -> mine -> cues [-> toolbox-import -> oracle] -> <dir>/<Scenario>.<strat>.context.md
// context  --dump dump.json --lesson lesson.json --out context.md [--plan plans/<id>.json] [--zone <ZoneType> --layout <name>]
// draft    --dump dump.json --lesson lesson.json --out lesson.json [--zone <ZoneType> --layout <name>]   rule/cue/roles/tolerance/skip from the mined data
// author-all --out <dir> [--seeds 1..12] [--scenario <substring>|--family <Family>] [--layout <name>] [--plans "UwuP2IfritScenario=562530446784261,..."] [--force]
//          author (with draft) for every catalog strat without an embedded lesson, in the Lessons layout; plans and layouts
//          come from Learn/Toolbox/plans.json unless --plans/--layout say otherwise
//          context: the context file again from an existing dump (after a miner change or a lesson edit), no recording
// wtfdig-fetch --fight umad [--path src/routes/ultimates/umad] [--branch main] --out AnoMech/Learn/Sources/wtfdig/umad.json   data.ts (+ mits.ts) -> JSON, no code run
// mit-import --plan <name> --wtfdig umad --mit-plan ikuya --tabs "p1=UmadP1TeleTrouncingScenario,p2=UmadP2ForsakenScenario" --out Plans/<name>.json
// slides-fetch --deck <google id|url|file.pptx> --out <dir> [--zone <ZoneType> [--layout <name>]]   text, notes and images per slide; XIVPlan links found become plans
// learn-ready --scenario <TypeName> [--strat N] [--no-run]   what the chain can read from the scenario and how it degrades
// status [--family <Family>]                                 scenario × strat × lesson × toolbox × example × mit × draft
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
                "draft" => Draft(opts),
                "author-all" => AuthorAll(opts),
                "learn-ready" => LearnReady(opts),
                "wtfdig-fetch" => WtfdigFetch(opts),
                "slides-fetch" => SlidesFetch(opts),
                "status" => Status(opts),
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
        Console.Error.WriteLine("usage: learn-ready|status|author|author-all|draft|context|record|skeleton|rekey|check|mine|cues|oracle|toolbox-import|mit-timeline|mit-import (see Cli.cs)");
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
        var seeds = Seeds(opts, "1,2,3");
        var dump = OrderRecorder.Record(scenario, strat, seeds, Console.Error, Example(opts));
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

    // A saved toolbox file, a bare toolbox id, or a plan link of a known host -> (id, raw toolbox JSON).
    private static (string Id, byte[] Raw) FetchPlan(string planArg)
    {
        if (File.Exists(planArg))
            return (Path.GetFileName(planArg).Split('.').FirstOrDefault(p => p.All(char.IsDigit) && p.Length >= 6) ?? Path.GetFileNameWithoutExtension(planArg), File.ReadAllBytes(planArg));
        var plan = PlanRef.Parse(planArg);
        using var http = new HttpClient();
        switch (plan.Host)
        {
            case RaidplanHost.Toolbox:
                return (plan.Id, http.GetByteArrayAsync($"https://ff14.toolboxgaming.space/strats/{plan.Id}.json").GetAwaiter().GetResult());
            case RaidplanHost.RaidplanIo:
                using (var api = JsonDocument.Parse(http.GetStringAsync($"https://raidplan.io/api/plan/{plan.Id}").GetAwaiter().GetResult()))
                {
                    var docUrl = api.RootElement.GetProperty("plan").GetProperty("doc_url").GetString() ?? throw new InvalidOperationException("api/plan has no doc_url");
                    return (plan.Id, http.GetByteArrayAsync(docUrl).GetAwaiter().GetResult());
                }
            case RaidplanHost.Xivplan:
                return (plan.Id, System.Text.Encoding.UTF8.GetBytes(plan.Url));
            case RaidplanHost.Stgy when plan.Url.Contains("[stgy:", StringComparison.Ordinal):
                return (plan.Id, System.Text.Encoding.UTF8.GetBytes(plan.Url));
            case RaidplanHost.Stgy:
            {
                // "<fight>/<id>" or "strategy/<id>" fetches one strategy; "<fight>" alone the popular feed.
                var parts = plan.Id.Split('/');
                var url = parts.Length == 2 ? $"https://api.ffxivstrats.io/strategy/{parts[1]}"
                    : $"https://api.ffxivstrats.io/fights/{parts[0]}/feed?type=strategy&sort=popular&offset=0&limit=20";
                return ($"stgy-{plan.Id.Replace('/', '-')}", http.GetByteArrayAsync(url).GetAwaiter().GetResult());
            }
            default:
                throw new ArgumentException($"no importer for {planArg}");
        }
    }

    private static IRaidplanProvider ProviderFor(string planArg)
        => Raidplans.For(!File.Exists(planArg) ? PlanRef.Parse(planArg).Host
            : planArg.Contains("raidplan", StringComparison.OrdinalIgnoreCase) ? RaidplanHost.RaidplanIo
            : planArg.Contains("stgy", StringComparison.OrdinalIgnoreCase) ? RaidplanHost.Stgy
            : planArg.EndsWith(".xivplan", StringComparison.OrdinalIgnoreCase) || planArg.Contains("xivplan", StringComparison.OrdinalIgnoreCase) ? RaidplanHost.Xivplan : RaidplanHost.Toolbox);

    private static int LearnReady(Dictionary<string, string> opts)
    {
        var name = Need(opts, "scenario");
        var scenario = ScenarioCatalog.Create().Scenarios.FirstOrDefault(s => s.GetType().Name == name) ?? throw new ArgumentException($"no scenario type named {name}");
        var strat = opts.TryGetValue("strat", out var st) && st.Length > 0 ? int.Parse(st) : 0;
        var dump = opts.ContainsKey("no-run") || scenario.AiStrats.Count == 0 ? null : OrderRecorder.Record(name, strat, [1], TextWriter.Null);
        var rows = LearnReadiness.Check(scenario, dump);
        Console.Write(LearnReadiness.Table(scenario, rows));
        return rows.Any(r => r.Status == Readiness.Missing) ? 1 : 0;
    }

    private static int Status(Dictionary<string, string> opts)
    {
        var rows = LearnStatus.Rows(ScenarioCatalog.Create());
        if (opts.TryGetValue("family", out var family) && family.Length > 0) rows = rows.Where(r => r.Family.Equals(family, StringComparison.OrdinalIgnoreCase)).ToList();
        Console.Write(LearnStatus.Table(rows));
        return 0;
    }

    private static int ToolboxImportCmd(Dictionary<string, string> opts)
    {
        var planArg = Need(opts, "plan");
        var (id, raw) = FetchPlan(planArg);
        var provider = ProviderFor(planArg);
        var zoneName = Need(opts, "zone");
        var zone = ScenarioCatalog.Create().Zones.FirstOrDefault(z => z.GetType().Name == zoneName)
                   ?? throw new ArgumentException($"no zone type named {zoneName}");
        var layout = opts.TryGetValue("layout", out var l) && l.Length > 0
            ? zone.WaymarkPresets.FirstOrDefault(w => w.Name.Equals(l, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException($"no layout {l}")
            : zone.WaymarkPresets[0];
        var (plan, report) = provider.Convert(raw, id, layout);
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
        if (opts.TryGetValue("wtfdig", out var fight) && fight.Length > 0) return MitImportWtfdig(opts, name, fight);
        var csvFile = opts.TryGetValue("csv", out var cf) && cf.Length > 0 ? cf : null;
        var tabs = csvFile != null
            ? new Dictionary<string, string> { [Path.GetFileNameWithoutExtension(csvFile)] = Need(opts, "scenario") }
            : Need(opts, "tabs").Split(',').Select(t => t.Split('=', 2)).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
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
            if (csvFile != null) csv = File.ReadAllText(csvFile);
            else if (csvDir.Length > 0 && File.Exists(csvPath)) csv = File.ReadAllText(csvPath);
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
        var sourceUrl = opts.TryGetValue("url", out var sourceArg) && sourceArg.Length > 0 ? sourceArg : sheetId.Length > 0 ? $"https://docs.google.com/spreadsheets/d/{sheetId}" : "";
        var plan = new MitPlan(name, new MitSource(title, sourceUrl, credits, DateTime.UtcNow.ToString("yyyy-MM-dd")), scenarios);
        foreach (var u in plan.UnresolvedTexts()) Console.WriteLine($"unresolved: {u}");
        var outPath = Need(opts, "out");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, plan.ToJson());
        Console.WriteLine($"plan -> {outPath}; {plan.UnresolvedTexts().Count()} unresolved");
        return 0;
    }

    // WTFDIG's mits.ts, already converted (wtfdig-fetch), rendered as the sheet grid per phase tag and
    // anchored on each scenario's timeline like any sheet.
    private static int MitImportWtfdig(Dictionary<string, string> opts, string name, string fight)
    {
        var source = WtfdigSource.Load(fight) ?? throw new ArgumentException($"no embedded WTFDIG source for {fight}; run wtfdig-fetch first");
        var planName = Need(opts, "mit-plan");
        if (source.Mits[planName] is null) throw new ArgumentException($"{fight} has no mit plan {planName}; it has {string.Join(", ", source.Mits.Select(kv => kv.Key))}");
        var tabs = Need(opts, "tabs").Split(',').Select(t => t.Split('=', 2)).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
        var scenarios = new Dictionary<string, MitScenario>();
        foreach (var (tag, scenarioType) in tabs)
        {
            var timeline = MitTimelines.Load(scenarioType) ?? throw new ArgumentException($"no embedded timeline for {scenarioType}; run mit-timeline first");
            var csv = WtfdigSource.GridToCsv(source.MitGrid(planName, tag));
            var scenario = MitSheetImport.Import(csv, timeline, MitAliases.Default);
            scenarios[scenarioType] = scenario;
            Console.WriteLine($"{tag} -> {scenarioType}: {scenario.Mechanics.Count} mechanics, {scenario.Mechanics.Count(m => m.Anchor != null)} anchored");
        }
        var label = source.Mits[planName]!["label"]?.GetValue<string>() ?? planName;
        var url = source.Mits[planName]!["url"]?.GetValue<string>() ?? source.Url;
        var plan = new MitPlan(name, new MitSource(label, url, [$"WTFDIG ({source.Url}, MIT)"], DateTime.UtcNow.ToString("yyyy-MM-dd")), scenarios);
        foreach (var u in plan.UnresolvedTexts()) Console.WriteLine($"unresolved: {u}");
        var outPath = Need(opts, "out");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, plan.ToJson());
        Console.WriteLine($"plan -> {outPath}; {plan.UnresolvedTexts().Count()} unresolved");
        return 0;
    }

    // A public Google Slides deck exported as PPTX (no login), split per slide; a slide whose text or
    // notes carries an XIVPlan link gets that scene imported as a plan when a zone is given.
    private static int SlidesFetch(Dictionary<string, string> opts)
    {
        var deck = Need(opts, "deck");
        var outDir = Need(opts, "out");
        Directory.CreateDirectory(outDir);
        byte[] pptx;
        if (File.Exists(deck)) pptx = File.ReadAllBytes(deck);
        else
        {
            using var http = new HttpClient();
            pptx = http.GetByteArrayAsync(PptxDeck.ExportUrl(deck)).GetAwaiter().GetResult();
            File.WriteAllBytes(Path.Combine(outDir, "deck.pptx"), pptx);
        }
        var slides = PptxDeck.Read(pptx);
        var layout = Layout(opts);
        var plans = 0;
        foreach (var slide in slides)
        {
            var stem = Path.Combine(outDir, $"slide{slide.Number:00}");
            File.WriteAllText(stem + ".txt", $"# {slide.Title}\n\n{string.Join("\n", slide.Texts)}\n\n## notes\n{slide.Notes}\n\n## links\n{string.Join("\n", slide.Links)}\n");
            foreach (var (name, bytes) in slide.Images) File.WriteAllBytes($"{stem}.{name}", bytes);
            var xivplan = slide.Links.FirstOrDefault(XivplanProvider.Looks);
            Console.WriteLine($"slide {slide.Number,2}: {slide.Title,-40} {slide.Texts.Count} texts, {slide.Images.Count} images{(xivplan != null ? ", XIVPlan link" : "")}");
            if (xivplan is null || layout is null) continue;
            var plan = PlanRef.Parse(xivplan);
            var (converted, report) = Raidplans.Convert(plan, System.Text.Encoding.UTF8.GetBytes(xivplan), layout);
            File.WriteAllText(stem + ".plan.json", converted.ToJson());
            foreach (var w in report.Warnings) Console.WriteLine($"   warning: {w}");
            plans++;
        }
        Console.WriteLine($"{slides.Count} slides -> {outDir}{(layout is null ? " (pass --zone to import XIVPlan links)" : $", {plans} plan(s)")}");
        return 0;
    }

    private static int WtfdigFetch(Dictionary<string, string> opts)
    {
        var fight = Need(opts, "fight");
        var path = opts.TryGetValue("path", out var p) && p.Length > 0 ? p.Trim('/') : $"src/routes/ultimates/{fight}";
        var branch = opts.TryGetValue("branch", out var b) && b.Length > 0 ? b : "main";
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AnoMech.LessonTools");
        var commit = "";
        try
        {
            using var doc = JsonDocument.Parse(http.GetStringAsync($"https://api.github.com/repos/mczub/wtfdig/commits/{branch}").GetAwaiter().GetResult());
            commit = doc.RootElement.GetProperty("sha").GetString() ?? "";
        }
        catch (HttpRequestException e) { Console.Error.WriteLine($"commit lookup failed: {e.Message}"); }
        var raw = $"https://raw.githubusercontent.com/mczub/wtfdig/{(commit.Length > 0 ? commit : branch)}/{path}";
        var data = http.GetStringAsync($"{raw}/data.ts").GetAwaiter().GetResult();
        string? mits = null;
        try { mits = http.GetStringAsync($"{raw}/mits.ts").GetAwaiter().GetResult(); }
        catch (HttpRequestException) { }
        var json = WtfdigSource.Convert(fight, data, mits, commit, $"https://github.com/mczub/wtfdig/tree/{(commit.Length > 0 ? commit[..7] : branch)}/{path}");
        var outPath = opts.TryGetValue("out", out var o) && o.Length > 0 ? o : $"AnoMech/Learn/Sources/wtfdig/{fight}.json";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, json);
        var source = WtfdigSource.Parse(json);
        Console.WriteLine($"{fight} @ {(commit.Length > 0 ? commit[..7] : branch)}: strats {string.Join(", ", source.StratNames)}; {source.Mechanics().Count} mechanics for {source.DefaultStrat}; mit plans {string.Join(", ", source.Mits.Select(kv => kv.Key))}{(mits is null ? " (no mits.ts)" : "")} -> {outPath} ({new FileInfo(outPath).Length / 1024} KiB)");
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

    // --example "SuparnaSongBearing=270;SatinPlumesFirst=[(16,-4),(-4,16)]" or the toolbox example of --lesson.
    private static IReadOnlyDictionary<string, string>? Example(Dictionary<string, string> opts)
    {
        if (opts.TryGetValue("example", out var text) && text.Length > 0)
            return text.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=', 2)).Where(p => p.Length == 2).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
        if (opts.TryGetValue("lesson", out var path) && path.Length > 0 && File.Exists(path))
            return LessonLoader.Parse(File.ReadAllText(path)).Toolbox?.Example;
        return null;
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
        if (opts.TryGetValue("pinned-dump", out var pinnedPath) && pinnedPath.Length > 0)
        {
            var pinned = ToolboxOracle.Check(plan, lesson, link, OrderDump.Parse(File.ReadAllText(pinnedPath)));
            Console.WriteLine($"with the example pinned (seed {pinned.ExampleSeed}): {pinned.TotalResidual:F1} m total, {pinned.Disagreements.Count()} disagree");
        }
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
        var registered = ScenarioCatalog.Create().Scenarios.FirstOrDefault(s => s.GetType().Name == scenario) is { } found && strat < found.AiStrats.Count
            ? PlanRegistry.For(scenario, found.AiStrats[strat].Name) : null;
        var planArg = opts.TryGetValue("plan", out var pl) && pl.Length > 0 ? pl : registered?.Url;
        var layoutName = opts.TryGetValue("layout", out var l) && l.Length > 0 ? l : registered?.Layout;
        var existing = opts.TryGetValue("lesson", out var lp) && lp.Length > 0 ? LessonLoader.Parse(File.ReadAllText(lp)) : null;
        var stem = AuthorOne(scenario, strat, Seeds(opts, "1..8"), outDir, null, planArg, layoutName, Console.Error, existing);
        Console.WriteLine($"dump     {stem}.dump.json");
        Console.WriteLine($"lesson   {stem}.lesson.json");
        Console.WriteLine($"context  {stem}.context.md");
        return 0;
    }

    // record -> skeleton -> mine -> cues -> draft [-> toolbox import -> oracle] -> context. Returns the file stem.
    private static string AuthorOne(string scenarioTypeName, int strat, List<int> seeds, string outDir, string? stemName, string? planArg, string? layoutName, TextWriter log, Lesson? existingLesson = null)
    {
        var dump = OrderRecorder.Record(scenarioTypeName, strat, seeds, log);
        var stem = Path.Combine(outDir, stemName ?? $"{dump.Scenario}.{strat}");
        File.WriteAllText(stem + ".dump.json", dump.ToJson());
        var lesson = LessonSkeleton.Generate(dump);

        var type = typeof(Game).Assembly.GetTypes().First(t => t.Name == dump.Scenario);
        var zone = ScenarioCatalog.Create().Scenarios.FirstOrDefault(s => s.GetType() == type)?.Phase.Zone;
        var layout = zone is null ? null : layoutName is null ? zone.WaymarkPresets.FirstOrDefault()
            : zone.WaymarkPresets.FirstOrDefault(w => w.Name.Equals(layoutName, StringComparison.OrdinalIgnoreCase)) ?? zone.WaymarkPresets.FirstOrDefault();
        var rules = RuleMiner.Mine(dump, layout);
        var cues = CueExtractor.Extract(dump);
        lesson = LessonDraft.Draft(lesson, dump, rules, cues, Glossary.Load(Glossary.FamilyOf(type)));

        ToolboxPlan? plan = null;
        OracleReport? oracle = null;
        if (planArg != null && layout != null)
        {
            var (id, raw) = FetchPlan(planArg);
            var (converted, report) = ProviderFor(planArg).Convert(raw, id, layout);
            plan = converted;
            File.WriteAllText(Path.Combine(outDir, $"{id}.json"), plan.ToJson());
            var refined = ToolboxImport.RefineRoleMap(plan, dump, report.RoleMap, out _);
            var steps = new Dictionary<string, int>();
            foreach (var (tb, stepId, _) in ToolboxImport.Align(plan, lesson, dump, refined))
                if (stepId != null) steps.TryAdd(stepId, tb);
            var link = new LessonToolbox(id, refined.ToDictionary(kv => kv.Value, kv => kv.Key), steps);
            oracle = ToolboxOracle.Check(plan, lesson, link, dump);
            lesson = lesson with { Toolbox = link with { ExampleSeed = oracle.ExampleSeed } };
            log.WriteLine($"toolbox {id}: {plan.Steps.Count} steps, {steps.Count} aligned, example seed {oracle.ExampleSeed}, {oracle.Disagreements.Count()} disagree");
            // A lesson that already pins the plan's rolls is scored again with them pinned: that is
            // the roll the player will replay, so the residual that matters is this one.
            var previous = existingLesson ?? LessonLoader.All.FirstOrDefault(l => l.Scenario == dump.Scenario && l.Strat == dump.Strat);
            if (previous?.Toolbox?.Example is { Count: > 0 } example)
            {
                var pinnedDump = OrderRecorder.Record(scenarioTypeName, strat, [oracle.ExampleSeed], TextWriter.Null, example);
                File.WriteAllText(stem + ".pinned.dump.json", pinnedDump.ToJson());
                var pinned = ToolboxOracle.Check(plan, lesson, lesson.Toolbox!, pinnedDump);
                lesson = lesson with { Toolbox = lesson.Toolbox! with { Example = new Dictionary<string, string>(example) } };
                oracle = pinned;
                log.WriteLine($"example pinned ({string.Join(", ", example.Keys)}): {pinned.TotalResidual:F1} m total, {pinned.Disagreements.Count()} disagree");
            }
        }
        var wtfdig = WtfdigSource.Load(Glossary.FamilyOf(type).ToLowerInvariant());
        var texts = wtfdig?.Mechanics();
        if (wtfdig != null && texts is { Count: > 0 } && lesson.Sources.All(s => !s.Url.Contains("wtfdig", StringComparison.OrdinalIgnoreCase)))
            lesson = lesson with { Sources = [.. lesson.Sources, new LessonSource($"WTFDIG ({wtfdig.DefaultStrat}), MIT", wtfdig.Url)] };
        File.WriteAllText(stem + ".lesson.json", LessonSkeleton.ToJson(lesson));
        File.WriteAllText(stem + ".context.md", LessonContext.Write(dump, lesson, rules, cues, oracle, plan, layout, texts));
        return stem;
    }

    private static int Draft(Dictionary<string, string> opts)
    {
        var dump = OrderDump.Parse(File.ReadAllText(Need(opts, "dump")));
        var lesson = LessonLoader.Parse(File.ReadAllText(Need(opts, "lesson")));
        var type = typeof(Game).Assembly.GetTypes().FirstOrDefault(t => t.Name == dump.Scenario);
        var zone = ScenarioCatalog.Create().Scenarios.FirstOrDefault(s => s.GetType() == type)?.Phase.Zone;
        var layout = Layout(opts, zone);
        var glossary = type is null ? Glossary.Empty : Glossary.Load(Glossary.FamilyOf(type));
        var drafted = LessonDraft.Draft(lesson, dump, RuleMiner.Mine(dump, layout), CueExtractor.Extract(dump), glossary);
        var outPath = Need(opts, "out");
        File.WriteAllText(outPath, LessonSkeleton.ToJson(drafted));
        Console.WriteLine($"draft    {outPath} ({drafted.Steps.Count(s => s.Skip)} skipped, {drafted.Steps.Count(s => s.Cue != null)} with a cue)");
        return 0;
    }

    private static int AuthorAll(Dictionary<string, string> opts)
    {
        var outDir = Need(opts, "out");
        var seeds = Seeds(opts, "1..12");
        var filter = opts.TryGetValue("scenario", out var f) ? f : "";
        var familyFilter = opts.TryGetValue("family", out var fam) ? fam : "";
        var force = opts.ContainsKey("force");
        var layoutName = opts.TryGetValue("layout", out var l) && l.Length > 0 ? l : null;
        var plans = (opts.TryGetValue("plans", out var pl) ? pl : "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=')).Where(x => x.Length == 2).ToDictionary(x => x[0].Trim(), x => x[1].Trim());
        var written = 0;
        foreach (var scenario in ScenarioCatalog.Create().Scenarios)
        {
            var typeName = scenario.GetType().Name;
            var family = Glossary.FamilyOf(scenario.GetType());
            if (filter.Length > 0 && !typeName.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
            if (familyFilter.Length > 0 && !family.Equals(familyFilter, StringComparison.OrdinalIgnoreCase)) continue;
            for (var i = 0; i < scenario.AiStrats.Count; i++)
            {
                var strat = scenario.AiStrats[i];
                if (!force && LessonLoader.For(scenario, strat) != null) continue;
                var dir = Path.Combine(outDir, family);
                Directory.CreateDirectory(dir);
                Console.Error.WriteLine($"{typeName} / {strat.Name}");
                var registered = PlanRegistry.For(typeName, strat.Name);
                var stem = AuthorOne(typeName, i, seeds, dir, $"{typeName.Replace("Scenario", "")}.{Slug(strat.Name)}",
                    plans.GetValueOrDefault(typeName) ?? registered?.Url, layoutName ?? registered?.Layout, TextWriter.Null);
                Console.WriteLine($"{stem}.lesson.json");
                written++;
            }
        }
        Console.WriteLine($"{written} lesson draft(s) -> {outDir}");
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
