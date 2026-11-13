using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using System.Numerics;

namespace AsuMech.Learn.Tools;

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

    public static Lesson Draft(Lesson skeleton, OrderDump dump, List<MinedRule> rules, Dictionary<(string Cls, int Line, string Role), List<CueCandidate>> cues, Glossary? glossary = null, Sources.ModuleSource? modules = null)
    {
        glossary ??= Glossary.Empty;
        modulesInUse = modules;
        var steps = new List<LessonStep>();
        LessonStep? previous = null;
        foreach (var step in skeleton.Steps)
        {
            var line = step.Key.Line ?? -1;
            var stepRules = rules.Where(r => r.Cls == step.Key.Cls && r.Line == line).ToList();
            var stepCues = Roles.Where(role => cues.ContainsKey((step.Key.Cls, line, role))).ToDictionary(role => role, role => cues[(step.Key.Cls, line, role)]);
            var skip = Skips(step, previous, dump);
            var method = dump.Orders.FirstOrDefault(o => step.Key.Matches(o.Key))?.Method;
            var (rule, roles) = Rules(stepRules, glossary, method);
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
        return skeleton with { Steps = steps, Draft = true };
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
    private static (string Rule, Dictionary<string, LessonRoleText> Roles) Rules(List<MinedRule> rules, Glossary glossary, string? method)
    {
        string Sentence(MinedRule r) => LessonDraft.Sentence(r, glossary, method);
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

    // A sentence when the glossary can word every roll value the rule turns on; a "Depends on"
    // table otherwise, which the validator refuses in a reviewed lesson; "read the strat" when the
    // rolls alone don't decide the spot.
    public static string Sentence(MinedRule rule, Glossary? glossary = null, string? method = null)
    {
        glossary ??= Glossary.Empty;
        if (rule.Fixed) return $"{Place(rule.Spots[0].Label)}.";
        if (rule.Relation is { } relation) return $"{glossary.Apply(Relation(relation))}.";
        if (rule.Fields.Count == 1 && rule.Fields[0].EndsWith(" has " + rule.Role, StringComparison.Ordinal) && rule.SpotByValue.Count == 2
            && rule.SpotByValue.TryGetValue("yes", out var inside) && rule.SpotByValue.TryGetValue("no", out var outside))
            return $"If you are in {glossary.Field(rule.Fields[0][..^(" has ".Length + rule.Role.Length)])}: {Place(inside)}; otherwise: {Place(outside)}.";
        if (rule.Fields.Count > 0 && rule.Fields.All(Readable) && rule.SpotByValue.Keys.All(Readable))
        {
            var clauses = rule.SpotByValue.Select(kv => (Words: Conditions(kv.Key, rule.Fields, glossary), Spot: kv.Value)).ToList();
            if (clauses.All(c => c.Words != null))
                return string.Join("; ", clauses.Select((c, i) => $"{(i == 0 ? "When" : "when")} {c.Words}, {Lower(Place(c.Spot))}")) + ".";
            return $"Depends on {string.Join(" and ", rule.Fields.Select(f => glossary.Apply(f)))}: {string.Join("; ", rule.SpotByValue.Select(kv => $"{ValueWords(kv.Key, rule.Fields, glossary)} → {kv.Value}"))}.";
        }
        var nearest = rule.NearestField ?? rule.Fields.FirstOrDefault();
        var closest = nearest is { } near && (rule.Purity >= 0.6f || rule.Fields.Count > 0) ? $" (closest roll: {glossary.Apply(FieldName(near))})" : "";
        return method is { Length: > 0 }
            ? $"Read the strat's {method}: the spot follows a rule the rolls alone don't show{closest}."
            : $"The spot follows a rule the rolls alone don't show; read the strat{closest}.";
    }

    // A roll value a player could read; a record list printed whole ("SawCycle { CastAt = … }") is not.
    private const int ReadableLength = 60;
    private static bool Readable(string text) => text.Length <= ReadableLength && !text.Contains('{');

    // "Cycles has SawCycle { … }" -> "Cycles": the item is unreadable, the field still names the roll.
    private static string FieldName(string field)
    {
        if (Readable(field)) return field;
        var has = field.IndexOf(" has ", StringComparison.Ordinal);
        return has > 0 ? field[..has] : field[..Math.Min(field.Length, ReadableLength)];
    }

    // "the nails' close pair is west and the safe side is right", or null when a value has no words.
    private static string? Conditions(string key, List<string> fields, Glossary glossary)
    {
        var parts = key.Split(" & ");
        if (parts.Length != fields.Count) return null;
        var clauses = new List<string>();
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var value = parts[i];
            var has = field.IndexOf(" has ", StringComparison.Ordinal);
            if (has > 0)
            {
                var item = Words(field[..has], field[(has + 5)..], glossary) ?? field[(has + 5)..];
                if (!Readable(item)) return null;
                if (value == "yes") clauses.Add($"{glossary.Field(field[..has])} includes {item}");
                else if (value == "no") clauses.Add($"{glossary.Field(field[..has])} leaves out {item}");
                else return null;
                continue;
            }
            if (Words(field, value, glossary) is not { } words) return null;
            clauses.Add($"{glossary.Field(field)} is {words}");
        }
        return string.Join(" and ", clauses);
    }

    // A roll value in a player's words: the glossary's, a compass point for a bearing, a seat
    // for a role; null for anything the player could not read off the screen.
    private static string? Words(string field, string value, Glossary glossary)
    {
        var worded = glossary.Value(field, value);
        if (worded != value) return worded;
        if (field.EndsWith("Bearing", StringComparison.Ordinal) && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var degrees) && degrees % 45f == 0f)
            return CompassName(degrees);
        if (value.Length > 0 && char.IsLetter(value[0]) && Enum.TryParse<PartyRole>(value, out var role)) return Seat(role);
        return null;
    }

    private static string Seat(PartyRole role) => role switch
    {
        PartyRole.MainTank => "MT", PartyRole.OffTank => "OT", PartyRole.RegenHealer => "H1", PartyRole.ShieldHealer => "H2",
        PartyRole.MeleeDpsA => "M1", PartyRole.MeleeDpsB => "M2", PartyRole.PhysRangedDps => "R1", _ => "R2",
    };

    private static string CompassName(float degrees) => (((int)degrees % 360 + 360) % 360) switch
    {
        0 => "north", 45 => "north-east", 90 => "east", 135 => "south-east", 180 => "south", 225 => "south-west", 270 => "west", _ => "north-west",
    };

    private static string Lower(string sentence) => sentence.Length > 0 ? char.ToLowerInvariant(sentence[0]) + sentence[1..] : sentence;

    // A table key ("270 & yes") with each field's value in the glossary's words.
    private static string ValueWords(string key, List<string> fields, Glossary glossary)
    {
        var parts = key.Split(" & ");
        if (parts.Length != fields.Count) return key;
        return string.Join(" & ", parts.Select((v, i) => glossary.Value(fields[i], v)));
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
        "tether" => own ? $"A tether{Named(c)} lands on you" : $"A tether{Named(c)} lands on {c.Target ?? "someone"}",
        "marker" => own ? $"A marker{Named(c)} lands on you" : $"A marker{Named(c)} lands on {c.Target ?? "someone"}",
        "spawn" => $"{c.Name} appears",
        _ => c.Name,
    };

    // The game gives head markers and tethers no name; a public module's enum member is the next
    // best thing, spaced out ("CometIcon" -> "Comet Icon"), until the glossary words it.
    [ThreadStatic] private static Sources.ModuleSource? modulesInUse;

    private static string Named(CueCandidate c)
    {
        var named = NameCue(c, modulesInUse);
        return named == c.Name ? "" : $" ({named})";
    }

    public static string NameCue(CueCandidate c, Sources.ModuleSource? modules)
    {
        if (modules is null || c.Kind is not ("marker" or "tether")) return c.Name;
        var space = c.Name.LastIndexOf(' ');
        if (space < 0 || !long.TryParse(c.Name[(space + 1)..], out var value)) return c.Name;
        var name = c.Kind == "marker" ? modules.IconName(value) : modules.TetherName(value);
        return name is null ? c.Name : System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])|_", " ").Trim();
    }

    // Steps whose cue came from something CueExtractor no longer offers get the next cue, and the
    // title drafted from the old cue follows it; written cues and titles stay.
    public static (Lesson Lesson, int Changed) Recue(Lesson lesson, OrderDump dump, Sources.ModuleSource? modules = null)
    {
        modulesInUse = modules;
        var stale = (dump.Runs ?? []).SelectMany(r => r.Events).Where(CueExtractor.NotACue).Select(e => e.Name).Where(n => n.Length > 0).ToHashSet();
        var cues = CueExtractor.Extract(dump);
        var changed = 0;
        var steps = lesson.Steps.Select(step =>
        {
            // A skipped step keeps no cue but still carries the title drafted from it.
            var old = step.Cue ?? (step.Title is { Length: > 6 } t && t.StartsWith("After ", StringComparison.Ordinal) ? $"{char.ToUpperInvariant(t[6])}{t[7..]}" : null);
            if (old is null || !IsStale(old, stale)) return step;
            var line = step.Key.Line ?? -1;
            var cue = Cue(Roles.Where(role => cues.ContainsKey((step.Key.Cls, line, role))).ToDictionary(role => role, role => cues[(step.Key.Cls, line, role)]));
            changed++;
            var draftedTitle = step.Title == AfterTitle(old) || step.Title == $"After {old[..1].ToLowerInvariant()}{old[1..]}";
            var time = step.Key.Time ?? step.Key.From ?? 0f;
            return step with
            {
                Cue = step.Cue is null ? null : cue?.Text,
                Title = !draftedTitle ? step.Title : cue is null ? $"Move at {time:0.#}s" : AfterTitle(cue.Text),
            };
        }).ToList();
        return (lesson with { Steps = steps }, changed);
    }

    private static bool IsStale(string cue, HashSet<string> partyMade)
        => cue.Length == 0 || char.IsWhiteSpace(cue[0]) || partyMade.Any(n => cue.StartsWith(n + " on ", StringComparison.Ordinal) || cue == $"{n} hits you" || cue == $"{n} resolves");

    // Only a leading article drops its capital; names keep theirs ("After Fearsome Fireball resolves").
    private static string AfterTitle(string cue)
        => System.Text.RegularExpressions.Regex.IsMatch(cue, "^(A|An|The) ") ? $"After {cue[..1].ToLowerInvariant()}{cue[1..]}" : $"After {cue}";

    private static string Title(LessonStep step, DraftCue? cue)
    {
        if (step.Title is { } t && !t.StartsWith("Move at", StringComparison.Ordinal)) return t;
        return cue is null ? step.Title ?? "Move" : AfterTitle(cue.Text);
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
