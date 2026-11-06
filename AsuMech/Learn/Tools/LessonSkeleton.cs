using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsuMech.Learn.Tools;

// A lesson with one step per distinct strat call, titles drafted from method names, and no rules
// yet. Calls that share a source line (a loop) fold into one step with a time range.
public static class LessonSkeleton
{
    public const float DefaultTolerance = 2f;

    public static Lesson Generate(OrderDump dump)
    {
        var steps = dump.Orders
            .GroupBy(o => (o.Cls, o.Line))
            .OrderBy(g => g.Min(o => o.Time))
            .Select(g =>
            {
                var times = g.Select(o => o.Time).Distinct().OrderBy(t => t).ToList();
                var first = g.First();
                var key = times.Count == 1
                    ? new LessonStepKey(first.Cls, Time: times[0], Line: first.Line)
                    : new LessonStepKey(first.Cls, From: times[0], To: times[^1], Line: first.Line);
                var title = first.Method is { } m ? Title(m) : $"Move at {times[0]:0.#}s";
                return new LessonStep(key, Title: title, Rule: "", Tolerance: DefaultTolerance);
            })
            .ToList();
        return new Lesson(dump.Scenario, dump.Strat, [], steps, Draft: true);
    }

    // TanksInterceptSistersSongsPartyStackSouthEast -> "Tanks intercept sisters songs party stack south east".
    public static string Title(string method)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < method.Length; i++)
        {
            var c = method[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(method[i - 1])) sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpperInvariant(c) : char.IsUpper(c) && i + 1 < method.Length && char.IsLower(method[i + 1]) ? char.ToLowerInvariant(c) : c);
        }
        return sb.ToString();
    }

    // Steps whose time no longer matches any order but whose line still does get the new time.
    // Returns the steps nothing matches at all.
    public static (Lesson Lesson, IReadOnlyList<LessonStep> Orphans) Rekey(Lesson lesson, OrderDump dump)
    {
        var byLine = dump.Orders.GroupBy(o => (o.Cls, o.Line)).ToDictionary(g => g.Key, g => g.Select(o => o.Time).Distinct().OrderBy(t => t).ToList());
        var orphans = new List<LessonStep>();
        var steps = lesson.Steps.Select(step =>
        {
            if (dump.Orders.Any(o => step.Key.Matches(o.Key))) return step;
            if (step.Key.Line is { } line && byLine.TryGetValue((step.Key.Cls, line), out var times))
            {
                var key = times.Count == 1
                    ? step.Key with { Time = times[0], From = null, To = null }
                    : step.Key with { Time = null, From = times[0], To = times[^1] };
                return step with { Key = key };
            }
            orphans.Add(step);
            return step;
        }).ToList();
        return (lesson with { Steps = steps }, orphans);
    }

    // What the dump has that the lesson doesn't, and the other way round.
    public static (IReadOnlyList<DumpedOrder> Uncovered, IReadOnlyList<LessonStep> Untriggered) Coverage(Lesson lesson, OrderDump dump)
    {
        var uncovered = dump.Orders.Where(o => lesson.StepFor(o.Key) is null).DistinctBy(o => (o.Cls, o.Time, o.Source)).ToList();
        var untriggered = lesson.Steps.Where(s => !s.Skip && !dump.Orders.Any(o => s.Key.Matches(o.Key))).ToList();
        return (uncovered, untriggered);
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string ToJson(Lesson lesson) => JsonSerializer.Serialize(lesson, Json);
}
