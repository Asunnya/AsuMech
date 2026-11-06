using System.Net;
using AsuMech.Network;

namespace AsuMech.Relay.Tests;

public class RelayOptionsTests
{
    [Test]
    public void MappedProxyCidrIsNormalized()
    {
        Assert.That(RelayOptions.TryParseNetwork("::ffff:192.0.2.0/120", out var mappedNetwork));
        Assert.That(mappedNetwork.Contains(IPAddress.Parse("192.0.2.8")));
    }

    [Test]
    public void GroupSettingsGoTogether()
    {
        List<RelayGroupMember> group = [new(0, "wss://us.example.com", "US East"), new(1, "wss://de.example.com", "EU Central")];
        Assert.That(new RelayOptions { GroupNumber = 1, Group = group }.Validate(), Is.Null);
        Assert.That(new RelayOptions { GroupNumber = 2, Group = group }.Validate(), Is.Not.Null, "a number the list doesn't have");
        Assert.That(new RelayOptions { GroupNumber = 0 }.Validate(), Is.Not.Null, "a number without a list");
        Assert.That(new RelayOptions { Group = group }.Validate(), Is.Not.Null, "a list without a number");
        Assert.That(new RelayOptions { GroupNumber = 0, Group = [new(0, "wss://us.example.com", "")] }.Validate(), Is.Not.Null, "an unusable list");
    }

    [Test]
    public void RecordingsNeedSomeoneToAnswerFor()
    {
        var peer = Guid.NewGuid();
        Assert.That(new RelayOptions { RecordingsDir = "r" }.Validate(), Is.Not.Null, "open to anyone");
        Assert.That(new RelayOptions { RecordingsDir = "r", RecordingsFrom = [peer] }.Validate(), Is.Null);
        Assert.That(new RelayOptions { RecordingsDir = "r", AccessToken = "join-token-0123456789abcdef", TrustedProxies = [IPNetwork.Parse("127.0.0.1/32")] }.Validate(), Is.Null);
        Assert.That(new RelayOptions { RecordingsFrom = [peer] }.Validate(), Is.Null, "a list without a dir is just unused");
    }
}
