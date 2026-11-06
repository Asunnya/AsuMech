using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AsuMech.Network;

namespace AsuMech.Relay.Tests;

public class LessonFeedbackTests
{
    private const string AdminToken = "admin-token-0123456789abcdef";

    private string dir = "";

    [SetUp]
    public void SetUp() => dir = Directory.CreateTempSubdirectory("asumech-feedback-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(dir, recursive: true);

    private static readonly LessonFeedback Down = new("DsrP7DragonKingScenario|Static south", "DsrP7DragonKingAi@80.15-241.2", LessonFeedback.Text, -1, "the tower is the other one", "0.6.0.0");

    [Test]
    public async Task AnyPeerTheTokenLetsInMayVoteAndTheAdminReadsIt()
    {
        await using var server = new RelayServer(new RelayOptions
        {
            BindAddress = IPAddress.Loopback, Port = 0, AdminToken = AdminToken, RecordingsDir = dir, RecordingsFrom = [Guid.NewGuid()],
        }, new QuietLog());
        server.Start();
        var port = server.LocalEndPoint!.Port;
        var secret = RelayWire.NewSecret();

        Assert.That(await Post(port, secret, JsonSerializer.Serialize(Down, LessonFeedback.Json)), Is.EqualTo(201));
        Assert.That(await Post(port, secret, """{"lesson":"x","step":"y","target":"text","vote":7,"comment":"","version":"1"}"""), Is.EqualTo(400));
        Assert.That(await Post(port, secret, "not json"), Is.EqualTo(400));
        Assert.That(await Post(port, null, JsonSerializer.Serialize(Down, LessonFeedback.Json)), Is.EqualTo(400), "no peer credential");

        var (status, json) = await Get(port, "/feedback", AdminToken);
        Assert.That(status, Is.EqualTo(200));
        var stored = JsonSerializer.Deserialize<List<StoredLessonFeedback>>(json, LessonFeedback.Json)!;
        Assert.That(stored.Select(s => s.Feedback), Is.EqualTo(new[] { Down }));
        Assert.That(stored[0].Peer, Is.EqualTo(RelayWire.PeerId(secret).ToString("N")[..8]));
        Assert.That((await Get(port, "/feedback", null)).Status, Is.Not.EqualTo(200), "reading needs the admin token");
    }

    [Test]
    public async Task WithoutARecordingsFolderTheRelayTakesNoFeedback()
    {
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0 }, new QuietLog());
        server.Start();
        Assert.That(await Post(server.LocalEndPoint!.Port, RelayWire.NewSecret(), JsonSerializer.Serialize(Down, LessonFeedback.Json)), Is.EqualTo(404));
    }

    [Test]
    public void EachPeerIsCappedPerHour()
    {
        var now = new DateTime(2026, 11, 6, 20, 0, 0, DateTimeKind.Utc);
        var log = new LessonFeedbackLog(Path.Combine(dir, "feedback.jsonl"), () => now);
        var body = JsonSerializer.Serialize(Down, LessonFeedback.Json);
        var spammer = Guid.NewGuid();
        for (var i = 0; i < LessonFeedbackLog.PerPeerPerHour; i++) Assert.That(log.Add(spammer, body), Is.EqualTo(LessonFeedbackLog.Outcome.Stored));
        Assert.That(log.Add(spammer, body), Is.EqualTo(LessonFeedbackLog.Outcome.TooMany));
        Assert.That(log.Add(Guid.NewGuid(), body), Is.EqualTo(LessonFeedbackLog.Outcome.Stored), "another player isn't held back");
        now = now.AddHours(1);
        Assert.That(log.Add(spammer, body), Is.EqualTo(LessonFeedbackLog.Outcome.Stored), "the cap resets after an hour");
        Assert.That(log.ReadAll(), Has.Count.EqualTo(LessonFeedbackLog.PerPeerPerHour + 2));
    }

    [TestCase("", "step", LessonFeedback.Text, 1, "")]
    [TestCase("lesson", "step", "picture", 1, "")]
    [TestCase("lesson", "step", LessonFeedback.Toolbox, 0, "")]
    [TestCase("lesson", "step\nline", LessonFeedback.Text, 1, "")]
    [TestCase("lesson", "step", LessonFeedback.Text, -1, "two\nlines")]
    public void MalformedVotesAreRefused(string lesson, string step, string target, int vote, string comment)
        => Assert.That(new LessonFeedback(lesson, step, target, vote, comment, "0.6.0.0").IsValid, Is.False);

    [Test]
    public void SummaryCountsEachPlayersLastVoteOnceAndListsComments()
    {
        var t0 = new DateTime(2026, 11, 6, 20, 0, 0, DateTimeKind.Utc);
        var up = Down with { Vote = 1, Comment = "" };
        var entries = new[]
        {
            new StoredLessonFeedback(t0, "aaaaaaaa", up),
            new StoredLessonFeedback(t0.AddMinutes(1), "aaaaaaaa", Down),
            new StoredLessonFeedback(t0, "bbbbbbbb", up),
            new StoredLessonFeedback(t0, "cccccccc", Down with { Target = LessonFeedback.Toolbox, Step = "#3 Towers", Comment = "" }),
        };
        Assert.That(LessonFeedbackSummary.Lines(entries), Is.EqualTo(new[]
        {
            "DsrP7DragonKingScenario|Static south",
            "  toolbox #3 Towers  +0 -1",
            "  text    DsrP7DragonKingAi@80.15-241.2  +1 -1",
            "      - the tower is the other one",
        }));
    }

    private static async Task<int> Post(int port, string? secret, string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var head = new StringBuilder($"POST /feedback HTTP/1.1\r\nHost: x\r\nContent-Length: {body.Length}\r\n{RelayHeaders.LegacyPrefix}{RelayHeaders.Protocol}: {RelayWire.Version}\r\n");
        if (secret != null) head.Append($"{RelayHeaders.Prefix}{RelayHeaders.PeerSecret}: {secret}\r\n");
        head.Append("\r\n");
        return (await Raw(port, [.. Encoding.ASCII.GetBytes(head.ToString()), .. body])).Status;
    }

    private static async Task<(int Status, string Body)> Get(int port, string path, string? adminToken)
    {
        var (status, bytes) = await Raw(port, Encoding.ASCII.GetBytes($"GET {path} HTTP/1.1\r\nHost: x\r\n{(adminToken != null ? $"{RelayHeaders.Prefix}{RelayHeaders.AdminToken}: {adminToken}\r\n" : "")}\r\n"));
        return (status, Encoding.UTF8.GetString(bytes));
    }

    private static async Task<(int Status, byte[] Body)> Raw(int port, byte[] request)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();
        var response = new MemoryStream();
        try
        {
            await stream.WriteAsync(request);
            await stream.CopyToAsync(response).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (IOException) { }
        var text = Encoding.UTF8.GetString(response.ToArray());
        if (!text.StartsWith("HTTP/1.1 ", StringComparison.Ordinal)) return (0, []);
        var status = int.Parse(text.AsSpan(9, 3));
        var split = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        return (status, split < 0 ? [] : Encoding.UTF8.GetBytes(text[(split + 4)..]));
    }
}
