using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using AnoMech.Multiplayer;
using AnoMech.Network;
using AnoMech.Relay;
using static AnoMech.Tests.TestWire;

namespace AnoMech.Tests;

// RelayClient against the real RelayServer over loopback.
[Category("Integration")]
public class RelayLoopbackTests
{
    // Membership, routing and ban rules are covered socket-free in RelayRoomsTests; this checks
    // that the client and relay agree on the wire.
    [Test]
    public async Task LiveRelay()
    {
        var log = new QuietLog();
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0 }, log);
        server.Start();
        var completed = false;
        try
        {
            var url = $"ws://127.0.0.1:{server.LocalEndPoint!.Port}";

            var hostSecret = RelayWire.NewSecret();
            var hostId = RelayWire.PeerId(hostSecret);
            using var host = new RelayClient(hostSecret);
            var code = await host.ConnectAndHostAsync(url);
            Assert.That(host.IsConnected && code != null, "client hosts room");
            var helloReceived = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
            host.MessageReceived += (message, fromHost, connection, sender) => { if (message is HelloMessage) helloReceived.TrySetResult(sender); };

            var peerSecret = RelayWire.NewSecret();
            var peerId = RelayWire.PeerId(peerSecret);
            using var peer = new RelayClient(peerSecret);
            var pingReceived = new TaskCompletionSource<(bool FromHost, Guid Sender)>(TaskCreationOptions.RunContinuationsAsynchronously);
            var compressedReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            peer.MessageReceived += (message, fromHost, connection, sender) =>
            {
                if (message is PingMessage) pingReceived.TrySetResult((fromHost, sender));
                if (message is AnnouncementMessage announcement) compressedReceived.TrySetResult(announcement.Text == new string('x', 512));
            };
            await peer.ConnectAsync(url, code!);
            Assert.That(peer.IsConnected, "client joins room");
            await peer.SendAsync(new HelloMessage(peerId, "Test", "1", "test"));
            Assert.That(await Within(helloReceived.Task, "hello"), Is.EqualTo(peerId), "relay stamps the authenticated sender");
            await host.SendAsync(new PingMessage(123));
            Assert.That(await Within(pingReceived.Task, "ping"), Is.EqualTo((true, hostId)), "host tag reaches clients");
            await host.SendAsync(new AnnouncementMessage(new string('x', 512)));
            Assert.That(await Within(compressedReceived.Task, "announcement"), "compressed payload survives the envelope");

            var rejected = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
            host.MessageRejected += (sender, fromHost, reason) => rejected.TrySetResult(sender);
            var roommateSecret = RelayWire.NewSecret();
            using var roommate = await ConnectRaw(url, code!, roommateSecret);
            await SendRaw(roommate, "{\"t\":\"pose\",");
            Assert.That(await Within(rejected.Task, "rejection"), Is.EqualTo(RelayWire.PeerId(roommateSecret)), "host drops a malformed peer message");
            Assert.That(host.IsConnected && peer.IsConnected, "and the room stays up");

            var kicked = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var removedNotice = new TaskCompletionSource<IReadOnlyList<Guid>>(TaskCreationOptions.RunContinuationsAsynchronously);
            peer.Disconnected += e => kicked.TrySetResult(e);
            host.PeersRemoved += ids => removedNotice.TrySetResult(ids);
            await host.ModerateAsync("ban", peerId);
            await Within(kicked.Task, "ban disconnect");
            Assert.That(peer.IsConnected, Is.False, "ban disconnects the peer");
            Assert.That(await ClosedByRelay(roommate), "ban closes other identities on the same address");
            Assert.That(await Within(removedNotice.Task, "removal notice"), Is.EqualTo(new[] { RelayWire.PeerId(roommateSecret) }), "host told who else the ban removed");
            Assert.That(host.IsConnected, "ban leaves the host connected");
            completed = true;
        }
        finally
        {
            if (!completed) TestContext.Out.WriteLine(string.Join(Environment.NewLine, log.Lines));
        }
    }

    // An address typed without a scheme dials wss:// first. A plain relay turns that away at once,
    // so the ws:// fallback doesn't wait out the fallback cap.
    [Test]
    public async Task BareAddressJoinDoesNotWaitOutTheFallbackCap()
    {
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0 }, new QuietLog());
        server.Start();
        var url = $"127.0.0.1:{server.LocalEndPoint!.Port}";

        using var host = new RelayClient(RelayWire.NewSecret());
        var code = await Within(host.ConnectAndHostAsync(url), "host connect");
        Assert.That(code, Is.Not.Null, "hosted from a bare address");
        Assert.That(host.FellBackToUnencrypted, "hosted over the ws:// fallback");
        var helloReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.MessageReceived += (message, fromHost, connection, sender) => { if (message is HelloMessage) helloReceived.TrySetResult(true); };

        var peerSecret = RelayWire.NewSecret();
        using var peer = new RelayClient(peerSecret);
        var answered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        peer.MessageReceived += (message, fromHost, connection, sender) => { if (fromHost && message is PingMessage) answered.TrySetResult(true); };
        var elapsed = Stopwatch.StartNew();
        await Within(peer.ConnectAsync(url, code!), "peer connect");
        Assert.That(peer.IsConnected, "joined from a bare address");
        await peer.SendAsync(new HelloMessage(RelayWire.PeerId(peerSecret), "Test", "1", "test"));
        await Within(helloReceived.Task, "hello");
        await host.SendAsync(new PingMessage(1));
        await Within(answered.Task, "host answer");
        Assert.That(elapsed.Elapsed, Is.LessThan(peer.FallbackAfter), "host answered before the cap would have run out");
    }

    // A plain relay older than RelayHttp's TLS check holds a bare address's wss:// attempt for its
    // 10 s handshake timeout, and a port that silently drops packets holds it until the OS gives
    // up. The ws:// fallback must still get the join answered before MultiplayerManager abandons
    // it, 15 s after Join.
    [Test]
    public async Task BareAddressJoinGetsPastAnEncryptedAttemptThatNeverAnswers()
    {
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0 }, new QuietLog());
        server.Start();
        await using var front = new RelayFront(server.LocalEndPoint!.Port, holdEncrypted: true, TimeSpan.Zero);

        using var host = new RelayClient(RelayWire.NewSecret());
        var code = await Within(host.ConnectAndHostAsync($"ws://127.0.0.1:{server.LocalEndPoint.Port}"), "host connect");
        var helloReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.MessageReceived += (message, fromHost, connection, sender) => { if (message is HelloMessage) helloReceived.TrySetResult(true); };

        var peerSecret = RelayWire.NewSecret();
        using var peer = new RelayClient(peerSecret);
        var answered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        peer.MessageReceived += (message, fromHost, connection, sender) => { if (fromHost && message is PingMessage) answered.TrySetResult(true); };
        var joinDeadline = TimeSpan.FromSeconds(15);
        var elapsed = Stopwatch.StartNew();
        await peer.ConnectAsync($"127.0.0.1:{front.Port}", code!).WaitAsync(joinDeadline);
        Assert.That(front.EncryptedAttempts, Is.EqualTo(1), "the wss:// attempt was made and left unanswered");
        Assert.That(peer.IsConnected && peer.FellBackToUnencrypted, "joined over the ws:// fallback");
        await peer.SendAsync(new HelloMessage(RelayWire.PeerId(peerSecret), "Test", "1", "test"));
        await Within(helloReceived.Task, "hello");
        await host.SendAsync(new PingMessage(1));
        await Within(answered.Task, "host answer");
        Assert.That(elapsed.Elapsed, Is.LessThan(joinDeadline), "host answered before the join is abandoned");
    }

    // The cap only moves on to a fallback: with none left, a slow relay is waited for, whether the
    // address names its scheme or the fallback itself is slow.
    [TestCase("ws://")]
    [TestCase("")]
    public async Task SlowRelayIsWaitedForWhenNoFallbackIsLeft(string scheme)
    {
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0 }, new QuietLog());
        server.Start();
        await using var front = new RelayFront(server.LocalEndPoint!.Port, holdEncrypted: false, TimeSpan.FromSeconds(1));
        using var host = new RelayClient(RelayWire.NewSecret()) { FallbackAfter = TimeSpan.FromMilliseconds(200) };
        Assert.That(await Within(host.ConnectAndHostAsync($"{scheme}127.0.0.1:{front.Port}"), "host connect"), Is.Not.Null, "hosted through the slow relay");
        Assert.That(host.FellBackToUnencrypted, Is.EqualTo(scheme == ""));
    }

    [Test]
    public async Task StoppingTheRelayDropsItsConnections()
    {
        await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = 0 }, new QuietLog());
        server.Start();
        var port = server.LocalEndPoint!.Port;
        using var host = new RelayClient(RelayWire.NewSecret());
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Disconnected += _ => disconnected.TrySetResult();
        Assert.That(await host.ConnectAndHostAsync($"ws://127.0.0.1:{port}"), Is.Not.Null, "hosts over the managed listener");
        await server.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(host.IsConnected, Is.False);
    }

    private static async Task<T> Within<T>(Task<T> task, string what)
    {
        try { return await task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException) { throw new TimeoutException($"Timed out: {what}"); }
    }

    private static async Task Within(Task task, string what)
    {
        try { await task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException) { throw new TimeoutException($"Timed out: {what}"); }
    }

    private static async Task<ClientWebSocket> ConnectRaw(string url, string code, string secret)
    {
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("X-AnoMech-Protocol", RelayWire.Version.ToString());
        socket.Options.SetRequestHeader("X-AnoMech-Peer-Secret", secret);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await socket.ConnectAsync(new Uri($"{url}/session/{code}"), timeout.Token);
        var frame = await ReadFrame(socket);
        Assert.That(frame.Type, Is.EqualTo(WebSocketMessageType.Text));
        Assert.That(JsonDocument.Parse(frame.Bytes).RootElement.GetProperty("peerId").GetGuid(), Is.EqualTo(RelayWire.PeerId(secret)),
            "greeting names the authenticated identity");
        return socket;
    }

    private static async Task SendRaw(ClientWebSocket socket, string text)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await socket.SendAsync(Bytes(text), WebSocketMessageType.Text, true, timeout.Token);
    }

    private static async Task<(byte[] Bytes, WebSocketMessageType Type)> ReadFrame(ClientWebSocket socket)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var output = new MemoryStream(); var buffer = new byte[65536];
        WebSocketReceiveResult result;
        do { result = await socket.ReceiveAsync(buffer, timeout.Token); output.Write(buffer, 0, result.Count); } while (!result.EndOfMessage);
        return (output.ToArray(), result.MessageType);
    }

    // Stands in front of the relay at relayPort. A connection that opens with a TLS ClientHello is
    // left unanswered when holdEncrypted; every other one reaches the relay after delay.
    private sealed class RelayFront : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new();
        private readonly ConcurrentBag<TcpClient> accepted = [];
        private readonly Task accepting;
        private int encryptedAttempts;

        public RelayFront(int relayPort, bool holdEncrypted, TimeSpan delay)
        {
            listener.Start();
            accepting = AcceptAsync(relayPort, holdEncrypted, delay);
        }

        public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
        public int EncryptedAttempts => Volatile.Read(ref encryptedAttempts);

        private async Task AcceptAsync(int relayPort, bool holdEncrypted, TimeSpan delay)
        {
            try
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync(stop.Token);
                    accepted.Add(client);
                    _ = ServeAsync(client, relayPort, holdEncrypted, delay);
                }
            }
            catch (Exception) when (stop.IsCancellationRequested) { }
        }

        private async Task ServeAsync(TcpClient client, int relayPort, bool holdEncrypted, TimeSpan delay)
        {
            try
            {
                var stream = client.GetStream();
                var first = new byte[1];
                if (await stream.ReadAsync(first, stop.Token) == 0) return;
                if (first[0] == 0x16)
                {
                    Interlocked.Increment(ref encryptedAttempts);
                    if (holdEncrypted) return;
                }
                await Task.Delay(delay, stop.Token);
                using var relay = new TcpClient();
                await relay.ConnectAsync(IPAddress.Loopback, relayPort, stop.Token);
                var upstream = relay.GetStream();
                await upstream.WriteAsync(first, stop.Token);
                await Task.WhenAny(stream.CopyToAsync(upstream, stop.Token), upstream.CopyToAsync(stream, stop.Token));
                client.Dispose();
            }
            catch (Exception) { }
        }

        public async ValueTask DisposeAsync()
        {
            stop.Cancel();
            listener.Stop();
            foreach (var client in accepted) client.Dispose();
            await accepting;
            stop.Dispose();
        }
    }

    // A close frame or an abort, after any room traffic already on its way; silence is a failure.
    private static async Task<bool> ClosedByRelay(ClientWebSocket socket)
    {
        try
        {
            for (var frames = 0; frames < 64; frames++)
                if ((await ReadFrame(socket)).Type == WebSocketMessageType.Close) return true;
            return false;
        }
        catch (WebSocketException) { return true; }
        catch (OperationCanceledException) { return false; }
    }
}
