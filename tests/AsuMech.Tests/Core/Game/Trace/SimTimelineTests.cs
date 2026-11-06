using System.Numerics;

namespace AsuMech.Tests;

public class SimTimelineTests
{
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Core", "Game", "Trace", "Fixtures", name));

    private static SimTimeline Synthetic(TextWriter? sink = null)
    {
        var t = 0f;
        var timeline = new SimTimeline(sink) { Clock = () => t, Source = () => "Scenario.cs:42" };
        t = 1.5f;
        timeline.Add("cast", 0x40001A2B, "Wicked Thunder", "37917", "Wrath of Zeus", position: new Vector3(100f, 0f, 100f), rotation: MathF.PI,
            detail: "cast=4.70s interruptible=0 rel_rot=0.00", extra: new Dictionary<string, string> { ["omen_delay"] = "0.0" });
        t = 6.2f;
        timeline.Add("ability", 0x40001A2B, "Wicked Thunder", "37917", "Wrath of Zeus", 0x10001234, "Creatina Whey",
            detail: "hits=2 [Creatina Whey, Alpha \"Bot\"]", extra: new Dictionary<string, string> { ["damage_type"] = "0x3" });
        timeline.Add("ac", 0x40001A2B, "Wicked Thunder", "0x23", p: SimTimeline.Params(0, 84, 0x10001234, 0, 0));
        return timeline;
    }

    [Test]
    public void WritesEveryColumnInOrderAndQuotesCellsWithCommasAndQuotes()
    {
        var csv = Synthetic().ToCsv();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines[0], Is.EqualTo(string.Join(",", SimTimeline.Columns)));
        Assert.That(lines, Has.Length.EqualTo(4));
        Assert.That(lines[1], Does.StartWith("1.500,cast,40001A2B,Wicked Thunder,37917,Wrath of Zeus,,,100.00,100.00,3.14,cast=4.70s interruptible=0 rel_rot=0.00,0.0,"));
        Assert.That(lines[1], Does.EndWith(",0.00,,Scenario.cs:42"));
        Assert.That(lines[2], Does.Contain("\"hits=2 [Creatina Whey, Alpha \"\"Bot\"\"]\""));
        Assert.That(lines[3], Does.Contain(",\"0x0,0x54,0x10001234\","));
    }

    [Test]
    public void ParamsDropTrailingZerosOnly()
    {
        Assert.That(SimTimeline.Params(0, 84, 0, 0), Is.EqualTo("0x0,0x54"));
        Assert.That(SimTimeline.Params(0, 0), Is.EqualTo(""));
        Assert.That(SimTimeline.Params(7), Is.EqualTo("0x7"));
    }

    [Test]
    public void ReadRoundTripsWhatWriteProduced()
    {
        var timeline = Synthetic();
        var rows = SimTimeline.Read(timeline.ToCsv());
        Assert.That(rows, Has.Count.EqualTo(timeline.Rows.Count));
        for (var i = 0; i < rows.Count; i++)
            foreach (var column in SimTimeline.Columns)
                Assert.That(rows[i][column], Is.EqualTo(timeline.Rows[i].GetValueOrDefault(column, "")), $"row {i} column {column}");
        Assert.That(SimTimeline.Time(rows[1]), Is.EqualTo(6.2f).Within(0.001f));
        Assert.That(SimTimeline.Float(rows[0], "x"), Is.EqualTo(100f));
        Assert.That(SimTimeline.Float(rows[1], "x"), Is.Null);
    }

    [Test]
    public void SinkReceivesHeaderOnceAndEachRowAsItIsAdded()
    {
        var sink = new StringWriter { NewLine = "\n" };
        var t = 0f;
        var timeline = new SimTimeline(sink) { Clock = () => t };
        Assert.That(sink.ToString(), Is.Empty);
        timeline.Add("duty", id: "start");
        Assert.That(sink.ToString(), Is.EqualTo(string.Join(",", SimTimeline.Columns) + "\n0.000,duty,,,start,,,,,,,,,,,,,,,,\n"));
        t = 2f;
        timeline.Add("duty", id: "wipe");
        Assert.That(sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries), Has.Length.EqualTo(3));
        Assert.That(timeline.Rows, Is.Empty);
        Assert.That(timeline.Count, Is.EqualTo(2));
    }

    [Test]
    public void ReadsAShortRealRecording()
    {
        var rows = SimTimeline.Read(Fixture("short-pull.csv"));
        Assert.That(rows.Select(r => r["kind"]).Distinct(), Is.SupersetOf(new[]
            { "recording", "status_gain", "pos", "duty", "spawn", "cast", "ac", "ability", "impact", "status_lose", "map_effect", "raw", "despawn", "raw_count", "recording_end" }));
        Assert.That(rows.Select(SimTimeline.Time), Is.Ordered);

        var cast = rows.Single(r => r["kind"] == "cast");
        Assert.That(cast["actor"], Is.EqualTo("40001A2B"));
        Assert.That(cast["id"], Is.EqualTo("37917"));
        Assert.That(cast["name"], Is.EqualTo("Wrath of Zeus"));
        Assert.That(SimTimeline.Float(cast, "rot"), Is.EqualTo(3.14f));
        Assert.That(cast["detail"], Does.Contain("cast=4.70s"));

        var ability = rows.Single(r => r["kind"] == "ability");
        Assert.That(ability["detail"], Does.StartWith("hits=8 ["));
        Assert.That(ability["damage_type"], Is.EqualTo("0x3"));
        Assert.That(SimTimeline.Time(ability) - SimTimeline.Time(cast), Is.EqualTo(4.708f).Within(0.001f));

        var poses = rows.Where(r => r["kind"] == "pos").ToList();
        Assert.That(poses, Has.Count.EqualTo(2));
        Assert.That(poses.Select(p => p["actor_name"]), Is.EquivalentTo(new[] { "Creatina Whey", "Wicked Thunder" }));
        Assert.That(SimTimeline.Float(poses[0], "z"), Is.EqualTo(118.5f));

        Assert.That(rows.Single(r => r["kind"] == "ac")["p"], Is.EqualTo("0x1"));
        Assert.That(rows.Single(r => r["kind"] == "raw")["id"], Is.EqualTo("0x02F4"));
        Assert.That(rows.Single(r => r["kind"] == "recording_end")["detail"], Is.EqualTo("left the duty"));
    }

    [Test]
    public void ReadToleratesAnotherColumnOrderAndMissingTrailingCells()
    {
        var rows = SimTimeline.Read("kind,t,detail\ncast,1.000\nability,2.000,\"a,b\"\n");
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0]["detail"], Is.EqualTo(""));
        Assert.That(SimTimeline.Time(rows[1]), Is.EqualTo(2f));
        Assert.That(rows[1]["detail"], Is.EqualTo("a,b"));
        Assert.That(SimTimeline.Read(""), Is.Empty);
    }
}
