using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace AnoMech.Core.Game.Trace;

// A recorded pull (PullRecorder) read back: the relay stores it gzipped, the plugin writes it plain.
public static class Recording
{
    public static List<Dictionary<string, string>> Load(string path)
    {
        if (!path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)) return SimTimeline.Read(File.ReadAllText(path));
        using var gzip = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return SimTimeline.Read(reader.ReadToEnd());
    }

    // One recording holds a whole lockout; a pull starts at each `duty start` row. Rows before the
    // first start (zone-in spawns, the first pull's snapshot) belong to the first pull.
    public static List<List<Dictionary<string, string>>> Pulls(IReadOnlyList<Dictionary<string, string>> rows)
    {
        var pulls = new List<List<Dictionary<string, string>>>();
        var current = new List<Dictionary<string, string>>();
        foreach (var row in rows)
        {
            if (row.GetValueOrDefault("kind") == "duty" && row.GetValueOrDefault("id") == "start" && current.Any(r => r.GetValueOrDefault("kind") is "cast" or "ability"))
            {
                pulls.Add(current);
                current = [];
            }
            current.Add(row);
        }
        if (current.Count > 0) pulls.Add(current);
        return pulls;
    }

    public static uint? Territory(IReadOnlyList<Dictionary<string, string>> rows)
        => rows.FirstOrDefault(r => r.GetValueOrDefault("kind") == "recording") is { } row && uint.TryParse(row.GetValueOrDefault("id"), out var id) ? id : null;
}

public sealed record CastRow(float T, uint ActionId, string Name, uint Actor, string ActorName, float? X, float? Z, float? Rot, string Target, string TargetName, float CastTime)
{
    public static CastRow? From(IReadOnlyDictionary<string, string> row)
    {
        if (row.GetValueOrDefault("kind") != "cast" || !uint.TryParse(row.GetValueOrDefault("id"), out var action)) return null;
        var detail = row.GetValueOrDefault("detail", "");
        var castTime = 0f;
        var at = detail.IndexOf("cast=", StringComparison.Ordinal);
        if (at >= 0)
        {
            var end = detail.IndexOf('s', at + 5);
            if (end > at && float.TryParse(detail[(at + 5)..end], NumberStyles.Float, CultureInfo.InvariantCulture, out var c)) castTime = c;
        }
        uint.TryParse(row.GetValueOrDefault("actor"), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var actor);
        return new CastRow(SimTimeline.Time(row), action, row.GetValueOrDefault("name", ""), actor, row.GetValueOrDefault("actor_name", ""),
            SimTimeline.Float(row, "x"), SimTimeline.Float(row, "z"), SimTimeline.Float(row, "rot"),
            row.GetValueOrDefault("target", ""), row.GetValueOrDefault("target_name", ""), castTime);
    }

    // Enemies' ids start with 4; a player's own casts are noise here.
    public bool IsEnemy => Actor >> 28 == 4;
}

// A headless run of a scenario against one recorded pull, cast by cast. Times are aligned on the
// first recorded enemy cast the scenario also has (or --t0), then each sim cast is paired with the
// nearest recorded cast of the same action inside Window; what pairs reports its drift, origin
// distance and facing difference, what doesn't is listed on its side. The recorded pull keeps
// going after the scenario ends, so RealOnly stops at the scenario's last cast plus Window.
public sealed class ReplayDiff
{
    public sealed record CastMatch(CastRow Sim, CastRow Real, float Dt, float? Distance, float? RotDelta);

    public float Offset { get; private init; }
    public CastRow? AlignedOn { get; private init; }
    public List<CastMatch> Matched { get; } = [];
    public List<CastRow> SimOnly { get; } = [];
    public List<CastRow> RealOnly { get; } = [];
    public int RealCastsAfterSimEnd { get; private set; }
    // EObj base ids and MapEffect index:state the recording has and the run never sent, with counts.
    public List<(string What, int Count)> RealExtras { get; } = [];

    public const float DefaultWindow = 3f;
    public const float DefaultSlack = 0.25f;

