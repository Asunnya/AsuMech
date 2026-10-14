using System.Numerics;
using AnoMech.Core.Map;
using AnoMech.Core.Native.Interfaces;
using NUnit.Framework;

namespace AnoMech.Tests;

public class MapControllerTests
{
    private static readonly Vector3 Spawn = new(100f, 0f, 116f);

    [Test]
    public void TheSpawnWallIsRetriedUntilTheZoneHasStreamedIn()
    {
        FakeGame.Install();
        var layout = new FakeLayoutFunctions { ColliderCallsBeforeStreamed = 3 };
        Natives.Layout = layout;
        var map = new MapController();

        map.DropSpawnWall(Spawn);
        for (var frame = 0; frame < 10; frame++) map.Tick();

        Assert.That(layout.ColliderDropCenters, Has.Count.EqualTo(4));
        Assert.That(layout.ColliderDropCenters, Is.All.EqualTo(Spawn));
    }

    [Test]
    public void LeavingTheZoneStopsTheSpawnWallRetry()
    {
        FakeGame.Install();
        var layout = new FakeLayoutFunctions { ColliderCallsBeforeStreamed = int.MaxValue };
        Natives.Layout = layout;
        var map = new MapController();

        map.DropSpawnWall(Spawn);
        map.Tick();
        map.Unload();
        map.Tick();

        Assert.That(layout.ColliderDropCenters, Has.Count.EqualTo(1));
    }
}
