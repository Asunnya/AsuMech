using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AsuMech.Core.Game.Trace;

// The pull recordings PullRecorder leaves in its folder, told apart from anything else that may
// sit there (the folder can be the repo's datalogs/replays) by the name it gives them.
public static class RecordingFiles
{
    private static readonly Regex Name = new(@"^\d+_\d{8}_\d{6}\.csv$");
    private static readonly Regex ActLogName = new(@"^\d+_\d{8}_\d{6}\.log$");

    public static bool IsRecording(string path) => Name.IsMatch(Path.GetFileName(path));

    // The ACT log slice saved beside a recording.
    public static string ActLogPath(string recordingPath) => Path.ChangeExtension(recordingPath, ".log");

    public static List<FileInfo> List(string dir)
        => Directory.Exists(dir) ? new DirectoryInfo(dir).EnumerateFiles("*.csv").Where(f => IsRecording(f.Name)).OrderBy(f => f.Name).ToList() : [];

    // Every recording but `except` (the one being written), with its ACT log slice; returns how
    // many recordings went.
    public static int DeleteAll(string dir, string? except = null)
    {
        var deleted = 0;
        foreach (var file in List(dir))
        {
            if (except != null && Path.GetFullPath(file.FullName) == Path.GetFullPath(except)) continue;
            if (TryDelete(file.FullName)) deleted++;
        }
        if (Directory.Exists(dir))
            foreach (var log in new DirectoryInfo(dir).EnumerateFiles("*.log").Where(f => ActLogName.IsMatch(f.Name)))
                if (except == null || Path.GetFullPath(log.FullName) != Path.GetFullPath(ActLogPath(except)))
                    TryDelete(log.FullName);
        return deleted;
    }

    private static bool TryDelete(string path)
    {
        try { File.Delete(path); return true; }
        catch (IOException) { return false; }
        catch (System.UnauthorizedAccessException) { return false; }
    }
}
