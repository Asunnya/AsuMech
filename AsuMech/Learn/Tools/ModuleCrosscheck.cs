using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Native.Interfaces;
using AsuMech.Learn.Mitigation;
using AsuMech.Learn.Sources;
using AsuMech.Scenarios;

namespace AsuMech.Learn.Tools;

// One EnemyAction of a family against one public module: the id is there or not, and the cast
// time, shape words, cone angle, donut hole and state length agree or don't. A disagreement is
// reported with both values and left for a human; the modules disagree with each other too.
public sealed record CrosscheckRow(string Action, uint Id, string Source, string Status, string Detail);

public static class ModuleCrosscheck
{
    public const string Ok = "ok", Differs = "differs", Missing = "missing";
    private const float CastSlack = 0.05f;
    private const float AngleSlack = 0.5f;
    private const float StateSlack = 0.25f;

    // The family's *Actions classes: every public static EnemyAction field, by field name.
    public static List<(string Name, EnemyAction Action)> FamilyActions(string family)
    {
        var ns = $"AsuMech.Scenarios.{family}";
        return typeof(ScenarioCatalog).Assembly.GetTypes()
            .Where(t => t.Namespace == ns && t.IsAbstract && t.IsSealed)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(EnemyAction)))
            .Select(f => (f.Name, (EnemyAction)f.GetValue(null)!))
            .OrderBy(x => x.Item2.ActionId)
            .ToList();
    }

    public static List<CrosscheckRow> Check(string family, ModuleSource modules, IGameData? data, CastTimeline? timeline)
    {
        var rows = new List<CrosscheckRow>();
        foreach (var (name, action) in FamilyActions(family))
        foreach (var source in modules.Sources)
            rows.Add(Check(name, action, source, data?.Action(action.ActionId), timeline));
        return rows;
    }

    private static CrosscheckRow Check(string name, EnemyAction action, ModuleSourceBlock source, ActionRow? sheet, CastTimeline? timeline)
    {
        var fact = source.Action(action.ActionId);
        if (fact is null) return new CrosscheckRow(name, action.ActionId, source.Name, Missing, "id not in the module");
        var notes = new List<string>();

        var cast = action.Cast.CastSecondsOverride ?? sheet?.CastSeconds;
        if (cast is { } c && fact.CastSeconds is { } fc && MathF.Abs(c - fc) > CastSlack)
            notes.Add($"cast {F(c)}s vs {F(fc)}s{(fact.CastExtraSeconds is { } e ? $"+{F(e)}" : "")}");

        var kind = action.Area.CastTypeOverride ?? sheet?.CastType;
        if (kind is { } k && fact.Shape is { } shape && shape.Kind != "single-target")
        {
            var ours = KindWord(k);
            if (ours != shape.Kind) notes.Add($"shape {ours} vs {shape.Kind}");
            else if (sheet is { } row && shape.Range is { } r && row.EffectRange != 0 && MathF.Abs(row.EffectRange - r) > 0.5f)
                notes.Add($"range {row.EffectRange} vs {F(r)}");
            if (ours == "rect" && sheet is { } rect && shape.Width is { } w && rect.XAxisModifier != 0 && MathF.Abs(rect.XAxisModifier - w) > 0.5f)
                notes.Add($"width {rect.XAxisModifier} vs {F(w)}");
        }

        var codeShapes = source.ShapesFor(fact.Name).ToList();
        if (kind is CastType.Cone or CastType.Cone2)
        {
            var oursDeg = 2f * (action.Area.Size ?? MathF.PI / 6f) * 180f / MathF.PI;
            var theirs = codeShapes.Where(s => s.Kind == "cone" && s.Angle is { }).Select(s => s.Angle!.Value)
                .Concat(fact.Shape?.Angle is { } a ? [a] : Array.Empty<float>()).Distinct().ToList();
            if (theirs.Count > 0 && theirs.All(t => MathF.Abs(t - oursDeg) > AngleSlack))
                notes.Add($"cone {F(oursDeg)}° vs {string.Join("/", theirs.Select(t => F(t) + "°"))}");
        }
        if (kind == CastType.Donut)
        {
            var oursInner = action.Area.Size ?? 0f;
            var theirs = codeShapes.Where(s => s.Kind == "donut" && s.Inner is { }).Select(s => s.Inner!.Value)
                .Concat(fact.Shape?.Inner is { } i ? [i] : Array.Empty<float>()).Distinct().ToList();
            if (theirs.Count > 0 && theirs.All(t => MathF.Abs(t - oursInner) > 0.5f))
                notes.Add($"donut hole {F(oursInner)} vs {string.Join("/", theirs.Select(F))}");
        }

        if (timeline is { } tl && tl.Casts.FirstOrDefault(e => e.ActionId == action.ActionId) is { } cast1 && cast1.Resolve > cast1.Start)
        {
            var lengths = source.States.Where(s => s.Kind == "Cast" && s.Actions.Contains(fact.Name) && s.Numbers.Count >= 2).Select(s => s.Numbers[1]).Distinct().ToList();
            // The timeline holds the bar the sim shows, which is the sheet's cast less the effect offset the modules keep.
            var ours = cast1.Resolve - cast1.Start + CastSpec.ActionEffectOffset;
            if (lengths.Count > 0 && lengths.All(l => MathF.Abs(l - ours) > StateSlack))
                notes.Add($"state {F(ours)}s vs {string.Join("/", lengths.Select(F))}s");
        }

        return notes.Count == 0
            ? new CrosscheckRow(name, action.ActionId, source.Name, Ok, fact.Name)
            : new CrosscheckRow(name, action.ActionId, source.Name, Differs, $"{fact.Name}: {string.Join("; ", notes)}");
    }

    private static string KindWord(CastType type) => type switch
    {
        CastType.Circle or CastType.Circle2 or CastType.GroundCircle => "circle",
        CastType.Cone or CastType.Cone2 => "cone",
        CastType.Rectangle or CastType.Rectangle2 or CastType.Charge => "rect",
        CastType.Donut => "donut",
        CastType.Cross => "cross",
        _ => "single-target",
    };

    // One readiness row per source, for learn-ready.
    public static List<ReadinessRow> Summarize(string family, IReadOnlyList<CrosscheckRow> rows, ModuleSource? modules)
    {
        if (modules is null) return [new ReadinessRow("public modules", Readiness.Missing, $"no Learn/Sources/modules/{family.ToLowerInvariant()}.json (crosscheck --fight {family.ToLowerInvariant()})")];
        var result = new List<ReadinessRow>();
        foreach (var source in modules.Sources)
        {
            var mine = rows.Where(r => r.Source == source.Name).ToList();
            var found = mine.Count(r => r.Status != Missing);
            var differs = mine.Where(r => r.Status == Differs).ToList();
            var missing = mine.Where(r => r.Status == Missing).Select(r => r.Action).ToList();
            var detail = $"{found}/{mine.Count} ids found @{source.Commit[..Math.Min(8, source.Commit.Length)]}";
            if (differs.Count > 0) detail += $"; differs: {string.Join(", ", differs.Select(d => $"{d.Action} ({d.Detail[(d.Detail.IndexOf(": ", StringComparison.Ordinal) + 2)..]})"))}";
            if (missing.Count > 0) detail += $"; not in module: {string.Join(", ", missing)}";
            result.Add(new ReadinessRow($"module {source.Name}", differs.Count == 0 && missing.Count == 0 ? Readiness.Ok : Readiness.Degraded, detail));
        }
        return result;
    }

    private static string F(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
