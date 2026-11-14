using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsuMech.Core.Game.Trace;
using Dalamud.Game.Text;

namespace AsuMech.Core.Native.Implementations;

// What PullRecorder does once a recording is closed: the combat parser's log, then the upload.
internal static class PullRecordingFinish
{
    // A parser flushes its log a moment after the packets; the slice waits for the pull's last lines.
    private static readonly TimeSpan SliceDelay = TimeSpan.FromSeconds(3);

    public sealed record SliceRequest(string Dir, DateTimeOffset From, DateTimeOffset To,
        Dictionary<uint, string> Labels, Dictionary<uint, string> RealNames);

    public sealed record UploadRequest(string Relay, string? Token, string PeerSecret, bool Keep);

    // Off the framework thread; only the chat lines come back to it.
    public static async Task RunAsync(string path, SliceRequest? slice, UploadRequest? send)
    {
        if (slice != null)
        {
            await Task.Delay(SliceDelay).ConfigureAwait(false);
            try
            {
                if (AttachParserLogTo(path, slice) is { } message) ChatLater(message);
            }
            catch (Exception e)
            {
                DiagnosticLog.Warn($"[PullRecorder] reading the parser log failed: {e}");
            }
        }
        if (send == null) return;
        var files = new[] { path, RecordingFiles.LogPath(path), RecordingFiles.ParserLogPath(path) }.Where(File.Exists).ToList();
        var notes = new List<string>();
        var sent = new List<string>();
        foreach (var file in files)
        {
            Multiplayer.RecordingUploader.Result result;
            try { result = await Multiplayer.RecordingUploader.UploadAsync(file, send.Relay, send.Token, send.PeerSecret).ConfigureAwait(false); }
            catch (Exception e) { result = new Multiplayer.RecordingUploader.Result(false, e.Message); }
            notes.Add(result.Ok ? result.Message : $"{System.IO.Path.GetFileName(file)} failed: {result.Message}");
            if (result.Ok) sent.Add(file);
        }
        ChatLater($"Upload: {string.Join("; ", notes)}{(!send.Keep && sent.Count > 0 ? DeleteSent(sent) : "")}");
    }

    // The pull's part of the player's combat parser log, kept beside the recorder's own.
    private static string? AttachParserLogTo(string recordingPath, SliceRequest request)
    {
        if (ParserLogFolder.Find(request.Dir, request.From) is not { } dir) return null;
        var labels = request.Labels;
        string Label(uint id)
        {
            if (!labels.TryGetValue(id, out var label)) labels[id] = label = $"player {labels.Count + 1}";
            return label;
        }
        var output = RecordingFiles.ParserLogPath(recordingPath);
        var partial = output + ".part";
        int lines;
        try
        {
            using (var writer = new StreamWriter(partial, false, new UTF8Encoding(false)) { NewLine = "\r\n" })
                lines = ParserLogSlice.Write(ParserLogSlice.Lines(dir, request.From, request.To), writer, new NetworkLogAnonymizer(request.RealNames, Label));
            if (lines > 0) File.Move(partial, output, overwrite: true);
        }
        finally
        {
            if (File.Exists(partial)) File.Delete(partial);
        }
        if (lines == 0) return null;
        return $"Took the pull's {lines} lines from your parser log into {output}";
    }

    private static void ChatLater(string text)
        => Plugin.Framework.RunOnFrameworkThread(() => Plugin.ChatGui.Print(new XivChatEntry { Type = XivChatType.SystemMessage, Message = $"[AsuMech] {text}" }));

    private static string DeleteSent(IEnumerable<string> paths)
    {
        var all = true;
        foreach (var path in paths)
        {
            try { File.Delete(path); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                DiagnosticLog.Warn($"[PullRecorder] deleting the sent {path} failed: {e.Message}");
                all = false;
            }
        }
        return all ? $"; {paths.Count()} sent file(s) removed from your disk" : "";
    }
}
