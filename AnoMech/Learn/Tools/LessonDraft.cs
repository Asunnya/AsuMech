using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AnoMech.Learn.Tools;

// A first text for every step, from the mined rules and cues alone: where to go (shared or per
// role), what to watch for, how tight the spot is, and whether the step is worth teaching at all.
// The author edits wording; the facts are already there.
public static class LessonDraft
{
    public const float StackYalms = 2f;
    public const float SameSpotYalms = 4f;
    private static readonly string[] GenericStatuses = ["Vulnerability Up", "Damage Down", "Weakness", "Brink of Death", "Transcendent", "Physical Vulnerability Up", "Magic Vulnerability Up"];
    public const float SkipMoveYalms = 1f;
    public const float CueMaxSecondsBefore = 8f;

    private static readonly string[] Roles = ["MainTank", "OffTank", "RegenHealer", "ShieldHealer", "MeleeDpsA", "MeleeDpsB", "PhysRangedDps", "CasterDps"];

    public static Lesson Draft(Lesson skeleton, OrderDump dump, List<MinedRule> rules, Dictionary<(string Cls, int Line, string Role), List<CueCandidate>> cues)
    {
        var steps = new List<LessonStep>();
        LessonStep? previous = null;
        foreach (var step in skeleton.Steps)
        {
            var line = step.Key.Line ?? -1;
            var stepRules = rules.Where(r => r.Cls == step.Key.Cls && r.Line == line).ToList();
            var stepCues = Roles.Where(role => cues.ContainsKey((step.Key.Cls, line, role))).ToDictionary(role => role, role => cues[(step.Key.Cls, line, role)]);
            var skip = Skips(step, previous, dump);
            var (rule, roles) = Rules(stepRules);
            var cue = Cue(stepCues);
            var drafted = step with
            {
                Title = Title(step, cue),
                Rule = skip ? null : rule,
                Roles = skip || roles.Count == 0 ? null : roles,
                Cue = skip ? null : cue?.Text,
                Tolerance = Tolerance(stepRules),
                Skip = skip,
            };
            steps.Add(drafted);
            previous = step;
        }
        return skeleton with { Steps = steps };
    }

    // The orders that moved nobody more than a step from where the previous order put them.
    private static bool Skips(LessonStep step, LessonStep? previous, OrderDump dump)
    {
        if (previous is null) return false;
        foreach (var role in Roles)
        {
            var now = Targets(step, role, dump);
            var before = Targets(previous, role, dump);
            if (now.Count == 0) continue;
            foreach (var (seed, at) in now)
                if (!before.TryGetValue(seed, out var was) || Vector2.Distance(at, was) > SkipMoveYalms) return false;
        }
        return true;
    }

    private static Dictionary<int, Vector2> Targets(LessonStep step, string role, OrderDump dump)
    {
        var orders = dump.Orders.Where(o => o.Role == role && step.Key.Matches(o.Key)).ToList();
        if (orders.Count == 0) return new();
        var first = orders.Min(o => o.Time);
        return orders.Where(o => Math.Abs(o.Time - first) < 0.01f).GroupBy(o => o.Seed).ToDictionary(g => g.Key, g => new Vector2(g.First().X, g.First().Z));
    }

    // The description most roles share is the rule; a role a few steps away from that spot (a
    // spread) shares it too; the rest get their own line.
    private static (string Rule, Dictionary<string, LessonRoleText> Roles) Rules(List<MinedRule> rules)
    {
        var byRole = rules.ToDictionary(r => r.Role, Sentence);
        if (byRole.Count == 0) return ("", new());
        var shared = byRole.Values.GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key;
        var anchor = rules.FirstOrDefault(r => Sentence(r) == shared);
        bool Same(MinedRule r) => Sentence(r) == shared || anchor is { Fixed: true } && r.Fixed
            && Vector2.Distance(new Vector2(r.Spots[0].X, r.Spots[0].Z), new Vector2(anchor.Spots[0].X, anchor.Spots[0].Z)) <= SameSpotYalms;
        var own = rules.Where(r => !Same(r)).ToDictionary(r => r.Role, r => new LessonRoleText(Rule: byRole[r.Role]));
        if (byRole.Count == 1) return (byRole.Values.First(), new());
        if (own.Count == byRole.Count - 1 && byRole.Count > 2) return ("Each role has its own spot.", byRole.ToDictionary(kv => kv.Key, kv => new LessonRoleText(Rule: kv.Value)));
        return (shared, own);
    }

    public static string Sentence(MinedRule rule)
    {
        if (rule.Fixed) return $"{Place(rule.Spots[0].Label)}.";
        if (rule.Relation is { } relation) return $"{Relation(relation)}.";
        if (rule.Fields.Count > 0)
            return $"Depends on {string.Join(" and ", rule.Fields)}: {string.Join("; ", rule.SpotByValue.Select(kv => $"{kv.Key} → {kv.Value}"))}.";
        var closest = rule.NearestField is { } near && rule.Purity >= 0.6f ? $" (closest field: {near})" : "";
        return $"The spot follows a rule the rolls alone don't show; read the strat{closest}.";
    }

