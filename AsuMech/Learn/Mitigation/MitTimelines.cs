using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AsuMech.Learn.Mitigation;

// Embedded cast timelines and plans, by scenario type name and plan name.
public static class MitTimelines
{
    private const string TimelinePrefix = "AsuMech.Learn.Mitigation.Timelines.";
    private const string PlanPrefix = "AsuMech.Learn.Mitigation.Plans.";

    public static CastTimeline? Load(string scenarioType) => Read($"{TimelinePrefix}{scenarioType}.json") is { } json ? CastTimeline.Parse(json) : null;

    public static MitPlan? LoadPlan(string name) => Read($"{PlanPrefix}{name}.json") is { } json ? MitPlan.Parse(json) : null;

    public static IEnumerable<string> PlanNames()
        => Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(n => n.StartsWith(PlanPrefix, StringComparison.Ordinal))
            .Select(n => n[PlanPrefix.Length..^".json".Length]);

    // Plans that cover a scenario.
    public static IEnumerable<MitPlan> PlansFor(string scenarioType)
        => PlanNames().Select(LoadPlan).OfType<MitPlan>().Where(p => p.Scenarios.ContainsKey(scenarioType));

    private static string? Read(string resource)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resource);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
