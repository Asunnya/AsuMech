using System;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;

namespace AsuMech.Scenarios.Fru;

// P4's actors as retail spawns them, for each scenario P4 runs through.
public static class FruP4Actors
{
    // South of centre, facing north.
    public static readonly Placement UsurperSpawn = new(new Vector3(0, 0, 10), MathF.PI);
    public static readonly Vector3 FragmentPosition = new(0, 0, -16);

    public static EnemySpawnConfig Usurper(bool shown) => new(
        BNpcBaseId: BNpcBaseId.UsurperP4,
        NameId: BNpcNameId.Usurper,
        Level: Level,
        Targetable: shown,
        EnemyList: EnemyListMode.Always,
        Visibility: shown ? SpawnVisibility.Visible : SpawnVisibility.HiddenUntilShown,
        Placement: UsurperSpawn,
        InitialModeAttributeFlags: 0xFC,
        LayoutId: LayoutId.UsurperP4);

    public static EnemySpawnConfig Fragment() => new(
        BNpcBaseId: BNpcBaseId.FragmentOfFate,
        NameId: BNpcNameId.FragmentOfFate,
        Level: Level,
        Targetable: true,
        EnemyList: EnemyListMode.Never,
        Placement: new Placement(FragmentPosition, 0f),
        DisplayFlags: 0x40003,
        LayoutId: LayoutId.FragmentOfFate,
        IsHostile: false,
        Statuses: [new(StatusId.Incurable)]);

    public static EnemySpawnConfig VisionOfGaia(bool shown) => Vision(BNpcBaseId.VisionOfGaia, BNpcNameId.Gaia, Appearance.VisionOfGaia,
        new Placement(new Vector3(-0.71f, 0, -16), 1.22f), 0x4020B, LayoutId.VisionOfGaia, 0x79, shown);

    public static EnemySpawnConfig VisionOfRyne(bool shown) => Vision(BNpcBaseId.VisionOfRyne, BNpcNameId.Ryne, Appearance.VisionOfRyne,
        new Placement(new Vector3(0.71f, 0, -16), -0.84f), 0x4020A, LayoutId.VisionOfRyne, 0x07, shown);

    // Retail spawns them with the hidden-until-shown bit; shown, they spawn as they stand after.
    private static EnemySpawnConfig Vision(uint bnpcBaseId, uint nameId, NpcAppearance appearance, Placement placement, uint shownDisplayFlags,
        uint layoutId, byte modelState, bool shown) => new(
        BNpcBaseId: bnpcBaseId,
        NameId: nameId,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Visibility: shown ? SpawnVisibility.Visible : SpawnVisibility.HiddenUntilShown,
        Placement: placement,
        DisplayFlags: shown ? shownDisplayFlags : shownDisplayFlags | 0x20000,
        LayoutId: layoutId,
        IsHostile: false,
        Appearance: appearance,
        Statuses: [new(StatusId.Levitation, 200)],
        ModelState: modelState);
}