    // "at B" / "near D (W 8m)" / "centre" / "N wall" / "SE 12m" in words.
    public static string Place(string label)
    {
        if (label == "centre") return "Stand at the centre";
        if (label.StartsWith("at ", StringComparison.Ordinal)) return $"Stand on waymark {label[3..]}";
        if (label.StartsWith("near ", StringComparison.Ordinal))
        {
            var open = label.IndexOf('(');
            var mark = open < 0 ? label[5..] : label[5..open].Trim();
            var inside = open < 0 ? "" : $", {Compass(label[(open + 1)..^1])}";
            return $"Stand near waymark {mark}{inside}";
        }
        return $"Go to {Compass(label)}";
    }

    private static string Relation(string relation)
        => relation.StartsWith("with ", StringComparison.Ordinal) && relation.Contains(": ")
            ? $"{relation[..relation.IndexOf(": ", StringComparison.Ordinal)]}: {Compass(relation[(relation.IndexOf(": ", StringComparison.Ordinal) + 2)..])}"
            : relation.StartsWith("at ", StringComparison.Ordinal) ? $"Go to {relation[3..]}" : $"Go {relation}";

    // "N wall" -> "the north wall", "SE 12m" -> "12m south-east of the centre".
    private static string Compass(string text)
    {
        var parts = text.Split(' ', 2);
        var dir = parts[0] switch
        {
            "N" => "north", "NE" => "north-east", "E" => "east", "SE" => "south-east",
            "S" => "south", "SW" => "south-west", "W" => "west", "NW" => "north-west", _ => parts[0],
        };
        if (parts.Length < 2) return dir;
        return parts[1] == "wall" ? $"the {dir} wall" : $"{parts[1]} {dir} of the centre";
    }

    public sealed record DraftCue(string Text, float SecondsBefore);

    // The last thing every seed showed before the order, the role's own events first.
    public static DraftCue? Cue(Dictionary<string, List<CueCandidate>> byRole)
    {
        var all = byRole.SelectMany(kv => kv.Value.Select(c => (Role: kv.Key, Cue: c))).ToList();
        var own = all.Where(x => x.Cue.Target == x.Role && x.Cue.Stable && Usable(x.Cue)).OrderBy(x => x.Cue.SecondsBefore).FirstOrDefault();
        if (own.Cue != null) return new DraftCue(Text(own.Cue, true), own.Cue.SecondsBefore);
        var shared = all.Where(x => x.Cue.Target != x.Role && x.Cue.Stable && Usable(x.Cue)).OrderBy(x => x.Cue.SecondsBefore).FirstOrDefault();
        return shared.Cue is null ? null : new DraftCue(Text(shared.Cue, false), shared.Cue.SecondsBefore);
    }

    private static bool Usable(CueCandidate c)
    {
        if (c.Name.Contains("BNpc", StringComparison.Ordinal) || GenericStatuses.Contains(c.Name)) return false;
        return c.Kind == "ability" ? c.SecondsBefore <= 3f : c.SecondsBefore <= CueMaxSecondsBefore;
    }

    private static string Text(CueCandidate c, bool own) => c.Kind switch
    {
        "cast" => $"{c.Name} cast starts",
        "ability" => own ? $"{c.Name} hits you" : $"{c.Name} resolves",
        "status" => own ? $"{c.Name} on you" : $"{c.Name} on {c.Target ?? "someone"}",
        "tether" => own ? "A tether lands on you" : $"A tether lands on {c.Target ?? "someone"}",
        "marker" => own ? "A marker lands on you" : $"A marker lands on {c.Target ?? "someone"}",
        "spawn" => $"{c.Name} appears",
        _ => c.Name,
    };

    private static string Title(LessonStep step, DraftCue? cue)
    {
        if (step.Title is { } t && !t.StartsWith("Move at", StringComparison.Ordinal)) return t;
        return cue is null ? step.Title ?? "Move" : $"After {cue.Text[..1].ToLowerInvariant()}{cue.Text[1..]}";
    }

    // Everyone on one spot is a stack; a spot nothing explains gets room; the rest is a reposition.
    private static float Tolerance(List<MinedRule> rules)
    {
        if (rules.Count == 0) return LessonSkeleton.DefaultTolerance;
        if (rules.Any(r => !r.Explained)) return 2.5f;
        var centres = rules.Where(r => r.Fixed).Select(r => new Vector2(r.Spots[0].X, r.Spots[0].Z)).ToList();
        if (centres.Count == rules.Count && centres.Count >= 4 && centres.All(c => centres.All(o => Vector2.Distance(c, o) <= StackYalms * 2))) return 1.5f;
        return LessonSkeleton.DefaultTolerance;
    }
}
