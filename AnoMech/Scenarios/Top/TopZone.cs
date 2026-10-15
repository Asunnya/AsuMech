using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top;

public sealed class TopZone : IZone
{
    public static readonly TopZone Instance = new();
    public static readonly Phase P2 = new(Instance, "P2", 78, BgmId.TopP2,
        arenaMapEffectOverrides: new Dictionary<byte, ushort> { [0x0A] = 1 });
    public static readonly Phase P5 = new(Instance, "P5", 174, BgmId.TopP5);
    public static readonly Phase P6 = new(Instance, "P6", 175, BgmId.TopP6);

    public string Name => "The Omega Protocol";
    public uint TerritoryId => 1122;
    public Vector3 Origin => new(100f, 0f, 100f);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("Ring", Core.Game.WaymarkPresets.Ring(13.63f))];

    public EntranceBarrier EntranceBarrier => new(2007457, 0x91D196, 0x91D193, new Vector3(5f, 0f, 15f));

    public IReadOnlyList<ushort> ArenaMapEffectSnapshot { get; } =
        [1, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4];

    public void Run(SimWorld world) => world.EnforceArenaBoundary(Geometry.ArenaRadius);
}
