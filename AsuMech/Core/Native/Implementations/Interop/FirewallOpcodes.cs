using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AsuMech.Core.Native.Implementations.Interop;

// One game version's firewall opcodes, from Hyperborea's published list: ZoneUp is the heartbeat,
// the one packet the send filter lets out; ZoneDown are the only packets the receive filter lets
// in. The list comes off the network, so anything outside a sane shape is refused rather than
// trusted with what the firewall passes.
internal sealed record FirewallOpcodes(ushort ZoneUp, uint[] ZoneDown)
{
    public const int MaxZoneDown = 8;

    public static FirewallOpcodes? Parse(string text, out string? problem)
    {
        problem = null;
        ushort? zoneUp = null;
        uint[]? zoneDown = null;
        foreach (var raw in text.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("ZoneUp=", StringComparison.Ordinal))
            {
                if (!ushort.TryParse(line["ZoneUp=".Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var up) || up == 0)
                    return Fail($"ZoneUp is not an opcode ({line})", out problem);
                zoneUp = up;
            }
            else if (line.StartsWith("ZoneDown=", StringComparison.Ordinal))
            {
                var values = new List<uint>();
                foreach (var part in line["ZoneDown=".Length..].Split(','))
                {
                    if (!ushort.TryParse(part.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var down) || down == 0)
                        return Fail($"ZoneDown holds something that is not an opcode ({part.Trim()})", out problem);
                    values.Add(down);
                }
                zoneDown = values.Distinct().ToArray();
            }
        }
        if (zoneUp is not { } heartbeat) return Fail("no ZoneUp line", out problem);
        if (zoneDown is not { Length: > 0 } allowed) return Fail("no ZoneDown line", out problem);
        if (allowed.Length > MaxZoneDown) return Fail($"ZoneDown lets {allowed.Length} opcodes in, more than the {MaxZoneDown} a firewall needs", out problem);
        if (allowed.Contains(heartbeat)) return Fail("ZoneUp is also listed as ZoneDown", out problem);
        return new FirewallOpcodes(heartbeat, allowed);
    }

    // The newest published file before this game version (its own just 404'd): a patch the list has no file for yet
    // most often kept the opcodes, and the heartbeat check still refuses a list the binary disagrees with.
    public static string? LatestPublishedFor(IEnumerable<string> fileNames, string gameVersion)
        => fileNames.Where(n => n.EndsWith(".txt", StringComparison.Ordinal)).Select(n => n[..^4])
            .Where(v => string.CompareOrdinal(v, gameVersion) < 0).OrderBy(v => v, StringComparer.Ordinal).LastOrDefault();

    // A refused file for the running patch drops the stored list only when that list was never this
    // patch's own: one fetched for it before already passed the heartbeat check.
    public static bool RefusedOwnFileForgets(string current, string stored, string storedFrom)
        => stored != current || storedFrom.Length > 0;

    private static FirewallOpcodes? Fail(string why, out string? problem)
    {
        problem = why;
        return null;
    }

    // Why a stay may not start on these opcodes, or null. `stored` is the game version the list was
    // fetched for; `heartbeat` the opcode read off the running binary.
    public static string? Problem(string current, string stored, ushort storedZoneUp, int zoneDownCount, ushort heartbeat)
    {
        if (stored != current || storedZoneUp == 0 || zoneDownCount == 0)
            return "the firewall's opcodes for this game patch are not available yet (they come from Hyperborea's list); try again once it is published";
        if (heartbeat != storedZoneUp)
            return $"the heartbeat opcode read from the game ({heartbeat}) does not match the published one ({storedZoneUp}), so the send filter could let the wrong packet out";
        return null;
    }
}
