using AsuMech.Core.Game.Trace;

namespace AsuMech.Tests.Core.Game.Trace;

public class ActLogSliceTests
{
    private static readonly TimeSpan Brt = TimeSpan.FromHours(-3);

    private static string Stamp(DateTimeOffset at) => at.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");

    private static string Line(string type, DateTimeOffset at, string rest) => $"{type}|{Stamp(at)}|{rest}|0123456789abcdef";

    private static string TempDir() => Directory.CreateTempSubdirectory("asumech-actlog-").FullName;

    // One line every 50 ms for `seconds`: a few hundred KB, past the binary search's scan threshold.
    private static List<string> WriteLog(string path, DateTimeOffset start, int seconds)
    {
        var lines = new List<string>();
        for (var ms = 0; ms < seconds * 1000; ms += 50)
            lines.Add(Line("270", start.AddMilliseconds(ms), $"40000001|0.0000|0000|0000|100.0000|100.0000|0.0000|{ms:D8}"));
        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n");
        return lines;
    }

    [Test]
    public void TakesExactlyTheWindowOutOfALargeLog()
    {
        var dir = TempDir();
        try
        {
            var start = new DateTimeOffset(2026, 11, 13, 20, 0, 0, Brt);
            var lines = WriteLog(Path.Combine(dir, "Network_30305_20261113.log"), start, 600);
            var from = start.AddSeconds(200);
            var to = start.AddSeconds(260);

            var slice = ActLogSlice.Lines(dir, from, to).ToList();

            var expected = lines.Where(l => ActLogSlice.Timestamp(l) is { } at && at >= from && at <= to).ToList();
            Assert.That(new FileInfo(Path.Combine(dir, "Network_30305_20261113.log")).Length, Is.GreaterThan(1 << 17));
            Assert.That(slice, Is.EqualTo(expected));
            Assert.That(ActLogSlice.Timestamp(slice[0]), Is.EqualTo(from));
            Assert.That(ActLogSlice.Timestamp(slice[^1]), Is.EqualTo(to));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Test]
    public void APullPastMidnightReadsBothDays()
    {
        var dir = TempDir();
        try
        {
            var first = Path.Combine(dir, "Network_30305_20261113.log");
            var second = Path.Combine(dir, "Network_30305_20261114.log");
            var evening = new DateTimeOffset(2026, 11, 13, 23, 50, 0, Brt);
            var midnight = new DateTimeOffset(2026, 11, 14, 0, 0, 0, Brt);
            WriteLog(first, evening, 600);
            WriteLog(second, midnight, 600);
            File.SetLastWriteTimeUtc(first, midnight.UtcDateTime);
            File.SetLastWriteTimeUtc(second, midnight.AddMinutes(10).UtcDateTime);

            var slice = ActLogSlice.Lines(dir, midnight.AddMinutes(-1), midnight.AddMinutes(1)).ToList();

            Assert.That(slice, Has.Count.EqualTo(2 * 60 * 20 + 1));
            Assert.That(slice.Select(l => ActLogSlice.Timestamp(l)), Is.Ordered);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Test]
    public void ALogNotWrittenDuringThePullIsNotRead()
    {
        var dir = TempDir();
        try
        {
            var old = Path.Combine(dir, "Network_30304_20261108.log");
            WriteLog(old, new DateTimeOffset(2026, 11, 8, 20, 0, 0, Brt), 10);
            File.SetLastWriteTimeUtc(old, new DateTime(2026, 11, 8, 23, 0, 10, DateTimeKind.Utc));

            Assert.That(ActLogSlice.Files(dir, new DateTimeOffset(2026, 11, 13, 20, 0, 0, Brt)), Is.Empty);
            Assert.That(ActLogSlice.Files(Path.Combine(dir, "missing"), DateTimeOffset.MinValue), Is.Empty);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Test]
    public void PlayersTakeTheRecordingsLabelsAndConversationIsDropped()
    {
        var at = new DateTimeOffset(2026, 11, 13, 20, 0, 0, Brt);
        var labels = new Dictionary<uint, string> { [0x100B0290] = "player 1" };
        string Label(uint id)
        {
            if (!labels.TryGetValue(id, out var label)) labels[id] = label = $"player {labels.Count + 1}";
            return label;
        }
        var anonymizer = new ActLogAnonymizer(new Dictionary<uint, string> { [0x100B0290] = "Creatina Whey" }, Label);

        var input = new[]
        {
            Line("03", at, "100BD4FB|Asunnya Frozenglade|28|64|0000|4F|Cactuar|0|0|23417|23417|10000|10000|||100.00|100.00|0.00|0.00"),
            Line("03", at, "40003FA1|Satin Plume|00|64|0000|00||6264|6290|1000|1000|10000|10000|||100.00|100.00|0.00|0.00"),
            Line("21", at, "40003FA1|Satin Plume|2B5D|Feather Rain|100B0290|Creatina Whey|750003|1F40000"),
            Line("00", at, "0E|SGE Asunnya Frozenglade|pull?"),
            Line("00", at, "000D|Someone Else|hi"),
            Line("00", at, "082B|Creatina Whey|Creatina Whey uses Gluttony."),
            Line("00", at, "0044|Garuda|My children!"),
            Line("261", at, "Add|1005EAF8|BNpcID|0|Name|Arci Do'tigris|Job|19"),
            Line("25", at, "1005EAF8|Arci Do'tigris|40003FA1|Satin Plume"),
        };

        var output = input.Select(anonymizer.Rewrite).ToList();

        Assert.That(output[0], Does.Contain("|100BD4FB|player 2|"));
        Assert.That(output[1], Does.Contain("|Satin Plume|"));
        Assert.That(output[2], Does.Contain("|100B0290|player 1|"));
        Assert.That(output[3], Is.Null, "party chat");
        Assert.That(output[4], Is.Null, "a tell");
        Assert.That(output[5], Does.Contain("|player 1|player 1 uses Gluttony."));
        Assert.That(output[6], Does.Contain("|Garuda|My children!"));
        Assert.That(output[7], Does.Contain("|Name|player 3|"));
        Assert.That(output[8], Does.Contain("|1005EAF8|player 3|"));
        Assert.That(string.Join("\n", output.OfType<string>()), Does.Not.Contain("Creatina").And.Not.Contain("Asunnya").And.Not.Contain("Arci"));
    }

    [Test]
    public void WriteCountsTheLinesItKept()
    {
        var at = new DateTimeOffset(2026, 11, 13, 20, 0, 0, Brt);
        var anonymizer = new ActLogAnonymizer(new Dictionary<uint, string>(), id => $"player {id & 0xF}");
        var output = new StringWriter();

        var written = ActLogSlice.Write([Line("00", at, "000E|A B|x"), Line("260", at, "1|0|1|0")], output, anonymizer);

        Assert.That(written, Is.EqualTo(1));
        Assert.That(output.ToString(), Does.StartWith("260|"));
    }
}
