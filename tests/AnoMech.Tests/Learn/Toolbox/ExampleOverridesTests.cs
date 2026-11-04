using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Learn.Toolbox;
using AnoMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Toolbox;

public class ExampleOverridesTests
{
    [Test]
    public void ValuesLandOnTheMatchingPropertiesAndLiftClearsThem()
    {
        var overrides = new UwuP1GarudaStateOverrides();
        var applied = ExampleOverrides.Apply(overrides, new Dictionary<string, string>
        {
            ["SuparnaSongBearing"] = "270",
            ["mistralSongTarget"] = "ShieldHealer",
            ["SatinPlumesFirst"] = "[(16,-4),(-4,16),(-12,-12),(-16,4)]",
            ["SistersSongTargets"] = "[CasterDps,RegenHealer]",
            ["NoSuchRoll"] = "1",
            ["ChiradaSongBearing"] = "north",
        });
        Assert.That(applied, Is.EquivalentTo(new[] { "SuparnaSongBearing", "MistralSongTarget", "SatinPlumesFirst", "SistersSongTargets" }));
        Assert.That(overrides.SuparnaSongBearing, Is.EqualTo(270f));
        Assert.That(overrides.MistralSongTarget, Is.EqualTo(PartyRole.ShieldHealer));
        Assert.That(overrides.SatinPlumesFirst, Is.EqualTo(new[] { new Vector2(16f, -4f), new Vector2(-4f, 16f), new Vector2(-12f, -12f), new Vector2(-16f, 4f) }));
        Assert.That(overrides.SistersSongTargets, Is.EqualTo(new[] { PartyRole.CasterDps, PartyRole.RegenHealer }));
        Assert.That(overrides.ChiradaSongBearing, Is.Null);
        ExampleOverrides.Lift(overrides, applied);
        Assert.That(overrides.Any, Is.False);
    }
}
