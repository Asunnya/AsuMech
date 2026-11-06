using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AsuMech.Network;

namespace AsuMech.Relay.Tests;

public class RunReportTests
{
    private const string AdminToken = "admin-token-0123456789abcdef";

    private string dir = "";

    [SetUp]
    public void SetUp() => dir = Directory.CreateTempSubdirectory("asumech-reports-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(dir, recursive: true);

    private const string Bug = """{"kind":"bug","text":"died to the second tower","scenario":{"type":"M11sArenaSplitScenario"},"run":{"seed":8,"elapsedSeconds":61.2}}""";

    [Test]
    public async Task AReportIsKeptWholeUnderItsSenderAndTheAdminFetchesIt()
    {
        await using var server = new RelayServer(new RelayOptions
        {
            BindAddress = IPAddress.Loopback, Port = 0, AdminToken = AdminToken, RecordingsDir = dir, RecordingsFrom = [Guid.NewGuid()],
        }, new QuietLog());
        server.Start();
        var port = server.LocalEndPoint!.Port;
        var secret = RelayWire.NewSecret();

        var (status, body) = await Post(port, secret, Bug);
        Assert.That(status, Is.EqualTo(201));
        var name = JsonDocument.Parse(body).RootElement.GetProperty("name").GetString()!;
        Assert.That(name, Does.Match("^[0-9]{8}-[0-9]{6}_bug_" + RelayWire.PeerId(secret).ToString("N")[..8] + "\\.json$"));

        var list = await Get(port, "/reports", AdminToken);
        Assert.That(JsonSerializer.Deserialize<List<RunReportInfo>>(list.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Select(r => r.Name), Is.EqualTo(new[] { name }));
        var stored = JsonNode.Parse((await Get(port, $"/reports/{name}", AdminToken)).Body)!;
        Assert.That(stored["report"]!.ToJsonString(), Is.EqualTo(JsonNode.Parse(Bug)!.ToJsonString()));
        Assert.That((await Get(port, $"/reports/{name}", null)).Status, Is.Not.EqualTo(200));
        Assert.That((await Get(port, "/reports/..%2Fsecret.json", AdminToken)).Body, Is.EqualTo("null"));
    }

    [TestCase("""{"kind":"spam","text":"x"}""")]
    [TestCase("""{"kind":"bug"}""")]
    [TestCase("""["bug"]""")]
    [TestCase("not json")]
    public async Task MalformedReportsAreRefused(string json)
    {
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0, RecordingsDir = dir }, new QuietLog());
        server.Start();
        Assert.That((await Post(server.LocalEndPoint!.Port, RelayWire.NewSecret(), json)).Status, Is.EqualTo(400));
        Assert.That(Directory.Exists(Path.Combine(dir, "reports")), Is.False);
    }

    [Test]
    public void EachPeerIsCappedPerHourAndNamesNeverCollide()
    {
        var now = new DateTime(2026, 11, 6, 20, 0, 0, DateTimeKind.Utc);
        var store = new RunReportStore(Path.Combine(dir, "reports"), () => now);
        var peer = Guid.NewGuid();
        var names = Enumerable.Range(0, RunReportStore.PerPeerPerHour).Select(_ => store.Add(peer, Bug)).ToList();
        Assert.That(names.All(n => n.Outcome == RunReportStore.Outcome.Stored));
        Assert.That(names.Select(n => n.Name).Distinct().Count(), Is.EqualTo(RunReportStore.PerPeerPerHour));
        Assert.That(store.Add(peer, Bug).Outcome, Is.EqualTo(RunReportStore.Outcome.TooMany));
        Assert.That(store.List(), Has.Count.EqualTo(RunReportStore.PerPeerPerHour));
    }

    private static async Task<(int Status, string Body)> Post(int port, string secret, string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var head = $"POST /reports HTTP/1.1\r\nHost: x\r\nContent-Length: {body.Length}\r\n{RelayHeaders.Prefix}{RelayHeaders.Protocol}: {RelayWire.Version}\r\n{RelayHeaders.Prefix}{RelayHeaders.PeerSecret}: {secret}\r\n\r\n";
        return await Raw(port, [.. Encoding.ASCII.GetBytes(head), .. body]);
    }

    private static Task<(int Status, string Body)> Get(int port, string path, string? adminToken)
        => Raw(port, Encoding.ASCII.GetBytes($"GET {path} HTTP/1.1\r\nHost: x\r\n{(adminToken != null ? $"{RelayHeaders.Prefix}{RelayHeaders.AdminToken}: {adminToken}\r\n" : "")}\r\n"));

    private static async Task<(int Status, string Body)> Raw(int port, byte[] request)
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
        if (!text.StartsWith("HTTP/1.1 ", StringComparison.Ordinal)) return (0, "");
        var split = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        return (int.Parse(text.AsSpan(9, 3)), split < 0 ? "" : text[(split + 4)..]);
    }
}
