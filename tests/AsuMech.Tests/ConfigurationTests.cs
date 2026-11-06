using AsuMech.Multiplayer;
using AsuMech.Network;

namespace AsuMech.Tests;

public class ConfigurationTests
{
    [Test]
    public void PasswordSavedBeforeOriginsBindsToTheSavedRelay()
    {
        var config = new Configuration { RelayServerUrl = "relay.example.com", RelayAccessToken = "secret" };
        Assert.That(config.TokenForRelay("relay.example.com"), Is.EqualTo("secret"));
        Assert.That(config.RelayTokenOrigin, Is.EqualTo("wss://relay.example.com:443"));
    }

    [TestCase("wss://relay.example.com/path", "secret")]
    [TestCase("other.example.com", "")]
    [TestCase("relay.example.com:8443", "")]
    [TestCase("ws://relay.example.com:443", "")]
    [TestCase("not a url ::", "")]
    public void SavedPasswordOnlyReturnedForTheRelayItWasEnteredFor(string url, string expected)
    {
        var config = new Configuration { RelayServerUrl = "relay.example.com", RelayAccessToken = "secret" };
        Assert.That(config.TokenForRelay(url), Is.EqualTo(expected));
    }

    [Test]
    public void DefaultServerGetsItsBuiltInTokenAndNoOtherDoes()
    {
        var config = new Configuration();
        Assert.That(config.TokenForRelay(DefaultRelay.Url), Is.EqualTo(DefaultRelay.Token));
        Assert.That(config.TokenForRelay(DefaultRelay.UpstreamUrl), Is.EqualTo(""));
    }

    [Test]
    public void ActiveServerFollowsTheChoice()
    {
        var config = new Configuration { RelayServerUrl = " relay.example.com " };
        Assert.That(config.ActiveRelayUrl(), Is.EqualTo(DefaultRelay.Url));
        config.UseCustomRelay = true;
        Assert.That(config.ActiveRelayUrl(), Is.EqualTo("relay.example.com"));
    }

    [Test]
    public void PerInstallCredentialIsStable()
    {
        var config = new Configuration();
        var first = config.EnsurePeerSecret();
        Assert.That(RelayWire.IsValidSecret(first));
        Assert.That(config.EnsurePeerSecret(), Is.EqualTo(first));
    }

    [Test]
    public void InvalidSavedCredentialIsReplaced()
    {
        var config = new Configuration { PeerSecret = "tampered" };
        Assert.That(RelayWire.IsValidSecret(config.EnsurePeerSecret()));
        Assert.That(config.PeerSecret, Is.Not.EqualTo("tampered"));
    }

    [Test]
    public void RecordingsGoToTheBuiltInServerUnlessOverridden()
    {
        var config = new Configuration();
        Assert.That(config.RecordingsDestination(), Is.EqualTo((DefaultRecordingsRelay.Url, DefaultRecordingsRelay.Token.Length > 0 ? DefaultRecordingsRelay.Token : null)));
        config.RecordingsRelayUrl = " wss://mine.example ";
        Assert.That(config.RecordingsDestination(), Is.EqualTo(("wss://mine.example", (string?)null)), "an own server without a password");
        config.RecordingsRelayToken = "pw";
        Assert.That(config.RecordingsDestination(), Is.EqualTo(("wss://mine.example", (string?)"pw")));
    }
}
