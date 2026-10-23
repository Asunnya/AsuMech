using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Map;

namespace AnoMech.Scenarios.Dsr;

// The territory's BGM is a null track; P1 plays InstanceContent 30106's and the director switches it each phase.
public sealed class DsrZone : IZone
{
    private const ushort BgmId = 920;
    private const ushort ThordanBgm = 921;
    private const ushort NidhoggBgm = 922;
    private const ushort EyesBgm = 923;
    private const ushort DarkThordanBgm = 925;
    private const ushort DoubleDragonsBgm = 926;
    private const byte FairSkies = 2;
    public const byte Oppression = 45;
    private const byte DarkThordanWeather = 46;
    private const byte Clouds = 3;

    public static readonly DsrZone Instance = new();
    public static readonly Phase Knights = new(Instance, "P1", FairSkies, BgmId);
    public static readonly Phase Thordan = new(Instance, "P2", Oppression, ThordanBgm);
    public static readonly Phase Nidhogg = new(Instance, "P3", Clouds, NidhoggBgm);
    // UNVERIFIED: the eyes' weather; P3's is kept.
    public static readonly Phase Eyes = new(Instance, "P4", Clouds, EyesBgm);
    public static readonly Phase DarkThordan = new(Instance, "P5", DarkThordanWeather, DarkThordanBgm);
    public static readonly Phase DoubleDragons = new(Instance, "P6", Clouds, DoubleDragonsBgm);

    public string Name => "Dragonsong's Reprise";
    public uint TerritoryId => 968;
    public Vector3 Origin => new(100f, 0f, 100f);
    public DutyInitData DutyInitData => DutyInitData.Unknown(Origin);

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("DSR Phase 1", DsrConstants.Phase1Waymarks), new WaymarkLayout("NAUR", DsrConstants.NaurWaymarks)];
}
