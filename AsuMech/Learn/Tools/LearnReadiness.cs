using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AsuMech.Learn.Mitigation;
using AsuMech.Learn.Toolbox;
using AsuMech.Scenarios;

namespace AsuMech.Learn.Tools;

public enum Readiness { Ok, Degraded, Missing }

// One thing the authoring chain reads from a scenario, whether it is there, and what the chain
// does without it.
public sealed record ReadinessRow(string Reads, Readiness Status, string Detail);

// What a scenario offers the authoring chain, by reflection and the catalog alone; the orders and
// events rows need a dump and say so without one. Nothing here asks a scenario to change.
public static class LearnReadiness
{
    public static List<ReadinessRow> Check(IScenario scenario, OrderDump? dump = null, PinReport? pins = null)
    {
        var rows = new List<ReadinessRow>();
        var type = scenario.GetType();
        var family = Glossary.FamilyOf(type);

        if (dump is null) rows.Add(new("orders for the player's slot", Readiness.Degraded, "not recorded (run without --no-run); a strat that never moves the player is Observe only"));
        else
        {
            var byRole = dump.Orders.GroupBy(o => o.Role).ToDictionary(g => g.Key, g => g.Select(o => (o.Cls, o.Line)).Distinct().Count());
            var silent = Enum.GetNames<Core.Game.Party.PartyRole>().Where(r => !byRole.ContainsKey(r)).ToList();
            rows.Add(byRole.Count == 0 ? new("orders for the player's slot", Readiness.Missing, "the strat never moves the player: Observe only, no lesson")
                : silent.Count > 0 ? new("orders for the player's slot", Readiness.Degraded, $"{byRole.Values.Max()} steps; no orders for {string.Join(", ", silent)}")
                : new("orders for the player's slot", Readiness.Ok, $"{byRole.Values.Max()} distinct calls, every role ordered"));
        }

        var states = StateTypes(scenario);
        var members = states.SelectMany(s => Members(s)).ToList();
        rows.Add(states.Count == 0 ? new("*State public members", Readiness.Missing, "no *State field on the scenario: every spot is 'geometric', the reviewer writes each rule")
            : members.Count == 0 ? new("*State public members", Readiness.Missing, $"{string.Join(", ", states.Select(s => s.Name))} exposes nothing public: every spot is 'geometric'")
            : new("*State public members", Readiness.Ok, $"{members.Count} ({string.Join(", ", members.Select(m => m.Name))})"));

        var glossary = Glossary.Load(family);
        var unworded = members.Where(IsRoll).Select(m => m.Name).Where(n => !glossary.Fields.ContainsKey(n)).ToList();
        rows.Add(members.Count == 0 ? new("glossary", Readiness.Ok, "nothing to word")
            : glossary.Fields.Count == 0 ? new("glossary", Readiness.Degraded, $"no Lessons/{family}/glossary.json: member names appear as they are in the draft")
            : unworded.Count == 0 ? new("glossary", Readiness.Ok, "every member worded")
            : new("glossary", Readiness.Degraded, $"not worded: {string.Join(", ", unworded)}"));

        var overrides = scenario.SettingsOverrides;
        if (overrides is null) rows.Add(new("*StateOverrides", Readiness.Missing, "no SettingsOverrides: replay by example seed only, nothing pinned"));
        else
        {
            var props = overrides.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var pinnable = members.Where(m => props.Contains(m.Name)).Select(m => m.Name).ToList();
            var unpinnable = members.Where(m => !props.Contains(m.Name) && IsRoll(m)).Select(m => m.Name).ToList();
            rows.Add(members.Count == 0 ? new("*StateOverrides", Readiness.Ok, overrides.GetType().Name)
                : pinnable.Count == 0 ? new("*StateOverrides", Readiness.Degraded, $"{overrides.GetType().Name} shares no name with the State: replay by example seed only")
                : unpinnable.Count == 0 ? new("*StateOverrides", Readiness.Ok, $"every roll pinnable ({pinnable.Count})")
                : new("*StateOverrides", Readiness.Degraded, $"pinnable: {string.Join(", ", pinnable)}; not: {string.Join(", ", unpinnable)}"));
            rows.Add(pins is null ? new("State draws before pinning", Readiness.Degraded, "not checked (overrides-check); a pin may shift the seed's other rolls")
                : pins.Clean ? new("State draws before pinning", Readiness.Ok, pins.Summary())
                : new("State draws before pinning", Readiness.Degraded, pins.Summary()));
        }

        var presets = scenario.Phase.Zone.WaymarkPresets;
        rows.Add(presets.Count == 0 ? new("waymark presets", Readiness.Missing, "spots named by compass only; no toolbox calibration")
            : new("waymark presets", Readiness.Ok, string.Join(", ", presets.Select(p => p.Name))));

        var plans = PlanRegistry.For(type.Name);
        if (plans.Count == 0) rows.Add(new("plans.json", Readiness.Missing, "no raidplan registered: lesson without a toolbox block, viewer off"));
        else
        {
            var bad = plans.Where(p => p.Ref.Host == RaidplanHost.Unknown).Select(p => p.Url).ToList();
            var badLayout = plans.Where(p => p.Layout != null && presets.All(w => !w.Name.Equals(p.Layout, StringComparison.OrdinalIgnoreCase))).Select(p => p.Layout!).ToList();
            rows.Add(bad.Count > 0 ? new("plans.json", Readiness.Degraded, $"unknown host: {string.Join(", ", bad)}")
                : badLayout.Count > 0 ? new("plans.json", Readiness.Degraded, $"layout not a preset of the zone: {string.Join(", ", badLayout)}")
                : new("plans.json", Readiness.Ok, string.Join("; ", plans.Select(p => $"{p.Ref.Host} {p.Ref.Id}{(p.Strat is null ? "" : $" ({p.Strat})")}"))));
        }

        if (dump?.Runs is { Count: > 0 } runs)
        {
            var kinds = runs.SelectMany(r => r.Events).GroupBy(e => e.Kind).ToDictionary(g => g.Key, g => g.Count() / runs.Count);
            rows.Add(kinds.Count == 0 ? new("sim events", Readiness.Degraded, "nothing visible recorded: steps without cues")
                : new("sim events", Readiness.Ok, string.Join(", ", kinds.OrderBy(kv => kv.Key).Select(kv => $"{kv.Value} {kv.Key}"))));
        }
        else rows.Add(new("sim events", Readiness.Degraded, "not recorded"));

        var timeline = MitTimelines.Load(type.Name);
        var mitPlans = MitTimelines.PlansFor(type.Name).Select(p => p.Plan).ToList();
        rows.Add(timeline is null ? new("mit timeline + plan", Readiness.Missing, "no embedded timeline (mit-timeline): Mit plan toggle greyed")
            : mitPlans.Count == 0 ? new("mit timeline + plan", Readiness.Degraded, $"{timeline.Casts.Count} casts, no plan covers it")
            : new("mit timeline + plan", Readiness.Ok, $"{timeline.Casts.Count} casts; plans: {string.Join(", ", mitPlans)}"));
        return rows;
    }

    public static string Table(IScenario scenario, IReadOnlyList<ReadinessRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{scenario.GetType().Name} ({scenario.FullName()}): {rows.Count(r => r.Status == Readiness.Ok)} ok, {rows.Count(r => r.Status == Readiness.Degraded)} degraded, {rows.Count(r => r.Status == Readiness.Missing)} missing");
        foreach (var r in rows) sb.AppendLine($"  {r.Status,-8} {r.Reads,-30} {r.Detail}");
        return sb.ToString();
    }

    // Every *State object the scenario holds, as StateSnapshot finds them.
    public static List<Type> StateTypes(IScenario scenario)
        => scenario.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => f.FieldType.Name.EndsWith("State", StringComparison.Ordinal))
            .Select(f => f.FieldType).Distinct().ToList();

    public static IEnumerable<MemberInfo> Members(Type state)
        => state.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Length == 0).Cast<MemberInfo>()
            .Concat(state.GetFields(BindingFlags.Instance | BindingFlags.Public));

    // Read-only members are rolls; settable ones are filled in mid-run (an intercept, a tether).
    private static bool IsRoll(MemberInfo m) => m is PropertyInfo { CanWrite: false } or FieldInfo { IsInitOnly: true };
}
