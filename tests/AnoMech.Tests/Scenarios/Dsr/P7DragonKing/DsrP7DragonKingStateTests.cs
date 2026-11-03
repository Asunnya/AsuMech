using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Dsr.P7DragonKing;

namespace AnoMech.Tests;

public class DsrP7DragonKingStateTests
{
    [Test]
    public void EachMechanicRollsItsOwnAscalon([Range(0, 49)] int seed)
        => Assert.That(new DsrP7DragonKingState(new Rng(seed)).Elements, Has.Count.EqualTo(8));

    [Test]
    public void ExaflaresPointAlongTheEightDirections([Range(0, 49)] int seed)
    {
        var state = new DsrP7DragonKingState(new Rng(seed));
        Assert.That(state.ExaflareHeadings, Has.Count.EqualTo(DsrP7DragonKingState.Cycles));
        Assert.That(state.ExaflareHeadings.SelectMany(h => h).All(h => h is >= 0 and < 8));
    }

    [Test]
    public void GigaflaresBlastEachSpotOnceOppositeIt([Range(0, 49)] int seed)
    {
        var state = new DsrP7DragonKingState(new Rng(seed));
        for (var cycle = 0; cycle < DsrP7DragonKingState.GigaflareCycles; cycle++)
        {
            Assert.That(state.GigaflareOrder[cycle].OrderBy(i => i), Is.EqualTo(new[] { 0, 1, 2 }));
            for (var blast = 0; blast < 3; blast++)
            {
                var spot = state.GigaflareSpot(cycle, blast);
                Assert.That(spot.Length(), Is.EqualTo(DsrP7DragonKingState.GigaflareRadius).Within(0.01f));
                var under = DsrP7DragonKingState.Spot((EdgeSpot)state.GigaflareOrder[cycle][blast]);
                Assert.That(Vector2.Dot(Vector2.Normalize(spot), Vector2.Normalize(under)), Is.EqualTo(-1f).Within(0.001f));
            }
        }
    }

    // The clear's three cycles: its exaflare headings, Ascalons and Gigaflare orders.
    internal static DsrP7DragonKingStateOverrides Pinned() => new()
    {
        ExaflareHeadings = [[4, 7, 2], [5, 3, 3], [5, 4, 3]],
        Elements =
        [
            AscalonElement.Flames, AscalonElement.Flames, AscalonElement.Flames,
            AscalonElement.Ice, AscalonElement.Flames, AscalonElement.Flames,
            AscalonElement.Flames, AscalonElement.Ice,
        ],
        GigaflareOrder = [[1, 0, 2], [0, 1, 2]],
    };
}
