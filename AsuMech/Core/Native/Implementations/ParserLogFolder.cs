using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AsuMech.Core.Game.Trace;

namespace AsuMech.Core.Native.Implementations;

// Where the player's combat parser writes its network logs.
internal static class ParserLogFolder
{
    // The first candidate holding a log written since `since`, or null.
    public static string? Find(string configured, DateTimeOffset since)
        => Candidates(configured).FirstOrDefault(dir => ParserLogSlice.Files(dir, since).Count > 0);

    private static IEnumerable<string> Candidates(string configured)
    {
        if (configured.Trim() is { Length: > 0 } dir) yield return dir;
        if (ConfiguredFolder() is { } fromConfig) yield return fromConfig;
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IINACT");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Advanced Combat Tracker", "FFXIVLogs");
    }

    private static string? ConfiguredFolder()
    {
        try
        {
            var config = Path.Combine(Plugin.PluginInterface.ConfigDirectory.Parent!.FullName, "IINACT.json");
            if (!File.Exists(config)) return null;
            using var json = JsonDocument.Parse(File.ReadAllText(config));
            return json.RootElement.TryGetProperty("LogFilePath", out var path) && path.GetString() is { Length: > 0 } dir ? dir : null;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            DiagnosticLog.Warn($"[ParserLogFolder] reading the parser's config failed: {e.Message}");
            return null;
        }
    }
}
