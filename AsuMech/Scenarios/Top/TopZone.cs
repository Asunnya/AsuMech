using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Map;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Top.TopConstants;

namespace AsuMech.Scenarios.Top;

public sealed class TopZone : IZone
{
    public static readonly TopZone Instance = new();
    public static readonly Phase P2 = new(Instance, "P2", 78, BgmId.TopP2,
        arenaMapEffectOverrides: new Dictionary<byte, ushort> { [0x0A] = 1 });
    public static readonly Phase P5 = new(Instance, "P5", 174, BgmId.TopP5);
    public static readonly Phase P6 = new(Instance, "P6", 175, BgmId.TopP6);

    // Each slot's resting state when the phase's scenarios start, applied from the first frame so
    // nothing plays its transition. Slots 1-8 are the eyes around the edge, hidden until a mechanic
    // opens one.
    private static readonly (byte Slot, ushort State)[] P1ArenaStates = [(0x00, 1), ..Hidden(0x01, 0x08), ..Hidden(0x0C, 0x13)];
    private static readonly (byte Slot, ushort State)[] P3IntermissionArenaStates =
        [(0x00, 1), (0x01, 1), ..Hidden(0x02, 0x08), (0x09, 4), (0x0A, 1), ..Hidden(0x0C, 0x13)];
    private static readonly (byte Slot, ushort State)[] P3ArenaStates = [(0x00, 1), ..Hidden(0x01, 0x0B), ..Hidden(0x0C, 0x13)];
    private static readonly (byte Slot, ushort State)[] P6ArenaStates = [(0x00, 1), ..Hidden(0x01, 0x0B), ..Hidden(0x0C, 0x14)];

    public static readonly Phase P1 = new(Instance, "P1", 77, BgmId.TopP1, init: world => InitArena(world, P1ArenaStates));
    // Starts on P2's kill: P2's sky and track, until the scenario brings in P3's.
    public static readonly Phase P3Intermission = new(Instance, "P3", 78, BgmId.TopP2, init: world => InitArena(world, P3IntermissionArenaStates));
    public static readonly Phase P3 = new(Instance, "P3", 79, BgmId.TopP3, init: world => InitArena(world, P3ArenaStates));
    // Starts on P3's last moments, so P3's sky, arena and track carry on.
    public static readonly Phase P4 = new(Instance, "P4", 79, BgmId.TopP3, init: world => InitArena(world, P3ArenaStates));
    // A start past Alpha Omega's opening, with the arena edge already up.
    public static readonly Phase P6Continued = new(Instance, "P6", 175, BgmId.TopP6, init: world => InitArena(world, P6ArenaStates));

    // Cosmo Meteor's arena change. Nothing turns it back before the territory reloads.
    public const byte CosmoMeteorArenaSlot = 0x15;

    public string Name => "The Omega Protocol";

    public string ShortName => "TOP";

    public string? Expansion => "Endwalker";
    public uint TerritoryId => 1122;
    public Vector3 Origin => new(100f, 0f, 100f);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("Ring", Core.Game.WaymarkPresets.Ring(13.63f))];

    public DutyInitData DutyInitData => TopReplayData.DutyInitData;

    public void Run(SimWorld world) => world.EnforceArenaBoundary(Geometry.ArenaRadius);

    private static IEnumerable<(byte Slot, ushort State)> Hidden(byte first, byte last)
    {
        for (var slot = first; slot <= last; slot++) yield return (slot, 4);
    }

    private static void InitArena(SimWorld world, (byte Slot, ushort State)[] states) => world.Events.Add(0f, () =>
    {
        foreach (var (slot, state) in states)
            world.Map.AddEffect(((uint)state << 16) | state, slot);
    });
}
