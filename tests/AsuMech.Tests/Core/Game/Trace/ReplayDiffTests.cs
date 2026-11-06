using System.IO.Compression;
using System.Numerics;

namespace AsuMech.Tests;

public class ReplayDiffTests
{
    private static Dictionary<string, string> Cast(float t, uint action, string name, string actor = "40001A2B", float x = 100f, float z = 100f, float rot = 0f, float castTime = 4f)
        => new()
        {
            ["t"] = SimTimeline.F(t, "F3"), ["kind"] = "cast", ["actor"] = actor, ["actor_name"] = "Boss", ["id"] = action.ToString(), ["name"] = name,
            ["x"] = SimTimeline.F(x), ["z"] = SimTimeline.F(z), ["rot"] = SimTimeline.F(rot), ["detail"] = $"cast={SimTimeline.F(castTime)}s interruptible=0 rel_rot=0.00",
        };

    private static Dictionary<string, string> Row(float t, string kind, string id = "", string detail = "")
        => new() { ["t"] = SimTimeline.F(t, "F3"), ["kind"] = kind, ["id"] = id, ["detail"] = detail };

    private static readonly List<Dictionary<string, string>> Sim =
    [
        Cast(5f, 100, "Opener"),
        Cast(12f, 101, "Cleave", rot: 1.5f),
        Cast(20f, 102, "Towers", x: 90f),
        Row(20.5f, "eobj_spawn", "2013000"),
        Row(21f, "map_effect", "3", "state=2 timeline=0 flags=0x2"),
        Cast(30f, 103, "Enrage"),
    ];

    private static List<Dictionary<string, string>> Real(float offset) =>
    [
        Row(0f, "recording", "1232"),
        Cast(offset + 1f, 7, "Auto", actor: "10001234"),
        Cast(offset + 5f, 100, "Opener"),
        Cast(offset + 12.1f, 101, "Cleave", rot: 1.6f),
        Cast(offset + 20.6f, 102, "Towers", x: 93f),
        Row(offset + 20.5f, "eobj_spawn", "2013000"),
        Row(offset + 20.5f, "eobj_spawn", "2013001"),
        Row(offset + 21f, "map_effect", "3", "state=2 timeline=0 flags=0x2"),
        Row(offset + 21f, "map_effect", "4", "state=32 timeline=0 flags=0x20"),
        Cast(offset + 25f, 104, "Unknown"),
        Cast(offset + 60f, 105, "Later"),
    ];

    [Test]
    public void AlignsOnTheFirstSharedCastAndPairsTheRest()
    {
        var diff = ReplayDiff.Compare(Sim, Real(100f));
        Assert.That(diff.AlignedOn!.ActionId, Is.EqualTo(100u), "the player's auto-attack isn't an enemy cast");
        Assert.That(diff.Offset, Is.EqualTo(100f).Within(0.001f));
        Assert.That(diff.Matched.Select(m => m.Sim.ActionId), Is.EqualTo(new uint[] { 100, 101, 102 }));
        Assert.That(diff.Matched[1].Dt, Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(diff.Matched[1].RotDelta, Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(diff.Matched[2].Dt, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(diff.Matched[2].Distance, Is.EqualTo(3f).Within(0.001f));
        Assert.That(diff.SimOnly.Select(c => c.Name), Is.EqualTo(new[] { "Enrage" }));
        Assert.That(diff.RealOnly.Select(c => c.Name), Is.EqualTo(new[] { "Unknown" }));
        Assert.That(diff.RealCastsAfterSimEnd, Is.EqualTo(1), "the pull going on past the scenario isn't drift");
        Assert.That(diff.RealExtras, Is.EqualTo(new[] { ("EObj 2013001", 1), ("MapEffect 4:32", 1) }));
        Assert.That(diff.WithinSlack(), Is.False);
        var report = diff.Report();
        Assert.That(report, Does.Contain("aligned on Opener (100) at 105.00s"));
        Assert.That(report, Does.Contain("+0.60!"));
        Assert.That(report, Does.Contain("sim only"));
        Assert.That(report, Does.Contain("EObj 2013001 x1"));
    }

    [Test]
    public void AnExactReplayIsWithinSlack()
    {
        var real = Sim.Select(r => new Dictionary<string, string>(r) { ["t"] = SimTimeline.F(SimTimeline.Time(r) + 42f, "F3") }).ToList();
        var diff = ReplayDiff.Compare(Sim, real);
        Assert.That(diff.WithinSlack());
        Assert.That(diff.Matched, Has.Count.EqualTo(4));
        Assert.That(diff.RealOnly, Is.Empty);
        Assert.That(diff.RealExtras, Is.Empty);
    }

    [Test]
    public void T0SkipsAnEarlierPullsCasts()
    {
        var real = Real(10f).Concat(Real(200f).Skip(1)).ToList();
        Assert.That(ReplayDiff.Compare(Sim, real).Offset, Is.EqualTo(10f).Within(0.001f));
        Assert.That(ReplayDiff.Compare(Sim, real, t0: 150f).Offset, Is.EqualTo(200f).Within(0.001f));
    }

    [Test]
    public void NothingSharedMeansNothingAligned()
    {
        var diff = ReplayDiff.Compare(Sim, [Cast(3f, 999, "Other")]);
        Assert.That(diff.AlignedOn, Is.Null);
        Assert.That(diff.SimOnly, Has.Count.EqualTo(4));
        Assert.That(diff.Report(), Does.Contain("nothing to align on"));
    }

    [Test]
    public void PullsSplitAtDutyStartsAndTheZoneInGoesWithTheFirst()
    {
        var rows = new List<Dictionary<string, string>>
        {
            Row(0f, "recording", "1232"), Row(1f, "spawn"), Row(2f, "duty", "start"), Cast(5f, 100, "Opener"),
            Row(40f, "duty", "wipe"), Row(50f, "duty", "start"), Cast(55f, 100, "Opener"), Row(90f, "duty", "clear"),
        };
        var pulls = Recording.Pulls(rows);
        Assert.That(pulls, Has.Count.EqualTo(2));
        Assert.That(pulls[0].Select(r => r["kind"]), Is.EqualTo(new[] { "recording", "spawn", "duty", "cast", "duty" }));
        Assert.That(pulls[1].Select(r => r["kind"]), Is.EqualTo(new[] { "duty", "cast", "duty" }));
        Assert.That(Recording.Territory(rows), Is.EqualTo(1232u));
        Assert.That(Recording.Pulls([Cast(1f, 1, "x")]), Has.Count.EqualTo(1), "no duty rows: one pull");
    }

    [Test]
    public void LoadsPlainAndGzippedRecordings()
    {
        var dir = Directory.CreateTempSubdirectory("asumech-replay-").FullName;
        try
        {
            var csv = SimTimeline.ToCsv(Real(0f));
            var plain = Path.Combine(dir, "a.csv");
            File.WriteAllText(plain, csv);
            var gz = Path.Combine(dir, "a.csv.gz");
            using (var stream = new GZipStream(File.Create(gz), CompressionLevel.Fastest))
                stream.Write(System.Text.Encoding.UTF8.GetBytes(csv));
            Assert.That(Recording.Load(plain), Has.Count.EqualTo(Real(0f).Count));
            Assert.That(Recording.Load(gz).Select(r => r["kind"]), Is.EqualTo(Recording.Load(plain).Select(r => r["kind"])));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
