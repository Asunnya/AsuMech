using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AsuMech.Network;

namespace AsuMech.Relay.Tests;

public class RelayRecordingsTests
{
    private const string AdminToken = "admin-token-0123456789abcdef";
    private const string AccessToken = "join-token-0123456789abcdef";

    private string dir = "";

    [SetUp]
    public void SetUp() => Directory.CreateDirectory(dir = Path.Combine(Path.GetTempPath(), "asumech-recordings-" + Guid.NewGuid().ToString("N")));

    [TearDown]
    public void TearDown() => Directory.Delete(dir, recursive: true);

    private static byte[] Gzip(string text)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Fastest, leaveOpen: true)) gzip.Write(Encoding.UTF8.GetBytes(text));
        return buffer.ToArray();
    }

    private static string Gunzip(byte[] bytes)
    {
        using var gzip = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Test]
    public async Task UploadIsStoredUnderTheSenderAndServedBackToTheAdmin()
    {
        var secret = RelayWire.NewSecret();
        var peer = RelayWire.PeerId(secret);
        await using var server = new RelayServer(new RelayOptions
        {
            BindAddress = IPAddress.Loopback, Port = 0, AdminToken = AdminToken, RecordingsDir = dir, RecordingsFrom = [peer],
        }, new QuietLog());
        server.Start();
        var port = server.LocalEndPoint!.Port;
        var csv = "t,kind\n0.000,recording\n1.000,cast\n";
        var body = Gzip(csv);

        var (status, json) = await Post(port, "1232_20261105_213000.csv", secret, body);
        Assert.That(status, Is.EqualTo(201));
        var stored = JsonDocument.Parse(json).RootElement.GetProperty("name").GetString()!;
        Assert.That(stored, Is.EqualTo($"1232_20261105_213000_{peer:N}"[..(21 + 8)] + ".csv.gz"));
        Assert.That(Gunzip(await File.ReadAllBytesAsync(Path.Combine(dir, stored))), Is.EqualTo(csv));
        Assert.That(Directory.GetFiles(dir, "*.part"), Is.Empty);
        Assert.That(server.GetStats().RecordingsStored, Is.EqualTo(1));

        var (listStatus, listJson) = await Get(port, "/recordings", AdminToken);
        Assert.That(listStatus, Is.EqualTo(200));
        var list = JsonSerializer.Deserialize<List<RecordingInfo>>(listJson, RelayAdmin.Json)!;
        Assert.That(list.Select(r => r.Name), Is.EqualTo(new[] { stored }));
        Assert.That(list[0].Bytes, Is.EqualTo(body.Length));

        var (fileStatus, fileBytes) = await GetBytes(port, $"/recordings/{stored}", AdminToken);
        Assert.That(fileStatus, Is.EqualTo(200));
        Assert.That(Gunzip(fileBytes), Is.EqualTo(csv));

        Assert.That((await Get(port, "/recordings", null)).Status, Is.EqualTo(401), "listing needs the admin token");
        Assert.That((await GetBytes(port, $"/recordings/{stored}", "wrong-token-0123456789abcdef")).Status, Is.EqualTo(401));
        Assert.That((await GetBytes(port, "/recordings/..%2Fsomething.csv.gz", AdminToken)).Status, Is.EqualTo(404), "only stored names are served");
        Assert.That((await GetBytes(port, "/recordings/missing_00000000.csv.gz", AdminToken)).Status, Is.EqualTo(404));
    }

    [Test]
    public async Task UploadRefusals()
    {
        var allowed = RelayWire.NewSecret();
        await using var server = new RelayServer(new RelayOptions
        {
            BindAddress = IPAddress.Loopback, Port = 0, RecordingsDir = dir, RecordingsFrom = [RelayWire.PeerId(allowed)], MaxRecordingBytes = 1024,
        }, new QuietLog());
        server.Start();
        var port = server.LocalEndPoint!.Port;
        var body = Gzip("t,kind\n");

        Assert.That((await Post(port, "a.csv", RelayWire.NewSecret(), body)).Status, Is.EqualTo(403), "a peer not on the list");
        Assert.That((await Post(port, "a.csv", null, body)).Status, Is.EqualTo(400), "no peer credential");
        Assert.That((await Post(port, "a.csv", allowed, new byte[2048])).Status, Is.EqualTo(413), "over the size cap");
        Assert.That((await Post(port, "a.csv", allowed, Encoding.UTF8.GetBytes("t,kind\n"))).Status, Is.EqualTo(400), "not gzip");
        Assert.That((await Post(port, "../a.csv", allowed, body)).Status, Is.EqualTo(400), "name that leaves the folder");
        Assert.That((await Post(port, "", allowed, body)).Status, Is.EqualTo(400), "empty name");
        Assert.That((await Post(port, "a.csv", allowed, body, origin: "https://evil.example")).Status, Is.EqualTo(403), "browser origin");
        Assert.That(Directory.GetFiles(dir), Is.Empty);
        Assert.That(server.GetStats().RecordingsRejected, Is.EqualTo(5), "list, size, gzip and the two name refusals are the upload's own");

        Assert.That((await Post(port, "a.csv", allowed, body)).Status, Is.EqualTo(201), "the allowed peer still gets through");
        Assert.That((await Get(port, "/recordings", null)).Status, Is.EqualTo(404), "no admin token: listing hidden");
    }

    [Test]
    public async Task UploadNeedsTheJoinTokenWhenTheRelayHasOne()
    {
        var secret = RelayWire.NewSecret();
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0, RecordingsDir = dir, AccessToken = AccessToken }, new QuietLog());
        server.Start();
        var port = server.LocalEndPoint!.Port;
        var body = Gzip("t,kind\n");
        Assert.That((await Post(port, "a.csv", secret, body)).Status, Is.EqualTo(401));
        Assert.That((await Post(port, "a.csv", secret, body, token: "wrong-token-0123456789abcdef")).Status, Is.EqualTo(401));
        Assert.That((await Post(port, "a.csv", secret, body, token: AccessToken)).Status, Is.EqualTo(201), "anyone with the token, no list set");
    }

    [Test]
    public async Task EndpointHiddenWithoutARecordingsDir()
    {
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0, AdminToken = AdminToken }, new QuietLog());
        server.Start();
        var port = server.LocalEndPoint!.Port;
        Assert.That((await Post(port, "a.csv", RelayWire.NewSecret(), Gzip("x"))).Status, Is.EqualTo(404));
        Assert.That((await Get(port, "/recordings", AdminToken)).Status, Is.EqualTo(200), "the listing just comes back empty");
        Assert.That((await Get(port, "/recordings", AdminToken)).Body, Is.EqualTo("[]"));
        Assert.That((await GetBytes(port, "/recordings/a_00000000.csv.gz", AdminToken)).Status, Is.EqualTo(404));
    }

    [Test]
    public void StoredNamesAreSafe()
    {
        var peer = Guid.Parse("0123456789abcdef0123456789abcdef".Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-"));
        Assert.That(RelayServer.RecordingFileName("1232_20261105_213000.csv", peer), Is.EqualTo("1232_20261105_213000_01234567.csv.gz"));
        Assert.That(RelayServer.RecordingFileName("pull", peer), Is.EqualTo("pull_01234567.csv.gz"));
        Assert.That(RelayServer.RecordingFileName("a/b.csv", peer), Is.Null);
        Assert.That(RelayServer.RecordingFileName("a..b.csv", peer), Is.Null);
        Assert.That(RelayServer.RecordingFileName(".hidden.csv", peer), Is.Null);
        Assert.That(RelayServer.RecordingFileName(new string('a', 81), peer), Is.Null);
        Assert.That(RelayServer.RecordingFileName(null, peer), Is.Null);
        Assert.That(RelayServer.IsStoredRecordingName("pull_01234567.csv.gz"));
        Assert.That(RelayServer.IsStoredRecordingName("pull.csv"), Is.False);
        Assert.That(RelayServer.IsStoredRecordingName("../x.csv.gz"), Is.False);
    }

    private static async Task<(int Status, string Body)> Post(int port, string name, string? secret, byte[] body, string? token = null, string? origin = null)
    {
        var head = new StringBuilder($"POST /recordings HTTP/1.1\r\nHost: x\r\nContent-Length: {body.Length}\r\n{RelayHeaders.Prefix}{RelayHeaders.RecordingName}: {name}\r\n{RelayHeaders.Prefix}{RelayHeaders.Protocol}: {RelayWire.Version}\r\n");
        if (secret != null) head.Append($"{RelayHeaders.Prefix}{RelayHeaders.PeerSecret}: {secret}\r\n");
        if (token != null) head.Append($"{RelayHeaders.Prefix}{RelayHeaders.RelayToken}: {token}\r\n");
        if (origin != null) head.Append($"Origin: {origin}\r\n");
        head.Append("\r\n");
        var (status, bytes) = await Raw(port, Encoding.ASCII.GetBytes(head.ToString()).Concat(body).ToArray());
        return (status, Encoding.UTF8.GetString(bytes));
    }

    private static async Task<(int Status, string Body)> Get(int port, string path, string? adminToken)
    {
        var (status, bytes) = await GetBytes(port, path, adminToken);
        return (status, Encoding.UTF8.GetString(bytes));
    }

    private static Task<(int Status, byte[] Body)> GetBytes(int port, string path, string? adminToken)
        => Raw(port, Encoding.ASCII.GetBytes($"GET {path} HTTP/1.1\r\nHost: x\r\n{(adminToken != null ? $"{RelayHeaders.Prefix}{RelayHeaders.AdminToken}: {adminToken}\r\n" : "")}\r\n"));

    // Status 0 when the relay reset the connection without a readable response.
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
        var bytes = response.ToArray();
        var text = Encoding.Latin1.GetString(bytes);
        var headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd < 0 || !text.StartsWith("HTTP/1.1 ", StringComparison.Ordinal)) return (0, []);
        return (int.Parse(text.AsSpan(9, 3)), bytes[(headerEnd + 4)..]);
    }
}
