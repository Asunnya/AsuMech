using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Umad;

public sealed class UmadZone : IZone
{
    public static readonly UmadZone Instance = new();

    public IReadOnlyList<ushort> ArenaMapEffectSnapshot { get; } =
    [
        1, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 1,
        1, 1, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
    ];

    private static readonly Dictionary<byte, ushort> P3ArenaMapEffectOverrides = new() { [0x00] = 0x200, [0x13] = 4 };

    // 77 is the zone-in weather between pulls; a pull switches to 78.
    public static readonly Phase P1 = new(Instance, "P1", 78, 20291,
        arenaMapEffectOverrides: new Dictionary<byte, ushort> { [0x00] = 0x10 });
    public static readonly Phase P2 = new(Instance, "P2", 79, 20292,
        arenaMapEffectOverrides: new Dictionary<byte, ushort> { [0x00] = 0x40 });
    public static readonly Phase P3 = new(Instance, "P3", 174, 20293, arenaMapEffectOverrides: P3ArenaMapEffectOverrides);
    public static readonly Phase P4 = new(Instance, "P4", 174, 20293, arenaMapEffectOverrides: P3ArenaMapEffectOverrides);
    public static readonly Phase P5 = new(Instance, "P5", 175, 20294,
        init: world =>
        {
            world.Map.DirectorUpdate(0x80000028U, 0x0U, 0x4U, 0x6U);
            world.Map.DirectorUpdate(0x8000000DU, 0x2U);
        },
        arenaMapEffectOverrides: new Dictionary<byte, ushort>
        {
            [0x00] = 0x800, [0x13] = 4,
            [0x14] = 1, [0x15] = 1, [0x16] = 1, [0x17] = 1, [0x18] = 1, [0x19] = 1, [0x1A] = 1,
            [0x1B] = 1, [0x1C] = 1, [0x1D] = 1, [0x1E] = 1, [0x1F] = 1, [0x20] = 1, [0x21] = 1,
        });

    public string Name => "Dancing Mad";
    public uint TerritoryId => 1363;
    public Vector3 Origin => new(100f, 0f, 100f);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets => Waymarks;

    public EntranceBarrier EntranceBarrier => new(2000182, 0xBC174E, 0xBC174C, new Vector3(0.41f, 0f, 15.75f));

    public void Run(SimWorld world)
    {
        world.Map.DirectorUpdate(0x80000028U, 0x4U);
        world.Map.DirectorUpdate(0x8000000DU, 0x1U);
    }

    public void RunClientSetup(SimWorld world) => UmadReplayData.Seed();

    private static readonly IReadOnlyList<WaymarkLayout> Waymarks =
    [
        new WaymarkLayout("Diamond Waymarks", [
            new(WaymarkSlot.A,     new Vector3(  0, 0, -12)),
            new(WaymarkSlot.B,     new Vector3( 12, 0,   0)),
            new(WaymarkSlot.C,     new Vector3(  0, 0,  12)),
            new(WaymarkSlot.D,     new Vector3(-12, 0,   0)),
            new(WaymarkSlot.One,   new Vector3( -6, 0,  -6)),
            new(WaymarkSlot.Two,   new Vector3(  6, 0,  -6)),
            new(WaymarkSlot.Three, new Vector3(  6, 0,   6)),
            new(WaymarkSlot.Four,  new Vector3( -6, 0,   6)),
        ]),
        new WaymarkLayout("DN Zenith Waymarks",
        [
            new(WaymarkSlot.A,     new Vector3(    0, 0,   -12)),
            new(WaymarkSlot.B,     new Vector3(   12, 0,     0)),
            new(WaymarkSlot.C,     new Vector3(    0, 0,    12)),
            new(WaymarkSlot.D,     new Vector3(  -12, 0,     0)),
            new(WaymarkSlot.One,   new Vector3(-8.765f, 0, -8.765f)),
            new(WaymarkSlot.Two,   new Vector3( 8.628f, 0, -8.765f)),
            new(WaymarkSlot.Three, new Vector3( 8.628f, 0,  8.628f)),
            new(WaymarkSlot.Four,  new Vector3(-8.765f, 0,  8.628f)),
        ]),
        // A/B/C/D on cardinals 12y out, 1-4 on the corners (±12, ±12).
        new WaymarkLayout("[LPDU] Big Box",
        [
            new(WaymarkSlot.A,     new Vector3(  0, 0, -12)),
            new(WaymarkSlot.B,     new Vector3( 12, 0,   0)),
            new(WaymarkSlot.C,     new Vector3(  0, 0,  12)),
            new(WaymarkSlot.D,     new Vector3(-12, 0,   0)),
            new(WaymarkSlot.One,   new Vector3(-12, 0, -12)),
            new(WaymarkSlot.Two,   new Vector3( 12, 0, -12)),
            new(WaymarkSlot.Three, new Vector3( 12, 0,  12)),
            new(WaymarkSlot.Four,  new Vector3(-12, 0,  12)),
        ]),
    ];
}
