using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AsuMech.Learn.Mitigation;

namespace AsuMech.Learn.Tools;

// The visible events of a headless run, read from the harness's timeline CSV. Entity ids become
// party roles through the seat map the recorder took from the world.
public static class TimelineEvents
{
    private const string HeadMarkerCategory = "0x22";
    private const string SetTetherCategory = "0x23";

    public static List<DumpedEvent> Parse(string csv, IReadOnlyDictionary<string, string> seatByEntity, Func<ushort, string?> statusName, System.Numerics.Vector3 origin = default)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return [];
        var header = lines[0].TrimEnd('\r').Split(',');
        int Col(string name) => Array.IndexOf(header, name);
        var (tCol, kindCol, actorCol, actorNameCol, idCol, nameCol, targetCol, targetNameCol, xCol, zCol, detailCol, pCol) =
            (Col("t"), Col("kind"), Col("actor"), Col("actor_name"), Col("id"), Col("name"), Col("target"), Col("target_name"), Col("x"), Col("z"), Col("detail"), Col("p"));
        var seatById = new Dictionary<uint, string>();
        foreach (var (hex, role) in seatByEntity)
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)) seatById[id] = role;
        string? Seat(string hex) => hex.StartsWith("0x") && uint.TryParse(hex[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id) && seatById.TryGetValue(id, out var role) ? role : null;
        var events = new List<DumpedEvent>();
        foreach (var line in lines.Skip(1))
        {
            var cells = CastTimeline.SplitCsv(line.TrimEnd('\r'));
            if (cells.Count <= Math.Max(detailCol, pCol)) continue;
            if (!float.TryParse(cells[tCol], NumberStyles.Float, CultureInfo.InvariantCulture, out var t)) continue;
            string Who(int idCol, int nameCol) => seatByEntity.TryGetValue(cells[idCol], out var role) ? role : cells[nameCol];
            var actor = Who(actorCol, actorNameCol);
            var target = Who(targetCol, targetNameCol);
            // Orders are arena-relative; so are events, once the zone origin is taken off.
            float? X = float.TryParse(cells[xCol], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x - origin.X : null;
            float? Z = float.TryParse(cells[zCol], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ? z - origin.Z : null;
            switch (cells[kindCol])
            {
                case "cast":
                    events.Add(new DumpedEvent(t, "cast", cells[nameCol], actor, target, X, Z, cells[detailCol]));
                    break;
                case "ability":
                    events.Add(new DumpedEvent(t, "ability", cells[nameCol], actor, target, X, Z, null));
                    break;
                case "status_gain":
                    if (ushort.TryParse(cells[idCol], out var statusId))
                        events.Add(new DumpedEvent(t, "status", statusName(statusId) ?? $"status {statusId}", target, actor, null, null, cells[detailCol]));
                    break;
                case "spawn":
                    events.Add(new DumpedEvent(t, "spawn", cells[actorNameCol], null, null, X, Z, null));
                    break;
                // Head markers and tethers ride on actor controls: the row's actor is the marked or
                // tethered one, the tether's other end is in the params.
                case "ac" or "ac_self" or "ac_target":
                    var p = cells[pCol].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (cells[idCol] == HeadMarkerCategory && p.Length > 0)
                        events.Add(new DumpedEvent(t, "marker", $"marker {p[0]}", null, actor, null, null, null));
                    else if (cells[idCol] == SetTetherCategory && p.Length > 1)
                        events.Add(new DumpedEvent(t, "tether", $"tether {p[1]}", actor, p.Length > 2 ? Seat(p[2]) ?? target : target, null, null, null));
                    break;
            }
        }
        // Many casters casting the same thing at once (feathers, sisters) read as one event.
        return events.Where((e, i) => !events.Take(i).Any(o => o.Kind == e.Kind && o.Name == e.Name && o.Target == e.Target && Math.Abs(o.Time - e.Time) < 0.5f)).ToList();
    }
}
