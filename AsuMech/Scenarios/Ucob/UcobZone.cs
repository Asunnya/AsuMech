using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Map;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Ucob;

public sealed class UcobZone : IZone
{
    public static readonly UcobZone Instance = new();
    // UNVERIFIED: P1 and P2's weather and track are borrowed from P5.
    public static readonly Phase P1 = new(Instance, "P1", 32, 499);
    public static readonly Phase P2 = new(Instance, "P2", 32, 499);
    // UNVERIFIED: P3 and P4's weather and track are borrowed from P5 as well.
    public static readonly Phase P3 = new(Instance, "P3", 32, 499);
    public static readonly Phase P4 = new(Instance, "P4", 32, 499);
    public static readonly Phase P5 = new(Instance, "P5", 32, 499,
        init: world =>
        {
            world.EnforceArenaBoundary(UcobConstants.Geometry.P5ArenaRadius);
            world.Events.Add(ArenaBeatAt, () => Instance.p5Arena?.PlayBeat(4, 0x8));
        });

    // The arena's SharedGroups are bound to EObjs the server respawns on every wipe reset; unbound,
    // they keep the layout's pre-pull look (the barrier ring, no death wall). A beat only lands once
    // the SharedGroup has attached, about 1s after the spawn.
    private const uint ArenaEObjId = 2007457;
    private const float ArenaBeatAt = 1f;
    private SimEventObject? p5Arena;

    public string Name => "The Unending Coil of Bahamut";

    public string ShortName => "UCoB";

    public string? Expansion => "Stormblood";
    public uint TerritoryId => 733;
    public Vector3 Origin => new(0f, 0f, 0f);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("Aether Markers", UcobConstants.AetherWaymarks)];

    public DutyInitData DutyInitData => UcobReplayData.DutyInitData;

    // The zone loads with its default (P1/P3) arena geometry rather than the Golden Bahamut
    // floor — dressing each phase correctly means switching whole native layout layers, which
    // needs engine support this codebase doesn't have yet. Known but not implemented here.
    //
    // TODO: Spotted issue with rendering, arena appears dependent on inn the sim starts in
    public void Run(SimWorld world)
    {
        var deathWall = SpawnArenaEObj(world, UcobConstants.ArenaLayoutId.DeathWall, 0x6BF5D1, 1);
        p5Arena = SpawnArenaEObj(world, UcobConstants.ArenaLayoutId.P5Arena, 0x6D0597, 1);
        SpawnArenaEObj(world, UcobConstants.ArenaLayoutId.Piece1, 0x6D0616, 1);
        SpawnArenaEObj(world, UcobConstants.ArenaLayoutId.Piece2, 0x6B6DD9, 4);
        SpawnArenaEObj(world, UcobConstants.ArenaLayoutId.Piece3, 0x6B6DBB, 1);
        SpawnArenaEObj(world, UcobConstants.ArenaLayoutId.Piece4, 0x6B6CB2, 1);
        world.Events.Add(ArenaBeatAt, () => deathWall?.PlayBeat(4, 0x8));
    }

    private static SimEventObject? SpawnArenaEObj(SimWorld world, uint layoutId, uint gimmickId, ushort timelineState)
        => world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = ArenaEObjId,
            Placement = new Placement(Vector3.Zero, 0f),
            TargetableStatus = 5,
            LayoutId = layoutId,
            GimmickId = gimmickId,
            TimelineState = timelineState,
        });
}
