using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;

namespace AnoMech.Scenarios.M10s;

// Single phase; the territory's BGM is a null track, so the fight's music is InstanceContent 30157's.
public sealed class M10sZone : IZone
{
    private const ushort BgmId = 20239;

    public static readonly M10sZone Instance = new();
    public static readonly Phase Fight = new(Instance, "", null, BgmId);

    public string Name => "AAC Heavyweight M2 (Savage)";
    public string Category => "Savage";
    public string? Expansion => "Dawntrail";
    public uint TerritoryId => 1323;
    public Vector3 Origin => new(100f, 0f, 100f);
    public DutyInitData DutyInitData => DutyInitData.Unknown(Origin);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("Toxic Friends", M10sConstants.ToxicWaymarks)];
}
