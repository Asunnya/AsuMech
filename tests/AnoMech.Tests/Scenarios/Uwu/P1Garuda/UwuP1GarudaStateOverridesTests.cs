using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Uwu.P1Garuda;
using NUnit.Framework;

namespace AnoMech.Tests.Scenarios.Uwu;

public class UwuP1GarudaStateOverridesTests
{
    [Test]
    public void APinnedRollLeavesTheSeedsOtherRollsAlone()
    {
        var free = new UwuP1GarudaState(new Rng(7));
        var pinned = new UwuP1GarudaState(new Rng(7), new UwuP1GarudaStateOverrides
        {
            SuparnaSongBearing = 270f,
            SatinPlumesFirst = [new Vector2(15f, -3f), new Vector2(-5f, 15f), new Vector2(-11f, -11f), new Vector2(-15f, 5f)],
        });
        Assert.That(pinned.SuparnaSongSpot, Is.EqualTo(UwuP1GarudaState.AtBearing(270f, UwuP1GarudaState.SongSpotRadius)));
        Assert.That(pinned.SatinPlumesFirst, Is.EqualTo(new[] { new Vector3(16f, 0f, -4f), new Vector3(-4f, 0f, 16f), new Vector3(-12f, 0f, -12f), new Vector3(-16f, 0f, 4f) }));
        Assert.That(pinned.ChiradaSongSpot, Is.EqualTo(free.ChiradaSongSpot));
        Assert.That(pinned.MistralSongTarget, Is.EqualTo(free.MistralSongTarget));
        Assert.That(pinned.SistersSongTargets, Is.EqualTo(free.SistersSongTargets));
        Assert.That(pinned.FrictionTargets, Is.EqualTo(free.FrictionTargets));
        Assert.That(pinned.SatinPlumesSecond, Is.EqualTo(free.SatinPlumesSecond));
        Assert.That(pinned.MesohighTargets, Is.EqualTo(free.MesohighTargets));
    }

    [Test]
    public void FewerThanFourPlumesPinNothing()
    {
        var free = new UwuP1GarudaState(new Rng(3));
        var pinned = new UwuP1GarudaState(new Rng(3), new UwuP1GarudaStateOverrides { SatinPlumesFirst = [new Vector2(16f, 4f)] });
        Assert.That(pinned.SatinPlumesFirst, Is.EqualTo(free.SatinPlumesFirst));
    }
}
