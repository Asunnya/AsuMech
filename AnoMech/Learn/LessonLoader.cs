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

    public static IReadOnlyList<Lesson> All => Embedded.Value;

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
