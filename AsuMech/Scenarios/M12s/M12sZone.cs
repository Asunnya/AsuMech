using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Map;

namespace AsuMech.Scenarios.M12s;

// UNVERIFIED: the fight's music and weather; both phases keep the inn's for now.
public sealed class M12sZone : IZone
{
    public static readonly M12sZone Instance = new();
    public static readonly Phase P1 = new(Instance, "Phase 1", null, 0);
    public static readonly Phase P2 = new(Instance, "Phase 2", null, 0);

    public string Name => "AAC Heavyweight M4 (Savage)";

    public string ShortName => "M12S";

    public string? Tier => "Heavyweight";
    public string Category => "Savage";
    public string? Expansion => "Dawntrail";
    public uint TerritoryId => 1327;
    public Vector3 Origin => new(100f, 0f, 100f);
    // The default entrance (16y south) falls outside P1's 40x30 floor.
    public DutyInitData DutyInitData => new(new EntranceBarrier(0, 0, 0, Origin + new Vector3(0f, 0f, 12f), MathF.PI), default, [], [], []);
    public IArenaShape Arena => new RectArena(M12sConstants.Geometry.P1HalfWidth, M12sConstants.Geometry.P1HalfDepth);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
    [
        new WaymarkLayout("Toxic Friends", M12sConstants.ToxicWaymarks),
        new WaymarkLayout("M12 P2", M12sConstants.DnP2Waymarks),
    ];
}
