using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Scenarios.Ucob;
using AsuMech.Scenarios.Ucob.P4Adds;
using static AsuMech.Core.Game.Party.PartyRole;
using Ai = AsuMech.Scenarios.Ucob.P4Adds.UcobP4AddsTuuflessAi;

namespace AsuMech.Tests;

public class UcobP4AddsTuuflessAiTests
{
    [Test]
    public void TheNeurolinksSitTopLeftTopRightAndBottom()
    {
        Assert.That(Ai.TopLeftNeurolink.X, Is.LessThan(-5f));
        Assert.That(Ai.TopLeftNeurolink.Y, Is.LessThan(-2f));
        Assert.That(Ai.TopRightNeurolink.X, Is.GreaterThan(5f));
        Assert.That(Ai.TopRightNeurolink.Y, Is.LessThan(-2f));
        Assert.That(System.MathF.Abs(Ai.BottomNeurolink.X), Is.LessThan(1f));
        Assert.That(Ai.BottomNeurolink.Y, Is.GreaterThan(8f));
    }

    [Test]
    public void WithTheFlexUnmarkedEachMarkedDpsTakesItsOwnNeurolink()
    {
        var claimed = Ai.HatchNeurolinks([MeleeDpsA, MeleeDpsB, PhysRangedDps]);
        Assert.That(claimed, Is.EquivalentTo(new Dictionary<PartyRole, Vector2>
        {
            [MeleeDpsA] = Ai.TopLeftNeurolink, [MeleeDpsB] = Ai.TopRightNeurolink, [PhysRangedDps] = Ai.BottomNeurolink,
        }));
    }

    [TestCase(MeleeDpsA)]
    [TestCase(MeleeDpsB)]
    [TestCase(PhysRangedDps)]
    public void AMarkedFlexTakesTheUnmarkedDpsNeurolink(PartyRole unmarked)
    {
        var marked = new List<PartyRole> { MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps };
        marked.Remove(unmarked);
        var claimed = Ai.HatchNeurolinks(marked);
        var home = Ai.HatchNeurolinks([MeleeDpsA, MeleeDpsB, PhysRangedDps])[unmarked];
        Assert.That(claimed[CasterDps], Is.EqualTo(home));
        Assert.That(claimed.Keys, Has.No.Member(unmarked));
        Assert.That(claimed.Values, Is.Unique);
    }

    [Test]
    public void TheStratPlaysOnElementalMarkers()
        => Assert.That(UcobMarkers.Of(new UcobP4AddsScenario().AiStrats, 1), Is.SameAs(UcobMarkers.Elemental));
}
