using AnoMech.Learn;
using AnoMech.Learn.Tools;
using AnoMech.Scenarios;

namespace AnoMech.LessonTools;

// record   --scenario <TypeName> [--strat N] [--seeds 1,2,3] --out dump.json
// skeleton --dump dump.json --out lesson.json
// rekey    --lesson lesson.json --dump dump.json [--out lesson.json]
// check    --lesson lesson.json --dump dump.json
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
