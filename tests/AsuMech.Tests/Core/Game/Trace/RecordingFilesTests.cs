using AsuMech.Core.Game.Trace;

namespace AsuMech.Tests.Core.Game.Trace;

public class RecordingFilesTests
{
    [Test]
    public void ClearingDeletesOnlyRecordingsAndNeverTheOneBeingWritten()
    {
        var dir = Directory.CreateTempSubdirectory("asumech-recordings-").FullName;
        try
        {
            string File(string name) { var p = Path.Combine(dir, name); System.IO.File.WriteAllText(p, "t,kind\n"); return p; }
            File("1232_20261105_213000.csv");
            File("1325_20261106_101500.csv");
            var writing = File("1325_20261106_112000.csv");
            File("notes.csv");
            File("1232_20261105_213000.csv.gz");
            File("pull.log");

            Assert.That(RecordingFiles.List(dir).Select(f => f.Name), Is.EqualTo(new[] { "1232_20261105_213000.csv", "1325_20261106_101500.csv", "1325_20261106_112000.csv" }));
            Assert.That(RecordingFiles.DeleteAll(dir, except: writing), Is.EqualTo(2));
            Assert.That(Directory.GetFiles(dir).Select(Path.GetFileName).Order(), Is.EqualTo(new[] { "1232_20261105_213000.csv.gz", "1325_20261106_112000.csv", "notes.csv", "pull.log" }));
            Assert.That(RecordingFiles.List(Path.Combine(dir, "missing")), Is.Empty);
        }
        finally { Directory.Delete(dir, true); }
    }
}
