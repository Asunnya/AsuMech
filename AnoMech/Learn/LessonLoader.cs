using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnoMech.Core.Game.Ai;
using AnoMech.Scenarios;

namespace AnoMech.Learn;

// Lessons are JSON resources under Learn/Lessons/, one per scenario type and strat name.
public static class LessonLoader
{
    private const string ResourcePrefix = "AnoMech.Learn.Lessons.";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Lazy<IReadOnlyList<Lesson>> Embedded = new(LoadEmbedded);

    // Lessons read from files under DevDirectory (any depth, glossaries aside) shadow the embedded
    // ones with the same scenario and strat, and are read again whenever a file changes, so a
    // lesson can be reviewed in game without a build.
    private static string? devDirectory;
    private static Dictionary<string, DateTime> devStamps = new();
    private static IReadOnlyList<Lesson> devLessons = [];
    private static DateTime lastScan = DateTime.MinValue;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(1);

    public static string? DevDirectory
    {
        get => devDirectory;
        set
        {
            devDirectory = string.IsNullOrWhiteSpace(value) ? null : value;
            devStamps = new();
            devLessons = [];
            lastScan = DateTime.MinValue;
            DevErrors = [];
            Refresh(force: true);
        }
    }

    public static IReadOnlyList<string> DevErrors { get; private set; } = [];
    public static int DevCount => devLessons.Count;
    public static DateTime LastReload { get; private set; }

    public static IReadOnlyList<Lesson> All
        => devLessons.Count == 0 ? Embedded.Value
            : devLessons.Concat(Embedded.Value.Where(e => devLessons.All(d => d.Scenario != e.Scenario || d.Strat != e.Strat))).ToList();

    // True when the dev folder changed since the last look; at most one look per second.
    public static bool Refresh(bool force = false)
    {
        if (devDirectory is null) return false;
        var now = DateTime.UtcNow;
        if (!force && now - lastScan < ScanInterval) return false;
        lastScan = now;
        if (!Directory.Exists(devDirectory)) { devStamps = new(); devLessons = []; DevErrors = [$"no folder {devDirectory}"]; return false; }
        var stamps = Directory.EnumerateFiles(devDirectory, "*.json", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).Equals(Glossary.FileName, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(f => f, File.GetLastWriteTimeUtc);
        if (stamps.Count == devStamps.Count && stamps.All(kv => devStamps.TryGetValue(kv.Key, out var t) && t == kv.Value)) return false;
        devStamps = stamps;
        var lessons = new List<Lesson>();
        var errors = new List<string>();
        foreach (var file in stamps.Keys.OrderBy(f => f))
        {
            try { lessons.Add(Parse(File.ReadAllText(file))); }
            catch (Exception e) when (e is JsonException or InvalidDataException or IOException) { errors.Add($"{Path.GetFileName(file)}: {e.Message}"); }
        }
        devLessons = lessons;
        DevErrors = errors;
        LastReload = now;
        return true;
    }

    public static Lesson? For(IScenario scenario, IScenarioAi strat)
        => All.FirstOrDefault(l => l.Scenario == scenario.GetType().Name && l.Strat == strat.Name);

    public static Lesson Parse(string json) => JsonSerializer.Deserialize<Lesson>(json, Json)
        ?? throw new InvalidDataException("empty lesson");

    // Why a lesson can't be used with a catalog, or nothing.
    public static IReadOnlyList<string> Validate(Lesson lesson, IEnumerable<IScenario> catalog)
    {
        var errors = new List<string>();
        var scenario = catalog.FirstOrDefault(s => s.GetType().Name == lesson.Scenario);
        if (scenario is null)
        {
            errors.Add($"no scenario named {lesson.Scenario}");
            return errors;
        }
        if (scenario.AiStrats.All(a => a.Name != lesson.Strat))
            errors.Add($"{lesson.Scenario} has no strat named \"{lesson.Strat}\"");
        for (var i = 0; i < lesson.Steps.Count; i++)
        {
            var key = lesson.Steps[i].Key;
            if (key.Time is null && (key.From is null || key.To is null))
                errors.Add($"step {i}: key needs a time or a from/to range");
            if (!lesson.Steps[i].Skip && string.IsNullOrWhiteSpace(lesson.Steps[i].Rule) && (lesson.Steps[i].Roles?.Values.All(r => string.IsNullOrWhiteSpace(r.Rule)) ?? true))
                errors.Add($"step {i} ({key.Time ?? key.From}): needs a rule or skip");
            if (!lesson.Draft && !lesson.Steps[i].Skip && (Lesson.IsTable(lesson.Steps[i].Rule) || (lesson.Steps[i].Roles?.Values.Any(r => Lesson.IsTable(r.Rule)) ?? false)))
                errors.Add($"step {i} ({key.Time ?? key.From}): rule is still a table; reword it or mark the lesson draft");
        }
        return errors;
    }

    private static IReadOnlyList<Lesson> LoadEmbedded()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var lessons = new List<Lesson>();
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal) && !n.EndsWith("." + Glossary.FileName, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            lessons.Add(Parse(reader.ReadToEnd()));
        }
        return lessons;
    }
}
