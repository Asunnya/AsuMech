using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AsuMech.Network;

namespace AsuMech.Relay;

// POST /feedback lands here as one JSON line each. The token ships in the plugin, so the per-peer
// hourly cap, not the token, is what keeps a script from filling the disk.
internal sealed class LessonFeedbackLog(string? path, Func<DateTime> now)
{
    public const int PerPeerPerHour = 120;

    public enum Outcome { Stored, Invalid, TooMany, Failed }

    private readonly object gate = new();
    private readonly Dictionary<Guid, (DateTime Start, int Count)> recent = [];

    public bool Enabled => path != null;

    public Outcome Add(Guid peer, string body)
    {
        if (path is null) return Outcome.Failed;
        LessonFeedback? feedback;
        try { feedback = JsonSerializer.Deserialize<LessonFeedback>(body, LessonFeedback.Json); }
        catch (JsonException) { return Outcome.Invalid; }
        if (feedback is not { IsValid: true }) return Outcome.Invalid;
        var at = now();
        lock (gate)
        {
            var (start, count) = recent.TryGetValue(peer, out var r) && at - r.Start < TimeSpan.FromHours(1) ? r : (at, 0);
            if (count >= PerPeerPerHour) return Outcome.TooMany;
            recent[peer] = (start, count + 1);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                var line = JsonSerializer.Serialize(new StoredLessonFeedback(at, peer.ToString("N")[..8], feedback), LessonFeedback.Json);
                File.AppendAllText(path, line + "\n");
            }
            catch (IOException) { return Outcome.Failed; }
            catch (UnauthorizedAccessException) { return Outcome.Failed; }
        }
        return Outcome.Stored;
    }

    public List<StoredLessonFeedback> ReadAll()
    {
        if (path is null || !File.Exists(path)) return [];
        lock (gate)
            return File.ReadLines(path)
                .Select(Parse)
                .OfType<StoredLessonFeedback>()
                .ToList();
    }

    private static StoredLessonFeedback? Parse(string line)
    {
        try { return JsonSerializer.Deserialize<StoredLessonFeedback>(line, LessonFeedback.Json); }
        catch (JsonException) { return null; }
    }
}
