using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;

namespace AnoMech.Scenarios.M11s;

// Single phase; the fight's music is InstanceContent 30159's.
public sealed class M11sZone : IZone
{
    private const ushort BgmId = 20240;

    public static readonly M11sZone Instance = new();
    public static readonly Phase Fight = new(Instance, "", null, BgmId);

    public string Name => "AAC Heavyweight M3 (Savage)";
    public string Category => "Savage";
    public string? Expansion => "Dawntrail";
    public uint TerritoryId => 1325;
    public Vector3 Origin => new(100f, 0f, 100f);
    public DutyInitData DutyInitData => DutyInitData.Unknown(Origin);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("Toxic Friends", M11sConstants.ToxicWaymarks)];
}
