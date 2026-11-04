using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Learn.Tools;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Tools;

public class RuleMinerTests
{
    private static DumpedOrder Order(int seed, string role, float x, float z, float time = 10f, int line = 40)
        => new("Ai", time, $"Ai.cs:{line}", null, role, seed, time, null, x, z);

    private static DumpedRun Run(int seed, params (string Field, string Value)[] state)
        => new(seed, state.ToDictionary(s => s.Field, s => s.Value), []);

    [Test]
    public void ASpotSharedByEverySeedIsFixed()
    {
        var dump = new OrderDump("S", "A", 0,
            [Order(1, "MainTank", 0f, -10f), Order(2, "MainTank", 0.5f, -10.5f), Order(3, "MainTank", -0.3f, -9.8f)],
            [Run(1, ("Side", "East")), Run(2, ("Side", "West")), Run(3, ("Side", "East"))]);
        var rule = RuleMiner.Mine(dump).Single();
        Assert.That(rule.Fixed, Is.True);
        Assert.That(rule.Spots[0].Label, Is.EqualTo("N 10m"));
        Assert.That(rule.Spots[0].Seeds, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void TheFieldWhoseValuePicksTheSpotExplainsIt()
    {
        var dump = new OrderDump("S", "A", 0,
            [Order(1, "MainTank", 15f, 0f), Order(2, "MainTank", -15f, 0f), Order(3, "MainTank", 15f, 0f), Order(4, "MainTank", -15f, 0f)],
            [Run(1, ("Side", "East"), ("Noise", "a")), Run(2, ("Side", "West"), ("Noise", "b")), Run(3, ("Side", "East"), ("Noise", "c")), Run(4, ("Side", "West"), ("Noise", "d"))]);
        var rule = RuleMiner.Mine(dump).Single();
        Assert.That(rule.Fields, Is.EqualTo(new[] { "Side" }));
        Assert.That(rule.SpotByValue["East"], Is.EqualTo("E wall"));
        Assert.That(rule.SpotByValue["West"], Is.EqualTo("W wall"));
    }

    [Test]
    public void BeingNamedInATargetListIsAFeatureOfItsOwn()
    {
        var dump = new OrderDump("S", "A", 0,
            [Order(1, "RegenHealer", 0f, 0f), Order(2, "RegenHealer", 0f, 12f), Order(3, "RegenHealer", 0f, 0f), Order(4, "RegenHealer", 0f, 12f)],
            [Run(1, ("Targets", "[RegenHealer,MeleeDpsA]")), Run(2, ("Targets", "[CasterDps,MeleeDpsB]")), Run(3, ("Targets", "[MeleeDpsB,RegenHealer]")), Run(4, ("Targets", "[ShieldHealer,CasterDps]"))]);
        var rule = RuleMiner.Mine(dump).Single();
        Assert.That(rule.Fields, Is.EqualTo(new[] { "Targets has RegenHealer" }));
        Assert.That(rule.SpotByValue["yes"], Is.EqualTo("centre"));
        Assert.That(rule.SpotByValue["no"], Is.EqualTo("S 12m"));
    }

    [Test]
    public void AFieldAsVariedAsTheSeedsExplainsNothing()
    {
        var dump = new OrderDump("S", "A", 0,
            [Order(1, "MainTank", 15f, 0f), Order(2, "MainTank", -15f, 0f), Order(3, "MainTank", 0f, 15f)],
            [Run(1, ("Noise", "a")), Run(2, ("Noise", "b")), Run(3, ("Noise", "c"))]);
        var rule = RuleMiner.Mine(dump).Single();
        Assert.That(rule.Explained, Is.False);
        Assert.That(rule.Spots, Has.Count.EqualTo(3));
    }

    [Test]
    public void CompassNamesWallsAndWaymarks()
    {
        Assert.That(RuleMiner.Compass(new Vector2(0f, -18f)), Is.EqualTo("N wall"));
        Assert.That(RuleMiner.Compass(new Vector2(7f, 7f)), Is.EqualTo("SE 10m"));
        var layout = new global::AnoMech.Core.Game.WaymarkLayout("t", [new global::AnoMech.Core.Game.Waymark(global::AnoMech.Core.Game.WaymarkSlot.A, new Vector3(0f, 0f, -10f)), new global::AnoMech.Core.Game.Waymark(global::AnoMech.Core.Game.WaymarkSlot.Two, new Vector3(10f, 0f, 0f))]);
        Assert.That(RuleMiner.Describe(new Vector2(0.5f, -10.5f), layout), Is.EqualTo("at A"));
        Assert.That(RuleMiner.Describe(new Vector2(12.5f, 1f), layout), Is.EqualTo("near 2 (E 13m)"));
    }
}
