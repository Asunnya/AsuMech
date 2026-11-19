using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

    // Steps whose time no longer matches any order but whose line still does get the new time; steps
    // whose time still matches but whose call moved to another line get that line (see FollowLine).
    // Returns the steps nothing matches at all. `source` reads a strat class's line, when it can.
    public static (Lesson Lesson, IReadOnlyList<LessonStep> Orphans) Rekey(Lesson lesson, OrderDump dump, Func<string, int, string?>? source = null)
    {
        var byLine = dump.Orders.GroupBy(o => (o.Cls, o.Line)).ToDictionary(g => g.Key, g => g.Select(o => o.Time).Distinct().OrderBy(t => t).ToList());
        var orphans = new List<LessonStep>();
        var steps = lesson.Steps.Select(step =>
        {
            var lines = dump.Orders.Where(o => step.Key.Matches(o.Key)).Select(o => o.Line).Distinct().ToList();
            if (lines.Count > 0)
                return FollowLine(step.Key, lines, byLine.ContainsKey, source) is { } moved ? step with { Key = step.Key with { Line = moved } } : step;
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

    // The line a step whose time still matches orders on `lines` should move to, or null to keep its
    // own. A key line that still holds the step's time literal is still its call; otherwise the line
    // that holds it now wins. A computed time moves only off a line that recorded nothing and no
    // longer reads as a call: a roll can leave a live call unrecorded on the seed, or put the time on
    // another branch's line.
    private static int? FollowLine(LessonStepKey key, List<int> lines, Func<(string, int), bool> recorded, Func<string, int, string?>? source)
    {
        if (key.Line is not { } old || lines.Contains(old) || source is null) return null;
        var literal = (key.Time ?? key.From)?.ToString("0.##", CultureInfo.InvariantCulture);
        bool Holds(int line) => literal != null && source(key.Cls, line) is { } text && HoldsLiteral(text, literal);
        if (Holds(old)) return null;
        var holding = lines.Where(Holds).ToList();
        if (holding.Count > 0) return Nearest(holding, old);
        return recorded((key.Cls, old)) || source(key.Cls, old) is { } oldText && LooksLikeCall(oldText) ? null : Nearest(lines, old);
    }

    // Calls at the same time on several lines: the one the edit shifted least.
    private static int Nearest(List<int> lines, int old) => lines.OrderBy(l => Math.Abs(l - old)).ThenBy(l => l).First();

    public static bool HoldsLiteral(string text, string literal)
        => Regex.IsMatch(text, $@"(?<![\d.]){Regex.Escape(literal)}(?![\d])");

    // A line that can carry an order: an AiManager call, or a strat helper that forwards its
    // caller's line (WalkAround(...), PairUp(...)).
    public static bool LooksLikeCall(string text)
        => Regex.IsMatch(text, @"\b(Move|MoveNow|Order|Automarker|MoveTo|Loop)\s*\(") || text.Contains("ai.", StringComparison.Ordinal)
           || Regex.IsMatch(text, @"^\s*(var\s+\w+\s*=\s*)?[A-Z]\w*\s*\(|=>\s*[A-Z]\w*\s*\(");

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
