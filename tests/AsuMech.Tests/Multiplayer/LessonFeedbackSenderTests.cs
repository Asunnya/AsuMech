using System.Net;
using System.Net.Sockets;
using AsuMech.Multiplayer;
using AsuMech.Network;
using AsuMech.Relay;

namespace AsuMech.Tests;

public class LessonFeedbackSenderTests
{
    private sealed class SilentLog : IRelayLog
    {
        public void Info(string message) { }
        public void Warn(string message) { }
        public void Detail(string message) { }
    }

    private static readonly LessonFeedback Up = new("UwuP1GarudaScenario|NAUR", "UwuP1GarudaAi@12.5", LessonFeedback.Toolbox, 1, "", "0.6.0.0");

    [Test]
    public async Task VoteReachesTheStudyServerUnderTheRelayCredential()
    {
        var dir = Directory.CreateTempSubdirectory("asumech-vote-").FullName;
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            var relay = $"ws://127.0.0.1:{port}";
            var credential = RelayWire.RelayCredential(RelayWire.NewSecret(), relay);
            await using var server = new RelayServer(new RelayOptions { BindAddress = IPAddress.Loopback, Port = port, RecordingsDir = dir, RecordingsFrom = [Guid.NewGuid()] }, new SilentLog());
            server.Start();

            var sent = await LessonFeedbackSender.SendAsync(Up, relay, null, credential);
            Assert.That(sent.Ok, sent.Message);
            var line = File.ReadAllLines(Path.Combine(dir, "feedback.jsonl")).Single();
            Assert.That(line, Does.Contain("UwuP1GarudaAi@12.5").And.Contain(RelayWire.PeerId(credential).ToString("N")[..8]));

            var withToken = await LessonFeedbackSender.SendAsync(Up, relay, "join-token-0123456789abcdef", credential);
            Assert.That(withToken.Ok, Is.False, "a password never goes over plain http");
            Assert.That((await LessonFeedbackSender.SendAsync(Up with { Vote = 0 }, relay, null, credential)).Ok, Is.False);
        }
        finally { Directory.Delete(dir, true); }
    }
}
