using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AsuMech.Core.Game.Trace;
using Dalamud.Game.Text;

namespace AsuMech.Core.Native.Implementations;

// What PullRecorder does once a recording is closed: the ACT log slice, then the upload.
internal static class PullRecordingFinish
{
    // ACT flushes its log a moment after the packets; the slice waits for the pull's last lines.
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
                if (UseActLog(path, slice) is { } message) ChatLater(message);
            }
            catch (Exception e)
            {
                DiagnosticLog.Warn($"[PullRecorder] reading the ACT log failed: {e}");
            }
        }
        if (send == null) return;
        var files = new List<string> { path };
        if (File.Exists(RecordingFiles.ActLogPath(path))) files.Add(RecordingFiles.ActLogPath(path));
        var notes = new List<string>();
        var allSent = true;
        foreach (var file in files)
        {
            Multiplayer.RecordingUploader.Result result;
            try { result = await Multiplayer.RecordingUploader.UploadAsync(file, send.Relay, send.Token, send.PeerSecret).ConfigureAwait(false); }
            catch (Exception e) { result = new Multiplayer.RecordingUploader.Result(false, e.Message); }
            notes.Add(result.Ok ? result.Message : $"{System.IO.Path.GetFileName(file)} failed: {result.Message}");
            allSent &= result.Ok;
        }
        ChatLater($"Upload: {string.Join("; ", notes)}{(allSent && !send.Keep ? DeleteSent(files) : "")}");
    }

    // The player's own ACT/IINACT log, when there is one for the pull, replaces the .log the
    // recorder wrote: it holds what the client never shows (full status lists, effect results).
    private static string? UseActLog(string recordingPath, SliceRequest request)
    {
        if (ActLogFolder.Find(request.Dir, request.From) is not { } dir) return null;
        var labels = request.Labels;
        string Label(uint id)
        {
            if (!labels.TryGetValue(id, out var label)) labels[id] = label = $"player {labels.Count + 1}";
            return label;
        }
        var output = RecordingFiles.ActLogPath(recordingPath);
        var partial = output + ".part";
        int lines;
        using (var writer = new StreamWriter(partial, false, new UTF8Encoding(false)) { NewLine = "\r\n" })
            lines = ActLogSlice.Write(ActLogSlice.Lines(dir, request.From, request.To), writer, new ActLogAnonymizer(request.RealNames, Label));
        if (lines == 0)
        {
            File.Delete(partial);
            return null;
        }
        File.Move(partial, output, overwrite: true);
        return $"Took the pull's {lines} lines from your ACT log into {output}";
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
        return all ? "; removed from your disk" : "";
    }
}
