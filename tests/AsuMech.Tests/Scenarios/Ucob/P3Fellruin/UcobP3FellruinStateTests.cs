using System;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P3Fellruin;

namespace AsuMech.Tests;

public class UcobP3FellruinStateTests
{
    private static readonly PartyRole[] Everyone = Enum.GetValues<PartyRole>();

    [Test]
    public void TheQuoteIsOneOfTheChoices([Range(0, 49)] int seed)
        => Assert.That(UcobP3FellruinState.QuoteChoices, Does.Contain(new UcobP3FellruinState(new Rng(seed)).Quote));

    [Test]
    public void APickedQuoteIsKept([Values(0, 1)] int pick)
        => Assert.That(new UcobP3FellruinState(new Rng(3), new UcobP3FellruinStateOverrides { Quote = pick }).Quote,
            Is.EqualTo(UcobP3FellruinState.QuoteChoices[pick]));

    [Test]
    public void FourDistinctPlayersTakeMeteorStreams([Range(0, 49)] int seed)
        => Assert.That(new UcobP3FellruinState(new Rng(seed)).MeteorTargets, Has.Count.EqualTo(UcobP3FellruinState.MeteorStreams).And.Unique);

    [Test]
    public void APickedMeteorStreamRoleIsAmongThem([ValueSource(nameof(Everyone))] PartyRole role)
        => Assert.That(new UcobP3FellruinState(new Rng(5), new UcobP3FellruinStateOverrides { MeteorIncludes = role }).MeteorTargets, Does.Contain(role));

    [Test]
    public void TheThreeBossesTakeTheThreeSpotsBetweenTheNeurolinks([Range(0, 49)] int seed)
    {
        var state = new UcobP3FellruinState(new Rng(seed));
        var spots = new[] { state.Bahamut, state.Nael, state.Twintania };
        Assert.That(spots.Select(s => (MathF.Round(s.X, 1), MathF.Round(s.Y, 1))), Is.Unique);
        foreach (var boss in spots)
        foreach (var neurolink in UcobConstants.Geometry.P3Neurolinks)
            Assert.That(Vector2.Distance(boss, neurolink), Is.GreaterThan(7f));
    }

    [Test]
    public void EachBahamutSpotLeavesThreeDistinctNeurolinks([Values(0, 1, 2)] int slot)
    {
        var bearing = UcobP3FellruinState.BossBearings[slot];
        var picked = new[] { bearing + 60f, bearing - 60f, bearing + 180f }.Select(UcobP3FellruinState.NeurolinkNearest).ToList();
        Assert.That(picked, Is.Unique);
    }
}
