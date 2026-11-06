using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using AnoMech.Multiplayer;
using AnoMech.Network;
using AnoMech.Relay;

namespace AnoMech.Tests;

public class RecordingUploaderTests
{
    private sealed class SilentLog : IRelayLog
    {
        public void Info(string message) { }
        public void Warn(string message) { }
        public void Detail(string message) { }
    }

    // The credential, and so the peer id, is keyed to the relay address, so a relay that lists
    // the uploader has to be bound to a port known before it starts.
    [Test]
    public async Task UploadsTheGzippedCsvUnderTheRelayCredential()
    {
        var dir = Directory.CreateTempSubdirectory("anomech-upload-").FullName;
        var csv = Path.Combine(dir, "1232_20261105_213000.csv");
        var text = string.Join(",", SimTimeline.Columns) + "\n1.000,pos\n2.000,cast\n";
        await File.WriteAllTextAsync(csv, text);
        var installSecret = RelayWire.NewSecret();
        var stored = Path.Combine(dir, "stored");
        try
        {
            var port = FreePort();
            var relay = $"ws://127.0.0.1:{port}";
            var credential = RelayWire.RelayCredential(installSecret, relay);

            await using (var strangers = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = port, RecordingsDir = stored, RecordingsFrom = [Guid.NewGuid()] }, new SilentLog()))
            {
                strangers.Start();
                var refused = await RecordingUploader.UploadAsync(csv, relay, null, credential);
                Assert.That(refused.Ok, Is.False);
                Assert.That(refused.Message, Does.Contain("403"));
                Assert.That(Directory.Exists(stored), Is.False);
            }

            await using var listed = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = port, RecordingsDir = stored, RecordingsFrom = [RelayWire.PeerId(credential)] }, new SilentLog());
            listed.Start();
            var result = await RecordingUploader.UploadAsync(csv, relay, null, credential);
            Assert.That(result.Ok, result.Message);
            Assert.That(result.Message, Does.Contain("1232_20261105_213000_"));
            var file = Directory.GetFiles(stored, "*.csv.gz").Single();
            using var gzip = new GZipStream(File.OpenRead(file), CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            Assert.That(await reader.ReadToEndAsync(), Is.EqualTo(text));

            var withToken = await RecordingUploader.UploadAsync(csv, relay, "join-token-0123456789abcdef", credential);
            Assert.That(withToken.Ok, Is.False, "a password never goes over plain http");
            Assert.That(withToken.Message, Does.Contain("https"));
            Assert.That(Directory.GetFiles(stored), Has.Length.EqualTo(1));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
