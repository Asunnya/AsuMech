using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AsuMech.Core.Game.Trace;

// The part of the network log the player's combat parser wrote that a pull recording covers.
// Logs reach a gigabyte a day, so each file is binary-searched for the window's start.
public static class ParserLogSlice
{
    private const long ScanThreshold = 1 << 16;

    public static IEnumerable<string> Lines(string dir, DateTimeOffset from, DateTimeOffset to)
        => Files(dir, from).SelectMany(file => FileLines(file, from, to));

    // Oldest first: a pull past midnight spans two files.
    public static List<string> Files(string dir, DateTimeOffset from)
    {
        if (!Directory.Exists(dir)) return [];
        return new DirectoryInfo(dir).EnumerateFiles("Network_*.log")
            .Where(f => f.LastWriteTimeUtc >= from.UtcDateTime)
            .OrderBy(f => f.LastWriteTimeUtc)
            .Select(f => f.FullName)
            .ToList();
    }

    public static IEnumerable<string> FileLines(string path, DateTimeOffset from, DateTimeOffset to)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var start = StartOffset(stream, from);
        stream.Position = start;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: start == 0);
        if (start > 0) reader.ReadLine();
        while (reader.ReadLine() is { } line)
        {
            if (Timestamp(line) is not { } at) continue;
            if (at < from) continue;
            if (at > to) yield break;
            yield return line;
        }
    }

    // An offset at or before the first line stamped `from` or later.
    private static long StartOffset(Stream stream, DateTimeOffset from)
    {
        long lo = 0, hi = stream.Length;
        while (hi - lo > ScanThreshold)
        {
            var mid = lo + (hi - lo) / 2;
            if (FirstTimestampAfter(stream, mid) is { } at && at < from) lo = mid;
            else hi = mid;
        }
        return lo;
    }

    private static DateTimeOffset? FirstTimestampAfter(Stream stream, long offset)
    {
        stream.Position = offset;
        var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, leaveOpen: true);
        reader.ReadLine();
        for (var i = 0; i < 8 && reader.ReadLine() is { } line; i++)
            if (Timestamp(line) is { } at) return at;
        return null;
    }

    public static DateTimeOffset? Timestamp(string line)
    {
        var a = line.IndexOf('|');
        if (a < 0) return null;
        var b = line.IndexOf('|', a + 1);
        if (b < 0) return null;
        return DateTimeOffset.TryParse(line.AsSpan(a + 1, b - a - 1), CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ? at : null;
    }

    // Each kept line is re-hashed in the slice's own sequence, so the slice checks out as a log.
    public static int Write(IEnumerable<string> lines, TextWriter output, NetworkLogAnonymizer anonymizer)
    {
        var seal = new NetworkLogSeal();
        var written = 0;
        foreach (var line in lines)
        {
            if (anonymizer.Rewrite(line) is not { } kept) continue;
            output.WriteLine(seal.Reseal(kept));
            written++;
        }
        return written;
    }
}

// Players renamed to the recording's labels and conversation dropped, since the log is sent on.
public sealed class NetworkLogAnonymizer
{
    private readonly Func<uint, string> label;
    private readonly Dictionary<string, string> aliases = new(StringComparer.Ordinal);
    private List<KeyValuePair<string, string>>? longestFirst;

    public NetworkLogAnonymizer(IReadOnlyDictionary<uint, string> knownNames, Func<uint, string> label)
    {
        this.label = label;
        foreach (var (id, name) in knownNames) Learn(id, name);
    }

    public string? Rewrite(string line)
    {
        var fields = line.Split('|');
        if (fields.Length < 4) return line;
        switch (fields[0])
        {
            case "00" when IsConversation(fields[2]):
                return null;
            case "12" when fields.Length > ContentIdField:
                fields[ContentIdField] = "00000000";
                line = string.Join("|", fields);
                break;
            case "02" or "03" or "04" when Hex(fields[2]) is { } id:
                Learn(id, fields[3]);
                break;
            case "261" when fields[2] == "Add" && Hex(fields[3]) is { } id:
                for (var i = 4; i + 1 < fields.Length; i += 2)
                    if (fields[i] == "Name") Learn(id, fields[i + 1]);
                break;
        }
        longestFirst ??= aliases.OrderByDescending(a => a.Key.Length).ToList();
        foreach (var (name, alias) in longestFirst)
            if (line.Contains(name, StringComparison.Ordinal)) line = line.Replace(name, alias, StringComparison.Ordinal);
        return line;
    }

    // The character's content id: identifying, so it never leaves the machine.
    private const int ContentIdField = 18;

    public void Know(uint id, string name) => Learn(id, name);

    private void Learn(uint id, string name)
    {
        if (id >> 28 != 1 || name.Length == 0 || aliases.ContainsKey(name)) return;
        aliases[name] = label(id);
        longestFirst = null;
    }

    // Player chat, emotes, echo, and the announcements that name people outside the party (free
    // company, novice network, PvP team, logins); battle, system and NPC lines stay.
    private static bool IsConversation(string code)
    {
        if (!int.TryParse(code, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return false;
        var channel = value & 0x7F;
        return channel is >= 0x0A and <= 0x25 or 0x38 or >= 0x45 and <= 0x4E or >= 0x65 and <= 0x6B;
    }

    private static uint? Hex(string text)
        => uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) ? value : null;
}
