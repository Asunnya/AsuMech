using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game;

namespace AnoMech.Learn.Toolbox;

// One raidplan host: turns the raw document its link resolves to into a ToolboxPlan, calibrated
// to the zone's waymark layout, with the same slot keys (tank1…dps4, waymarkA…) every host shares
// so the role map, alignment, oracle and viewer stay one code path.
public interface IRaidplanProvider
{
    RaidplanHost Host { get; }
    (ToolboxPlan Plan, ImportReport Report) Convert(byte[] raw, string id, WaymarkLayout layout);
}

public static class Raidplans
{
    public static readonly IReadOnlyList<IRaidplanProvider> All = [new ToolboxProvider(), new RaidplanIoProvider(), new Stgy.StgyProvider(), new XivplanProvider()];

    public static IRaidplanProvider For(RaidplanHost host)
        => All.FirstOrDefault(p => p.Host == host) ?? throw new ArgumentException($"no importer for {host}");

    public static (ToolboxPlan Plan, ImportReport Report) Convert(PlanRef plan, byte[] raw, WaymarkLayout layout)
        => For(plan.Host).Convert(raw, plan.Id, layout);
}

public sealed class ToolboxProvider : IRaidplanProvider
{
    public RaidplanHost Host => RaidplanHost.Toolbox;
    public (ToolboxPlan Plan, ImportReport Report) Convert(byte[] raw, string id, WaymarkLayout layout) => ToolboxImport.Convert(raw, id, layout);
}