    public static ReplayDiff Compare(IReadOnlyList<Dictionary<string, string>> simRows, IReadOnlyList<Dictionary<string, string>> realRows,
        float? t0 = null, float window = DefaultWindow)
    {
        var simCasts = simRows.Select(CastRow.From).OfType<CastRow>().OrderBy(c => c.T).ToList();
        var realCasts = realRows.Select(CastRow.From).OfType<CastRow>().Where(c => c.IsEnemy).OrderBy(c => c.T).ToList();
        var simIds = simCasts.Select(c => c.ActionId).ToHashSet();
        var anchor = t0 is { } given
            ? realCasts.FirstOrDefault(c => c.T >= given && simIds.Contains(c.ActionId))
            : realCasts.FirstOrDefault(c => simIds.Contains(c.ActionId));
        var simAnchor = anchor is null ? null : simCasts.First(c => c.ActionId == anchor.ActionId);
        var diff = new ReplayDiff { AlignedOn = anchor, Offset = anchor is null || simAnchor is null ? 0f : anchor.T - simAnchor.T };
        if (anchor is null)
        {
            diff.SimOnly.AddRange(simCasts);
            return diff;
        }

        var taken = new bool[realCasts.Count];
        foreach (var sim in simCasts)
        {
            var expected = sim.T + diff.Offset;
            var best = -1;
            var bestDt = float.MaxValue;
            for (var i = 0; i < realCasts.Count; i++)
            {
                if (taken[i] || realCasts[i].ActionId != sim.ActionId) continue;
                var dt = realCasts[i].T - expected;
                if (MathF.Abs(dt) > window || MathF.Abs(dt) >= MathF.Abs(bestDt)) continue;
                best = i;
                bestDt = dt;
            }
            if (best < 0) { diff.SimOnly.Add(sim); continue; }
            taken[best] = true;
            var real = realCasts[best];
            float? distance = sim.X is { } sx && sim.Z is { } sz && real.X is { } rx && real.Z is { } rz ? MathF.Sqrt((sx - rx) * (sx - rx) + (sz - rz) * (sz - rz)) : null;
            float? rot = sim.Rot is { } sr && real.Rot is { } rr ? float.Ieee754Remainder(rr - sr, MathF.Tau) : null;
            diff.Matched.Add(new CastMatch(sim, real, bestDt, distance, rot));
        }

        var simEnd = (simCasts.Count > 0 ? simCasts.Max(c => c.T + c.CastTime) : 0f) + diff.Offset + window;
        var simStart = anchor.T - window;
        for (var i = 0; i < realCasts.Count; i++)
        {
            if (taken[i]) continue;
            if (realCasts[i].T > simEnd) diff.RealCastsAfterSimEnd++;
            else if (realCasts[i].T >= simStart) diff.RealOnly.Add(realCasts[i]);
        }

        var simEobjs = simRows.Where(r => r.GetValueOrDefault("kind") == "eobj_spawn").Select(r => r.GetValueOrDefault("id", "")).ToHashSet();
        var simEffects = simRows.Where(r => r.GetValueOrDefault("kind") == "map_effect").Select(MapEffectKey).ToHashSet();
        var extras = new Dictionary<string, int>();
        foreach (var row in realRows)
        {
            var t = SimTimeline.Time(row);
            if (t < simStart || t > simEnd) continue;
            var kind = row.GetValueOrDefault("kind");
            if (kind == "eobj_spawn" && !simEobjs.Contains(row.GetValueOrDefault("id", "")))
                extras[$"EObj {row.GetValueOrDefault("id")}"] = extras.GetValueOrDefault($"EObj {row.GetValueOrDefault("id")}") + 1;
            else if (kind == "map_effect" && !simEffects.Contains(MapEffectKey(row)))
                extras[$"MapEffect {MapEffectKey(row)}"] = extras.GetValueOrDefault($"MapEffect {MapEffectKey(row)}") + 1;
        }
        diff.RealExtras.AddRange(extras.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)));
        return diff;
    }

    private static string MapEffectKey(IReadOnlyDictionary<string, string> row)
    {
        var detail = row.GetValueOrDefault("detail", "");
        var at = detail.IndexOf("state=", StringComparison.Ordinal);
        var state = at < 0 ? "?" : new string(detail[(at + 6)..].TakeWhile(char.IsDigit).ToArray());
        return $"{row.GetValueOrDefault("id")}:{state}";
    }

    public bool WithinSlack(float slack = DefaultSlack) => SimOnly.Count == 0 && Matched.All(m => MathF.Abs(m.Dt) <= slack);

    public string Report(float slack = DefaultSlack)
    {
        var text = new StringBuilder();
        if (AlignedOn is null)
        {
            text.AppendLine($"no recorded enemy cast matches any of the scenario's {SimOnly.Count} casts; nothing to align on");
            return text.ToString();
        }
        text.AppendLine($"aligned on {AlignedOn.Name} ({AlignedOn.ActionId}) at {F(AlignedOn.T)}s of the recording; offset {F(Offset)}s");
        text.AppendLine($"{"sim t",8} {"dt",7} {"dist",6} {"drot",6}  cast");
        foreach (var m in Matched)
        {
            var flag = MathF.Abs(m.Dt) > slack ? "!" : " ";
            text.AppendLine($"{F(m.Sim.T),8} {F(m.Dt, "+0.00;-0.00"),7}{flag}{(m.Distance is { } d ? F(d, "0.0") : "-"),6} {(m.RotDelta is { } r ? F(r, "+0.00;-0.00") : "-"),6}  {m.Sim.Name} ({m.Sim.ActionId}) {m.Sim.ActorName}");
        }
        foreach (var c in SimOnly) text.AppendLine($"{F(c.T),8}   sim only          {c.Name} ({c.ActionId}) {c.ActorName}");
        foreach (var c in RealOnly) text.AppendLine($"{F(c.T - Offset),8}   real only         {c.Name} ({c.ActionId}) {c.ActorName}");
        foreach (var (what, count) in RealExtras) text.AppendLine($"         real sends       {what} x{count}");
        var drifted = Matched.Count(m => MathF.Abs(m.Dt) > slack);
        text.AppendLine($"{Matched.Count} matched ({drifted} past {F(slack)}s), {SimOnly.Count} sim only, {RealOnly.Count} real only, "
                        + $"{RealExtras.Count} kinds of EObj/MapEffect the sim never sends, {RealCastsAfterSimEnd} recorded casts after the scenario's end");
        return text.ToString();
    }

    private static string F(float value, string format = "0.00") => value.ToString(format, CultureInfo.InvariantCulture);
}
