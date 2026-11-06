using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using AsuMech.Learn.Tools;

namespace AsuMech.Learn.Toolbox;

public sealed record ExtractedPin(string Property, string Value, string How);

public sealed record ExampleExtraction(Dictionary<string, string> Example, List<ExtractedPin> Pins, List<string> Skipped);

// Reads the roll a raidplan draws off its adds and buffs and writes it as the scenario's
// *StateOverrides values: a list of spots from a cluster of adds named like the roll (snapped to
// the State's public static spot set named alike), a bearing from an add at the step the lesson
// aligns to the roll's mechanic, a list of roles from the buff icons drawn there. Whatever does
// not match stays out, with the reason, so the author can pin it by hand.
public static class ExampleExtractor
{
    private const float StackedYalms = 3f;
    private const float MovedYalms = 3f;
    private static readonly string[] Ordinals = ["First", "Second", "Third", "Fourth"];
    private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase) { "spots", "spot", "targets", "target", "bearing", "bearings", "first", "second", "third", "fourth", "set", "list" };

    public static ExampleExtraction Extract(ToolboxPlan plan, Lesson lesson, LessonToolbox link, OrderDump dump, Type overridesType, Type? stateType = null)
    {
        var pins = new List<ExtractedPin>();
        var skipped = new List<string>();
        var seed = link.ExampleSeed ?? dump.Runs?.FirstOrDefault()?.Seed ?? 0;
        var free = dump.Runs?.FirstOrDefault(r => r.Seed == seed)?.State ?? dump.Runs?.FirstOrDefault()?.State ?? new();
        // Two calls at one time (different lines) share an id; the first keeps it.
        var stepsById = lesson.Steps.GroupBy(s => MasteryStore.StepId(s.Key)).ToDictionary(g => g.Key, g => g.First());
        var alignedSteps = link.Steps.Where(kv => stepsById.ContainsKey(kv.Key) && kv.Value >= 0 && kv.Value < plan.Steps.Count)
            .Select(kv => (Lesson: stepsById[kv.Key], Toolbox: kv.Value)).OrderBy(x => x.Toolbox).ToList();
        var roleBySlot = link.RoleMap.ToDictionary(kv => kv.Value, kv => kv.Key);
        var bearingPairs = new List<(PropertyInfo Property, string Stem)>();

        foreach (var property in overridesType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite))
        {
            var name = property.Name;
            var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var stateName = free.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            var tokens = Tokens(name);

            if (IsVectorList(target))
            {
                if (stateName is null) { skipped.Add($"{name}: the State has no member of that name"); continue; }
                var count = ExampleOverrides.Items(free[stateName]).Count();
                var ordinal = Array.FindIndex(Ordinals, o => name.EndsWith(o, StringComparison.Ordinal));
                var clusters = Clusters(plan, tokens, count);
                var wanted = Math.Max(ordinal, 0);
                if (clusters.Count <= wanted) { skipped.Add($"{name}: the plan draws {clusters.Count} group(s) of {count} add(s) named like it, needed {wanted + 1}"); continue; }
                var (stepIndex, positions) = clusters[wanted];
                var (snapped, spotSet) = Snap(positions, stateType, tokens);
                if (snapped.Distinct().Count() != snapped.Count) { skipped.Add($"{name}: two adds at toolbox step {stepIndex} snap to the same {spotSet} spot"); continue; }
                var value = "[" + string.Join(",", snapped.Select(v => $"({F(v.X)},{F(v.Y)})")) + "]";
                pins.Add(new ExtractedPin(name, value, $"{count} adds at toolbox step {stepIndex} \"{plan.Steps[stepIndex].Label}\"{(spotSet is null ? "" : $", snapped to {spotSet}")}"));
                continue;
            }

            if (target == typeof(float) && name.EndsWith("Bearing", StringComparison.Ordinal))
            {
                bearingPairs.Add((property, name[..^"Bearing".Length]));
                continue;
            }

            if (IsRoleList(target))
            {
                if (stateName is null) { skipped.Add($"{name}: the State has no member of that name"); continue; }
                var count = ExampleOverrides.Items(free[stateName]).Count();
                var aligned = Aligned(alignedSteps, tokens);
                if (aligned is null) { skipped.Add($"{name}: no aligned lesson step mentions {string.Join("/", tokens)}"); continue; }
                // The plan's buffs may land a step or two after the aligned one; look up to the next aligned step.
                var end = alignedSteps.Select(a => a.Toolbox).Where(t => t > aligned.Value.Toolbox).DefaultIfEmpty(plan.Steps.Count).Min();
                var found = false;
                for (var t = aligned.Value.Toolbox; t < end && !found; t++)
                {
                    var previous = t > 0 ? plan.Steps[t - 1] : null;
                    List<string>? Before(ToolboxObject o) => previous?.Objects.FirstOrDefault(p => p.Key == o.Key)?.Buffs;
                    var players = plan.Steps[t].Objects.Where(o => o.Kind == "player" && o.Buffs is { Count: > 0 }).ToList();
                    var first = players.Where(o => Before(o) is not { Count: > 0 }).Select(o => roleBySlot.GetValueOrDefault(o.Key)).OfType<string>().ToList();
                    var changed = players.Where(o => Before(o) is { Count: > 0 } before && !before.SequenceEqual(o.Buffs!)).Select(o => roleBySlot.GetValueOrDefault(o.Key)).OfType<string>().ToList();
                    var (drawn, how) = first.Count == count ? (first, "a first buff drawn on") : changed.Count == count ? (changed, "the buff icon changed on") : ([], "");
                    if (drawn.Count != count) continue;
                    pins.Add(new ExtractedPin(name, "[" + string.Join(",", drawn) + "]", $"{how} {string.Join(", ", drawn)} at toolbox step {t} \"{plan.Steps[t].Label}\""));
                    found = true;
                }
                if (!found) skipped.Add($"{name}: toolbox steps {aligned.Value.Toolbox}-{end - 1} (\"{plan.Steps[aligned.Value.Toolbox].Label}\"…) draw no group of {count} new buffs");
                continue;
            }
        }

        // Bearings: an add named like the roll, else the rolls of one kind paired with the numbered
        // adds of one kind (Sister 1, Sister 2) in declaration order, read at the toolbox step the
        // lesson aligns to the mechanic the roll names.
        var unnamed = new List<(PropertyInfo Property, string Stem)>();
        foreach (var (property, stem) in bearingPairs)
        {
            var tokens = Tokens(stem);
            List<ToolboxObject> Named(int t) => plan.Steps[t].Objects.Where(o => o.Kind is "add" or "boss" && tokens.Any(tk => o.Label.Contains(tk, StringComparison.OrdinalIgnoreCase))).ToList();
            var aligned = Aligned(alignedSteps, tokens, t => Named(t).Count == 1);
            var adds = aligned is null ? [] : Named(aligned.Value.Toolbox);
            if (adds.Count == 1) pins.Add(new ExtractedPin(property.Name, F(Bearing(adds[0])), $"add \"{adds[0].Label}\" at toolbox step {aligned!.Value.Toolbox}"));
            else unnamed.Add((property, stem));
        }
        foreach (var group in unnamed.GroupBy(p => Regex.Replace(p.Stem, "^[A-Z][a-z]+", "")))
        {
            var props = group.ToList();
            var tokens = Tokens(group.Key);
            IGrouping<string, ToolboxObject>? Numbered(int t) => plan.Steps[t].Objects.Where(o => o.Kind == "add" && Regex.IsMatch(o.Label, @"\s?\d$"))
                .GroupBy(o => Regex.Replace(o.Label, @"\s?\d$", "")).FirstOrDefault(g => g.Count() == props.Count);
            // Only steps that draw a group of that many numbered adds can name the roll.
            var aligned = Aligned(alignedSteps, tokens.Concat(props.SelectMany(p => Tokens(p.Stem))).Distinct().ToList(), t => Numbered(t) != null);
            if (aligned is null) { skipped.AddRange(props.Select(p => $"{p.Property.Name}: no add named like it and no aligned lesson step naming it draws {props.Count} numbered adds")); continue; }
            var at = aligned.Value.Toolbox;
            var numbered = Numbered(at)!;
            var ordered = numbered.OrderBy(o => o.Label).ToList();
            for (var i = 0; i < props.Count; i++)
                pins.Add(new ExtractedPin(props[i].Property.Name, F(Bearing(ordered[i])), $"\"{ordered[i].Label}\" at toolbox step {at} \"{plan.Steps[at].Label}\" (assumed {ordered[i].Label} = {props[i].Stem})"));
        }

        return new ExampleExtraction(pins.ToDictionary(p => p.Property, p => p.Value), pins, skipped);
    }

    // Distinct placements of `count` adds named like the roll, in toolbox order: a new placement
    // when any add moved, and never a stacked group (adds parked on one spot).
    private static List<(int Step, List<Vector2> Positions)> Clusters(ToolboxPlan plan, List<string> tokens, int count)
    {
        var clusters = new List<(int, List<Vector2>)>();
        List<Vector2>? last = null;
        foreach (var step in plan.Steps)
        {
            var adds = step.Objects.Where(o => o.Kind == "add" && tokens.Any(t => o.Label.Contains(t, StringComparison.OrdinalIgnoreCase))).ToList();
            if (adds.Count != count) { last = null; continue; }
            var positions = adds.Select(o => new Vector2(o.X, o.Z)).ToList();
            var stacked = positions.All(p => Vector2.Distance(p, positions[0]) < StackedYalms);
            if (stacked) { last = null; continue; }
            if (last != null && positions.Zip(last).All(pair => Vector2.Distance(pair.First, pair.Second) < MovedYalms)) continue;
            clusters.Add((step.Index, positions));
            last = positions;
        }
        return clusters;
    }

    private static (List<Vector2> Snapped, string? SpotSet) Snap(List<Vector2> positions, Type? stateType, List<string> tokens)
    {
        if (stateType is null) return (positions, null);
        foreach (var member in stateType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Cast<MemberInfo>()
                     .Concat(stateType.GetProperties(BindingFlags.Static | BindingFlags.Public)))
        {
            if (!Tokens(member.Name).Intersect(tokens, StringComparer.OrdinalIgnoreCase).Any()) continue;
            var value = member is FieldInfo f ? f.GetValue(null) : ((PropertyInfo)member).GetValue(null);
            var spots = (value as System.Collections.IEnumerable)?.Cast<object>().Select(o => o switch
            {
                Vector3 v => new Vector2(v.X, v.Z), Vector2 v => v, _ => (Vector2?)null,
            }).ToList();
            if (spots is null || spots.Count == 0 || spots.Any(s => s is null)) continue;
            var set = spots.Select(s => s!.Value).ToList();
            return (positions.Select(p => set.MinBy(s => Vector2.Distance(s, p))).ToList(), member.Name);
        }
        return (positions, null);
    }

    private static bool IsVectorList(Type t)
        => t.IsGenericType && t.GetGenericArguments() is [var item] && (item == typeof(Vector2) || item == typeof(Vector3)) || t.IsArray && (t.GetElementType() == typeof(Vector2) || t.GetElementType() == typeof(Vector3));

    private static bool IsRoleList(Type t)
        => t.IsGenericType && t.GetGenericArguments() is [var item] && item == typeof(Core.Game.Party.PartyRole) || t.IsArray && t.GetElementType() == typeof(Core.Game.Party.PartyRole);

    // "SatinPlumesFirst" -> ["Satin", "Plumes"]: the words a plan's add label could carry.
    public static List<string> Tokens(string name)
        => Regex.Split(name, @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])").Where(t => t.Length >= 3 && !Noise.Contains(t)).Select(t => t.TrimEnd('s')).Distinct().ToList();

    // The aligned step whose title or cue names the most of the roll's words; none when nothing does.
    private static (LessonStep Lesson, int Toolbox)? Aligned(List<(LessonStep Lesson, int Toolbox)> aligned, IReadOnlyList<string> tokens, Func<int, bool>? usable = null)
    {
        var best = aligned.Where(a => usable is null || usable(a.Toolbox))
            .Select(a => (a, Score: tokens.Count(t => $"{a.Lesson.Title} {a.Lesson.Cue}".Contains(t, StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenBy(x => x.a.Toolbox).FirstOrDefault();
        return best.Score > 0 ? best.a : null;
    }

    // Compass degrees of an add from the arena centre, on the 45° grid the scenarios roll.
    private static float Bearing(ToolboxObject o)
    {
        var degrees = (MathF.Atan2(o.X, -o.Z) * 180f / MathF.PI + 360f) % 360f;
        return MathF.Round(degrees / 45f) * 45f % 360f;
    }

    private static string F(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);
}
