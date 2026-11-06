using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AsuMech.Learn.Toolbox;

// Compact plans embedded under Learn/Toolbox/Plans/<id>.json, loaded once each.
public static class ToolboxPlans
{
    private const string ResourcePrefix = "AsuMech.Learn.Toolbox.Plans.";
    private static readonly Dictionary<string, ToolboxPlan?> Cache = new();

    public static ToolboxPlan? Load(string id)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(id, out var cached)) return cached;
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n == $"{ResourcePrefix}{id}.json");
            ToolboxPlan? plan = null;
            if (name != null)
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                plan = ToolboxPlan.Parse(reader.ReadToEnd());
            }
            Cache[id] = plan;
            return plan;
        }
    }

    public static IEnumerable<string> Ids()
        => Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Select(n => n[ResourcePrefix.Length..^".json".Length]);
}
