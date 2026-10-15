using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru;

public sealed class FruZone : IZone
{
    public static readonly FruZone Instance = new();
    public static readonly Phase P2 = new(Instance, "P2", WeatherId.Usurper, BgmId.Usurper,
        init: world => ShowOnly(world, MapEffect.P2Platform));
    public static readonly Phase P3 = new(Instance, "P3", WeatherId.Oracle, BgmId.Oracle,
        init: world => ShowOnly(world, MapEffect.OracleStage));
    public static readonly Phase P4 = new(Instance, "P4", WeatherId.Oracle, BgmId.OracleAndUsurper,
        init: world => ShowOnly(world, MapEffect.OracleStage, MapEffect.OracleStageType2));
    public static readonly Phase P5 = new(Instance, "P5", WeatherId.Pandora, BgmId.Pandora,
        init: world => ShowOnly(world, MapEffect.PandoraArena));

    public string Name => "Futures Rewritten";
    public uint TerritoryId => 1238;
    public Vector3 Origin => new(100f, 0f, 100f);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("Inner Circle", InnerCircleWaymarks)];

    // TODO: EObj 2007457 is the barrier of TOP and FRU alike; its EObj and wall layout ids and the
    // spawn point for FRU have to be read from a replay's eobj_spawn.
    public EntranceBarrier EntranceBarrier => new(2007457, 0, 0, new Vector3(0f, 0f, 15f));

    public void Run(SimWorld world) => world.EnforceArenaBoundary(Geometry.ArenaRadius);

    // TODO: replace with IZone.ArenaMapEffectSnapshot and per-phase overrides read from a replay's
    // MapEffect snapshot. Until then every slot is set by hand, once the zone's SharedGroups have
    // loaded, which also undoes what the previous run left on the arena.
    private static void ShowOnly(SimWorld world, byte slot, uint show = MapEffect.Show)
        => world.Events.Add(1f, () =>
        {
            for (byte i = 0; i < MapEffect.SlotCount; i++)
                world.Map.AddEffect(i == slot ? show : MapEffect.Hide, i);
        });
}
