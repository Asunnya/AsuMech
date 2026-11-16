using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s;

public static class M12sConstants
{
    public const byte Level = 100;
    public const uint DirectorEventId = 0x800375D1;

    public static IReadOnlyList<Waymark> ToxicWaymarks =>
    [
        new(WaymarkSlot.A,     new Vector3( -0.016f, 0f, -12.409f)),
        new(WaymarkSlot.B,     new Vector3( 12.405f, 0f,  -0.086f)),
        new(WaymarkSlot.C,     new Vector3(  0.012f, 0f,  12.338f)),
        new(WaymarkSlot.D,     new Vector3(-12.251f, 0f,   0.017f)),
        new(WaymarkSlot.One,   new Vector3(-15.901f, 0f, -12.411f)),
        new(WaymarkSlot.Two,   new Vector3( 15.843f, 0f, -12.456f)),
        new(WaymarkSlot.Three, new Vector3( 15.92f,  0f,  12.349f)),
        new(WaymarkSlot.Four,  new Vector3(-15.921f, 0f,  12.366f)),
    ];

    public static IReadOnlyList<Waymark> DnP2Waymarks =>
    [
        new(WaymarkSlot.A,     new Vector3( -0.138f, 0f, -13.591f)),
        new(WaymarkSlot.B,     new Vector3( 13.711f, 0f,  -0.015f)),
        new(WaymarkSlot.C,     new Vector3(  0.044f, 0f,  13.551f)),
        new(WaymarkSlot.D,     new Vector3(-13.629f, 0f,   0.034f)),
        new(WaymarkSlot.One,   new Vector3(  0.096f, 0f,  -8.627f)),
        new(WaymarkSlot.Two,   new Vector3(  8.282f, 0f,   0.024f)),
        new(WaymarkSlot.Three, new Vector3(  0.003f, 0f,   8.251f)),
        new(WaymarkSlot.Four,  new Vector3( -8.221f, 0f,   0.053f)),
    ];

    // Each light party's healer sits at index 1.
    public static readonly PartyRole[][] LightParties =
    [
        [PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.PhysRangedDps],
        [PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.MeleeDpsB, PartyRole.CasterDps],
    ];

    public static class Geometry
    {
        public const float P1HalfWidth = 20f;
        public const float P1HalfDepth = 15f;
    }

    public static class BNpcBaseId
    {
        public const uint Helper = 9020;
        public const uint LindwurmP2 = 19202;
        public const uint Lindschrat = 19204;
        public const uint Understudy = 19210;
        public const uint TemporalTear = 19211;
    }

    public static class BNpcNameId
    {
        public const uint LindwurmP1 = 14378;
        public const uint LindwurmP2 = 14379;
        public const uint Lindschrat = 14380;
        public const uint Understudy = 14383;
        public const uint TemporalTear = 14384;
    }

    public static class ActionId
    {
        public const uint P2Attack = 0xB51F;
        public const uint IdyllicDream = 0xB509;
        public const uint Staging = 0xB4E1;
        public const uint TwistedVision = 0xBBE2;
        public const uint Replication = 0xB4D8;
        public const uint PowerGusherNorthClone = 0xB50F;
        public const uint PowerGusherSouthClone = 0xB510;
        public const uint SnakingKickRehearsal = 0xB511;
        public const uint PowerGusherRehearsal = 0xB512;
        public const uint PowerGusherNorthReplay = 0xB513;
        public const uint PowerGusherSouthReplay = 0xB514;
        public const uint SnakingKickReplayVisual = 0xB515;
        public const uint SnakingKickReplay = 0xBE95;
        public const uint PowerGusherReplay = 0xB516;
        public const uint LindwurmsMeteor = 0xB4F2;
        public const uint Downfall = 0xB4F3;
        public const uint ArcadianArcanum = 0xB529;
        public const uint ArcadianArcanumSpread = 0xB9D9;
        public const uint ManaBurstMarker = 0xB517;
        public const uint ManaBurst = 0xB518;
        public const uint HeavySlam = 0xB519;
        public const uint CosmicKiss = 0xB4F4;
        public const uint UnmitigatedExplosion = 0xB4F5;
        public const uint LindwurmsDarkII = 0xB4F6;
        public const uint LindwurmsStoneIII = 0xB4F7;
        public const uint LindwurmsGlare = 0xB4F8;
        public const uint PyreticWurm = 0xB4F9;
        public const uint LindwurmsThunderII = 0xB4FA;
        public const uint TemporalCurtain = 0xB51C;
        public const uint IntoTheTear = 0xB51D;
        public const uint CloneJump = 0xB4D9;
        public const uint SnakingKickPlatform = 0xBCAF;
        public const uint Reenactment = 0xB4EC;
        public const uint ReenactedHeavySlamMarker = 0xB4EF;
        public const uint ReenactedManaBurstMarker = 0xB4EE;
        public const uint ReenactedManaBurst = 0xBBE3;
        public const uint ReenactedHeavySlam = 0xBE5D;
        public const uint OutOfTheTear = 0xB51E;
        public const uint PowerGusherFromTheTear = 0xB51A;
        public const uint DoubleSobat = 0xB520;
        public const uint DoubleSobatFirst = 0xB521;
        public const uint DoubleSobatSecond = 0xB525;
        public const uint EsotericFinisher = 0xB526;
        public const uint ReplicationFinale = 0xB46C;
        public const uint ArcadianHell = 0xB533;
        public const uint ArcadianHellFour = 0xB534;
        public const uint ArcadianHellEight = 0xB535;
    }

    public static class StatusId
    {
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort LightResistanceDownII = 0x1044;
        public const ushort Doom = 0xD24;
        public const ushort HotBlooded = 0x12A0;
        public const ushort LindwurmsPortent = 0x129D;
        public const ushort FarawayPortent = 0x129E;
        public const ushort NearbyPortent = 0x129F;
    }

    public static class TetherId
    {
        public const ushort ManaBurst = 0x170;
        public const ushort HeavySlam = 0x171;
        public const ushort Bound = 0x175;
    }

    public static class LockonId
    {
        public const uint DoubleSobat = 0x256;
    }

    // Slot 0x01 is the Idyllic Dream backdrop, 0x20/0x21 the floor, 0x22-0x29 the eight towers.
    public static class MapEffect
    {
        public const byte DreamSlot = 0x01;
        public const byte MiddleFloorSlot = 0x20;
        public const byte FloorSlot = 0x21;
        public const byte FirstTowerSlot = 0x22;
        public const uint DreamBegins = 0x00080010;
        public const uint VisionStarts = 0x00200040;
        public const uint VisionShows = 0x00800100;
        public const uint VisionEnds = 0x02000400;
        public const uint VisionCleared = 0x08000010;
        public const uint MeteorFalls = 0x00200010;
        public const uint FloorSplits = 0x00800040;
        public const uint FloorSplitsAgain = 0x02000040;
        public const uint FloorRestored = 0x01000001;
        public const uint MiddleShows = 0x00020001;
        public const uint MiddleGone = 0x00080004;
        public const uint TowerShows = 0x00020001;
        public const uint TowerLit = 0x00200010;
        public const uint TowerOff = 0x00080004;
    }
}
