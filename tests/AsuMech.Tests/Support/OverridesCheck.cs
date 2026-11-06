using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AsuMech.Learn.Toolbox;
using AsuMech.Learn.Tools;

namespace AsuMech.Tests;

// Pins each roll of a scenario's *State to the value a free run drew and runs the seed again:
// a State that draws every roll before a pin replaces it leaves the other rolls untouched; one
// that short-circuits the draw shifts them. Settable State members (filled mid-run) are left out.
internal static class OverridesCheck
{
    private const float StopAtSeconds = 3f;

    public static PinReport Run(Type scenarioType, int strat = 0, int seed = 1)
    {
        new DalamudServices().InstallNullServices();
        var free = Snapshot(scenarioType, strat, seed, null, out var overridesType);
        var rows = new List<PinRow>();
        if (overridesType is null) return new PinReport(scenarioType.Name, seed, rows, "no SettingsOverrides");
        var state = StateType(scenarioType);
        var rolls = state is null ? free.Keys.ToList()
            : LearnReadiness.Members(state).Where(m => m is PropertyInfo { CanWrite: false } or FieldInfo { IsInitOnly: true }).Select(m => m.Name).Where(free.ContainsKey).ToList();
        foreach (var name in rolls)
        {
            var property = overridesType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property is null) { rows.Add(new PinRow(name, PinStatus.NotPinnable, "no override of that name")); continue; }
            if (ExampleOverrides.Parse(free[name], property.PropertyType) is null && !property.PropertyType.IsArray && !IsPerRole(property.PropertyType))
            {
                rows.Add(new PinRow(name, PinStatus.NotPinnable, $"{free[name]} does not read as {property.PropertyType.Name}"));
                continue;
            }
            var example = new Dictionary<string, string> { [property.Name] = free[name] };
            var pinned = Snapshot(scenarioType, strat, seed, example, out _);
            var shifted = free.Keys.Where(k => k != name && pinned.TryGetValue(k, out var v) && v != free[k]).ToList();
            var own = pinned.TryGetValue(name, out var self) && self != free[name] ? $"the pin reads back as {self}" : null;
            rows.Add(shifted.Count > 0 ? new PinRow(name, PinStatus.Shifts, $"shifts {string.Join(", ", shifted)}")
                : own != null ? new PinRow(name, PinStatus.NoRoundTrip, own)
                : new PinRow(name, PinStatus.Ok, ""));
        }
        return new PinReport(scenarioType.Name, seed, rows, null);
    }

    private static bool IsPerRole(Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AsuMech.Scenarios.PerRoleSetting<>);

    private static Type? StateType(Type scenarioType)
        => scenarioType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(f => f.FieldType).FirstOrDefault(t => t.Name.EndsWith("State", StringComparison.Ordinal));

    private static Dictionary<string, string> Snapshot(Type scenarioType, int strat, int seed, IReadOnlyDictionary<string, string>? example, out Type? overridesType)
    {
        var state = new Dictionary<string, string>();
        Type? overrides = null;
        var frames = 0;
        ScenarioRun.Execute(scenarioType, strat, seed, new ScenarioRunOptions
        {
            StopAt = StopAtSeconds,
            WriteArtifactsOnFailure = false,
            Overrides = o =>
            {
                overrides = o.GetType();
                if (example != null) ExampleOverrides.Apply(o, example);
            },
            Probe = p =>
            {
                if (frames++ % 30 == 0 && p.Game.ActiveScenario is { } scenario) state = StateSnapshot.Of(scenario);
            },
        });
        overridesType = overrides;
        return state;
    }
}
