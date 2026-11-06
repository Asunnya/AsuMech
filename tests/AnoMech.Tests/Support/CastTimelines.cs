using System;
using System.Collections.Generic;
using System.IO;
using AnoMech.Learn.Mitigation;

namespace AnoMech.Tests;

// A scenario's cast timeline from one headless run (strat 0, seed 1 by default): the boss's cast
// times do not depend on the roll, so one run stands for the scenario.
internal static class CastTimelines
{
    public static CastTimeline Record(Type scenarioType, int strat, int seed, out string? failure)
    {
        new DalamudServices().InstallNullServices();
        var csvPath = Path.GetTempFileName();
        try
        {
            var run = ScenarioRun.Execute(scenarioType, strat, seed, new ScenarioRunOptions { TimelinePath = csvPath, WriteArtifactsOnFailure = false });
            failure = run.Failure;
            return CastTimeline.FromTimelineCsv(scenarioType.Name, File.Exists(csvPath) ? File.ReadAllText(csvPath) : "t,kind,id,name,detail\n");
        }
        finally
        {
            if (File.Exists(csvPath)) File.Delete(csvPath);
        }
    }

    public static CastTimeline Record(Type scenarioType, int strat = 0, int seed = 1) => Record(scenarioType, strat, seed, out _);

    // The run's whole timeline, rows as SimTimeline wrote them.
    public static List<Dictionary<string, string>> SimRows(Type scenarioType, int strat, int seed, out string? failure)
    {
        new DalamudServices().InstallNullServices();
        var csvPath = Path.GetTempFileName();
        try
        {
            var run = ScenarioRun.Execute(scenarioType, strat, seed, new ScenarioRunOptions { TimelinePath = csvPath, WriteArtifactsOnFailure = false });
            failure = run.Failure;
            return File.Exists(csvPath) ? SimTimeline.Read(File.ReadAllText(csvPath)) : [];
        }
        finally
        {
            if (File.Exists(csvPath)) File.Delete(csvPath);
        }
    }
}
