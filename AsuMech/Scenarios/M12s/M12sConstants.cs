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
        public const float P2ArenaRadius = 20f;
    }

    public static class BNpcBaseId
    {
        public const uint Helper = 9020;
        public const uint LindwurmP1 = 19195;
        public const uint SnakeHead = 19196;
        public const uint PurpleOrb = 19200;
        public const uint GreenOrb = 19201;
        public const uint LindwurmP2 = 19202;
        public const uint Lindschrat = 19204;
        public const uint BloodVessel = 19199;
        public const uint Understudy = 19210;
        public const uint BlackHole = 19205;
        public const uint WaterSphere = 19206;
        public const uint AeroSphere = 19207;
        public const uint ThunderSphere = 19208;
        public const uint FireSphere = 19209;
        public const uint TemporalTear = 19211;
    }

    public static class EObjId
    {
        public const uint BloodVessel = 0x1EBF29;
    }

    public static class BNpcNameId
    {
        public const uint LindwurmP1 = 14378;
        public const uint LindwurmP2 = 14379;
        public const uint Lindschrat = 14380;
        public const uint BloodVessel = 14381;
        public const uint ManaSphere = 14382;
        public const uint Understudy = 14383;
        public const uint TemporalTear = 14384;
    }

    public static class ActionId
    {
        public const uint TheFixer = 0xB4D7;
        public const uint MortalSlayer = 0xB495;
        public const uint MortalSlayerGreen = 0xB496;
        public const uint ArmySlayerGreen = 0xB497;
        public const uint MortalSlayerPurple = 0xB498;
        public const uint ArmySlayerPurple = 0xB499;
        public const uint GrotesquerieAct1 = 0xBEBD;
        public const uint RavenousReach = 0xB469;
        public const uint RavenousReachFromWest = 0xB49A;
        public const uint RavenousReachFromEast = 0xB49B;
        public const uint RavenousReachHeadStrike = 0xB769;
        public const uint RavenousReachCone = 0xB49D;
        public const uint PhagocyteSpotlight = 0xB49E;
        public const uint IdyllicManaBurst = 0xB518;
        public const uint IdyllicHeavySlam = 0xB519;
        public const uint IdyllicUnmitigatedExplosion = 0xB4F5;
        public const uint Act2PhagocyteSpotlight = 0xB4B6;
        public const uint Act2Splattershed = 0xB9C4;
        public const uint CloneHemorrhagicProjection = 0xB4EB;
        public const uint MutationDramaticLysis = 0xB507;
        public const uint MutationUnmitigatedExplosion = 0xB508;
        public const uint DramaticLysis = 0xB4AA;
        public const uint FourthWallFusion = 0xB4AE;
        public const uint HemorrhagicProjection = 0xB4AF;
        public const uint Burst = 0xB49F;
        public const uint VisceralBurst = 0xB4D6;
        public const uint Act1FourthWallFusion = 0xB9B9;
        public const uint CellShedding = 0xB4AC;
        public const uint CellDeath = 0xB4AD;
        public const uint ChainBreak = 0xB4B4;
        public const uint ChainHeld = 0xB4B5;
        public const uint GrotesquerieCurtainCall = 0xBEC0;
        public const uint Splattershed = 0xB9C3;
        public const uint SplattershedRaidwide = 0xB9C6;
        public const uint SlaughtershedArms = 0xB4C3;
        public const uint SlaughtershedHeads = 0xB4C6;
        public const uint SlaughtershedRaidwide = 0xADC9;
        public const uint SerpentineScourgeWestEast = 0xB4CB;
        public const uint RaptorKnucklesWestEast = 0xB4CC;
        public const uint SerpentineScourgeEastWest = 0xB4CD;
        public const uint RaptorKnucklesEastWest = 0xB4CE;
        public const uint RaptorKnucklesWest = 0xB4CF;
        public const uint RaptorKnucklesEast = 0xB4D0;
        public const uint SerpentineScourgeWest = 0xB4D1;
        public const uint SerpentineScourgeEast = 0xB4D2;
        public const uint SerpentineScourgeRect = 0xB9BC;
        public const uint RaptorKnucklesKnockback = 0xB9C7;
        public const uint CurtainCallDramaticLysis = 0xB4D4;
        public const uint CurtainCallFourthWallFusion = 0xB4D5;
        public const uint RefreshingOverkill = 0xB538;
        public const uint RefreshingOverkillRaidwide = 0xB539;
        public const uint Replication = 0xB4D8;
        public const uint Dash = 0xB4D9;
        public const uint WingedScourgeVertical = 0xB4DA;
        public const uint WingedScourgeHorizontal = 0xB4DB;
        public const uint WingedScourge = 0xB4DC;
        public const uint TopTierSlamCast = 0xB4DD;
        public const uint TopTierSlam = 0xB4DE;
        public const uint MightyMagicCast = 0xB4DF;
        public const uint MightyMagic = 0xB4E0;
        public const uint DoubleSobat = 0xB520;
        // B521..B524: the same kick, each turning the follow-up by another quarter.
        public const uint DoubleSobatBuster = 0xB521;
        public const uint DoubleSobatRepeat = 0xB525;
        public const uint EsotericFinisher = 0xB526;
        public const uint SnakingKick = 0xB527;
        public const uint ArcadiaAflame = 0xB528;
        public const uint GrotesquerieAct2 = 0xBEBE;
        public const uint GrotesquerieAct3 = 0xBEBF;
        // Cruel Coil's four casts start the boss facing east, west, north and south.
        public const uint CruelCoilEast = 0xB4B8;
        public const uint CruelCoilBind = 0xB472;
        public const uint SkinsplitterBoss = 0xB4BC;
        public const uint SkinsplitterDonut = 0xB53E;
        public const uint DramaticLysisChainBreak = 0xB4B4;
        public const uint DramaticLysisChainFail = 0xB4B5;
        public const uint RoilingMassChain = 0xB4B3;
        public const uint RoilingMassBlob = 0xB4B7;
        public const uint UnmitigatedExplosion = 0xB4B2;
        // Same order as Cruel Coil's.
        public const uint ConstrictorEast = 0xB4BD;
        public const uint ConstrictorKill = 0xB4C2;
        public const uint FeralFission = 0xBE09;
        public const uint GrandEntranceAppear1 = 0xB4A0;
        public const uint GrandEntranceAppear2 = 0xB4A1;
        public const uint GrandEntranceAppear3 = 0xB4A2;
        public const uint GrandEntranceDisappear = 0xB4A3;
        public const uint BringDownTheHouseBoss = 0xBE0A;
        public const uint BringDownTheHouseLarge = 0xB4A4;
        public const uint BringDownTheHouseMedium = 0xB4A5;
        public const uint BringDownTheHouseSmall = 0xB4A6;
        public const uint MitoticDramaticLysis = 0xB4B0;
        public const uint MetamitosisProjection = 0xB4B1;
        public const uint MetamitosisTower = 0xB923;
        public const uint SplitScourgeHead = 0xB4A7;
        public const uint SplitScourge = 0xB4AB;
        public const uint VenomousScourge = 0xB4A8;
        public const uint Staging = 0xB4E1;
        public const uint FirefallSplashCast = 0xB4E3;
        public const uint FirefallSplashJump = 0xB4E4;
        public const uint ScaldingWaves = 0xB4E5;
        public const uint ManaBurstVisual = 0xB4E6;
        public const uint ManaBurst = 0xB4E7;
        public const uint HeavySlam = 0xB4E8;
        public const uint GrotesquerieVisual = 0xB4E9;
        public const uint Grotesquerie = 0xB4EA;
        public const uint Reenactment = 0xB4EC;
        public const uint FirefallSplashReplay = 0xB4ED;
        public const uint ManaBurstReplayVisual = 0xB4EE;
        public const uint HeavySlamReplayVisual = 0xB4EF;
        public const uint UnmitigatedImpact = 0xB4F0;
        public const uint GrotesquerieReplayVisual = 0xB4F1;
        public const uint ScaldingWavesReplay = 0xB8E1;
        public const uint HemorrhagicProjectionReplay = 0xB922;
        public const uint ManaBurstReplay = 0xBBE3;
        public const uint HeavySlamReplay = 0xBE5D;
        public const uint NetherwrathNear = 0xB52E;
        public const uint NetherwrathFar = 0xB52F;
        public const uint TimelessSpite = 0xB530;
        public const uint BloodMana = 0xB4FB;
        public const uint BlackHoleActivate = 0xB4FC;
        public const uint ManaSphereSpawn = 0xB4FD;
        public const uint BloodyBurst = 0xB4FE;
        public const uint BlackHoleAbsorb = 0xB4FF;
        public const uint BloodWakening = 0xB500;
        public const uint LindwurmsWaterIII = 0xB501;
        public const uint LindwurmsAeroIII = 0xB502;
        public const uint StraightforwardThunderII = 0xB503;
        public const uint SidewaysFireII = 0xB504;
        public const uint MutatingCells = 0xB505;
        public const uint MutatingCellsVisual = 0xB506;
        public const uint NetherworldNear = 0xB52B;
        public const uint NetherworldFar = 0xB52C;
        public const uint WailingWave = 0xB52D;
        public const uint BlackHolePulse = 0xBCB0;
        public const uint P2Attack = 0xB51F;
        public const uint IdyllicDream = 0xB509;
        public const uint TwistedVision = 0xBBE2;
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
        public const uint CosmicKiss = 0xB4F4;
        public const uint LindwurmsDarkII = 0xB4F6;
        public const uint LindwurmsStoneIII = 0xB4F7;
        public const uint LindwurmsGlare = 0xB4F8;
        public const uint PyreticWurm = 0xB4F9;
        public const uint LindwurmsThunderII = 0xB4FA;
        public const uint TemporalCurtain = 0xB51C;
        public const uint IntoTheTear = 0xB51D;
        public const uint CloneJump = 0xB4D9;
        public const uint SnakingKickPlatform = 0xBCAF;
        public const uint ReenactedHeavySlamMarker = 0xB4EF;
        public const uint ReenactedManaBurstMarker = 0xB4EE;
        public const uint ReenactedManaBurst = 0xBBE3;
        public const uint ReenactedHeavySlam = 0xBE5D;
        public const uint OutOfTheTear = 0xB51E;
        public const uint PowerGusherFromTheTear = 0xB51A;
        public const uint DoubleSobatFirst = 0xB521;
        public const uint DoubleSobatSecond = 0xB525;
        public const uint ReplicationFinale = 0xB46C;
        public const uint ArcadianHell = 0xB533;
        public const uint ArcadianHellFour = 0xB534;
        public const uint ArcadianHellEight = 0xB535;
    }

    public static class StatusId
    {
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort DamageDown = 0xB5F;
        public const ushort PoisonResistanceDownII = 0xF5F;
        public const ushort BurstingGrotesquerie = 0x1299;
        public const ushort SharedGrotesquerie = 0x129A;
        public const ushort DirectedGrotesquerie = 0x1370;
        // Its param (0x40C-0x40F) draws the arrow over the head.
        public const ushort DirectedArrow = 0xDE6;
        public const ushort BondsOfFleshAlpha = 0x1290;
        public const ushort UnbreakableFleshAlpha = 0x1291;
        public const ushort RottingFlesh = 0x129B;
        public const ushort FireResistanceDownII = 0xB79;
        public const ushort DarkResistanceDownII = 0xCFB;
        public const ushort Bind = 2518;
        public const ushort FirstInLine = 3004;
        public const ushort SecondInLine = 3005;
        public const ushort ThirdInLine = 3006;
        public const ushort FourthInLine = 3451;
        public const ushort BondsOfFleshBeta = 4754;
        public const ushort UnbreakableFleshBeta = 4755;
        public const ushort MitoticPhase = 4764;
        public const ushort FateOfTheWurm = 4772;
        public const ushort MutationAlpha = 4769;
        public const ushort MutatingCells = 4770;
        public const ushort MutationBeta = 4771;
        public const ushort LightResistanceDownII = 0x1044;
        public const ushort Doom = 0xD24;
        public const ushort HotBlooded = 0x12A0;
        public const ushort LindwurmsPortent = 0x129D;
        public const ushort FarawayPortent = 0x129E;
        public const ushort NearbyPortent = 0x129F;
    }

    public static class LockonId
    {
        public const uint Stack = 0xA1;
        public const uint TankBuster = 0x158;
        public const uint CellChain = 0x291;
        public const uint SlaughtershedStack = 0x13D;
        public const uint SlaughtershedSpread = 0x177;
        public const uint DoubleSobat = 0x256;
        public const uint CellChainSpawn = 657;
    }

    public static class TetherId
    {
        public const ushort CellChain = 0x16E;
        public const ushort Cell = 366;
        public const ushort CloneCone = 0x16F;
        public const ushort CloneDefamation = 0x170;
        public const ushort CloneStack = 0x171;
        public const ushort Locked = 0x175;
        public const ushort BossJump = 0x176;
        public const ushort ManaBurst = 0x170;
        public const ushort HeavySlam = 0x171;
        public const ushort Bound = 0x175;
    }

    // The boss's model states (ActorControl 0x3F) while a mechanic is up.
    public static class ModelState
    {
        public const byte Idle = 0;
        public const byte CurtainCall = 4;
        public const byte Slaughtershed = 7;
        public const byte MortalSlayer = 0x1F;
        public const byte SnakeHeadOut = 5;
        public const byte Coiled = 0x1C;
        public const byte Burrowed = 0x06;
        public const byte Normal = 0x00;
    }

    // DirectedGrotesquerie's param: the way the growth points.
    public static class GrowthParam
    {
        public const int North = 0x436;
        public const int East = 0x437;
        public const int South = 0x438;
        public const int West = 0x439;
    }

    public static class ActionTimeline
    {
        public const ushort BloodVesselAppears = 0x2488;
        public const ushort SnakeHeadRises = 0x11D3;
    }

    public static class MapEffect
    {
        public const byte ArenaSlot = 0x00;
        public const uint SnakeShows = 0x00010001;
        public const uint SnakeGone = 0x00040004;
        public const uint CardinalPlatforms = 0x00020001;
        public const uint CardinalHoles = 0x00200010;
        public const uint IntercardinalPlatforms = 0x02000100;
        public const uint IntercardinalHoles = 0x08000400;
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

    public static class BattleTalk
    {
        public const uint Speaker = 0x330A;
        public const uint NoActor = 0xE0000000;
    }
}
