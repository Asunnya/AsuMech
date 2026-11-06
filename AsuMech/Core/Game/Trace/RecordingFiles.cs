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

    public static bool IsRecording(string path) => Name.IsMatch(Path.GetFileName(path));

    public static List<FileInfo> List(string dir)
        => Directory.Exists(dir) ? new DirectoryInfo(dir).EnumerateFiles("*.csv").Where(f => IsRecording(f.Name)).OrderBy(f => f.Name).ToList() : [];

    // Every recording but `except` (the one being written); returns how many went.
    public static int DeleteAll(string dir, string? except = null)
    {
        var deleted = 0;
        foreach (var file in List(dir))
        {
            if (except != null && Path.GetFullPath(file.FullName) == Path.GetFullPath(except)) continue;
            try { file.Delete(); deleted++; }
            catch (IOException) { }
            catch (System.UnauthorizedAccessException) { }
        }
        return deleted;
    }
}
