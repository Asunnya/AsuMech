using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Scenarios.Fru;

public static class FruConstants
{
    public const byte Level = 100;

    public static IReadOnlyList<Waymark> InnerCircleWaymarks =>
    [
        new(WaymarkSlot.A,     new Vector3(     0f, 0f,   -10f)),
        new(WaymarkSlot.B,     new Vector3(    10f, 0f,      0f)),
        new(WaymarkSlot.C,     new Vector3(     0f, 0f,    10f)),
        new(WaymarkSlot.D,     new Vector3(   -10f, 0f,      0f)),
        new(WaymarkSlot.One,   new Vector3( -7.07f, 0f, -7.07f)),
        new(WaymarkSlot.Two,   new Vector3(  7.07f, 0f, -7.07f)),
        new(WaymarkSlot.Three, new Vector3(  7.07f, 0f,  7.07f)),
        new(WaymarkSlot.Four,  new Vector3( -7.07f, 0f,  7.07f)),
    ];

    public static class Geometry
    {
        public const float ArenaRadius = 20f;
    }

    public static class WeatherId
    {
        public const byte Usurper = 105;
        public const byte Oracle = 106;
        // n4gw's combat background uses bg6_1; weather 1 enables the victory scene.
        public const byte Pandora = 108;
    }

    public static class BgmId
    {
        public const ushort Usurper = 759;
        public const ushort Oracle = 802;
        public const ushort OracleAndUsurper = 801;
        public const ushort Pandora = 20099;
    }

    // ContentDirectorManagedSG 181. Packet flags: low16 = state, high16 = timeline mask.
    public static class MapEffect
    {
        public const byte SlotCount = 54;
        public const uint Show = 0x00010001;
        public const uint Hide = 0x00040004;

        public const byte P2Platform = 23;
        // The P3/P4 stage controller; Show is the P3 platform, StageType2 the P4 one.
        public const byte OracleStage = 40;
        public const uint OracleStageType2 = 0x00020001;
        public const byte PandoraArena = 47;

        // Mode 1 with action flags 0x20/0x40: stage2_ice_a / stage2_nomal_a.
        public const uint FreezeFloor = 0x00010020;
        public const uint ThawFloor = 0x00010040;

        public const byte LightRampantFirstTower = 9;
        public const byte LightRampantCenterTower = 21;
        // Indexed by clock from true north; the scenery's model sits 10.5y across its origin.
        public const byte RelativityFirstHourglass = 26;
        public const byte CrystallizeFirstHourglass = 34;
        public const byte DarklitFirstTower = 42;
        public const byte FragmentOfFate = 46;
        // 51 = NW, 52 = NE, 53 = S.
        public const byte ParadiseFirstTower = 51;
        public const uint ParadiseTowerShow = 0x00020001;
    }

    public static class BNpcBaseId
    {
        public const uint Helper = 9020;
        public const uint UsurperP2 = 17823;
        public const uint FrozenMirror = 17824;
        public const uint HolyLight = 17826;
        public const uint OracleP3 = 17831;
        public const uint SorrowsHourglassP3 = 17832;
        public const uint UsurperP4 = 17833;
        public const uint OracleP4 = 17835;
        public const uint DrachenWanderer = 17836;
        public const uint SorrowsHourglassP4 = 17837;
        public const uint Pandora = 17839;
        public const uint FragmentOfFate = 17841;
        public const uint VisionOfRyne = 17844;
        public const uint VisionOfGaia = 17845;
    }

    public static class BNpcNameId
    {
        public const uint Oracle = 9832;
        public const uint SorrowsHourglass = 9823;
        public const uint DrachenWanderer = 9323;
        public const uint Ryne = 9829;
        public const uint Gaia = 9830;
        public const uint Usurper = 12809;
        public const uint FrozenMirror = 13554;
        public const uint FragmentOfFate = 13559;
        public const uint Pandora = 13561;
    }

    public static class ModelCharaId
    {
        public const uint UsurperIce = 4374;
        public const uint UsurperLight = 4375;
        public const uint UsurperDragon = 4376;
    }

    // The Usurper's Redress into her dragon armor.
    public const uint UsurperDragonBNpcState = 45;

    public static class ActionId
    {
        // -- P2 Diamond Dust --
        public const uint MirrorImage = 40180;
        public const uint DiamondDust = 40197;
        public const uint IcicleImpact = 40198;
        public const uint FrigidStone = 40199;
        public const uint FrigidNeedleCircle = 40200;
        public const uint FrigidNeedleCross = 40201;
        public const uint AxeKick = 40202;
        public const uint ScytheKick = 40203;
        public const uint HouseOfLightDiamondDust = 40206;
        public const uint HeavenlyStrike = 40207;
        public const uint SinboundHolyCast = 40208;
        public const uint SinboundHoly = 40209;
        public const uint ShiningArmor = 40185;
        public const uint FrostArmor = 40184;
        public const uint TwinStillnessFirst = 40193;
        public const uint TwinSilenceFirst = 40194;
        public const uint TwinSilenceSecond = 40195;
        public const uint TwinStillnessSecond = 40196;

        // -- P2 Light Rampant --
        public const uint LightRampant = 40212;
        public const uint BrightHunger = 40213;
        public const uint InescapableIllumination = 40214;
        public const uint RefulgentFate = 40215;
        public const uint PowerfulLight = 40217;
        public const uint LuminousHammer = 40218;
        public const uint Burst = 40219;
        public const uint BanishPairsCast = 40220;
        public const uint BanishSpreadCast = 40221;
        public const uint BanishPairs = 40222;
        public const uint BanishSpread = 40223;
        public const uint HouseOfLightCast = 40189;
        public const uint HouseOfLight = 40188;

        // -- P3 --
        public const uint UltimateRelativity = 40266;
        public const uint Return = 40267;
        public const uint ReturnIV = 40268;
        public const uint SpellInWaitingRefrain = 40269;
        public const uint DarkWaterCast = 40270;
        public const uint DarkWater = 40271;
        public const uint DarkEruptionCast = 40273;
        public const uint DarkEruption = 40274;
        public const uint DarkFire = 40276;
        public const uint UnholyDarkness = 40277;
        public const uint Shadoweye = 40278;
        public const uint DarkBlizzard = 40279;
        public const uint DarkAero = 40280;
        public const uint Quietus = 40281;
        public const uint ShockwavePulsar = 40282;
        public const uint ShellCrusherCast = 40286;
        public const uint ShellCrusher = 40287;
        public const uint SpiritTakerCast = 40288;
        public const uint SpiritTaker = 40289;
        public const uint SinboundMeltdownCast = 40291;
        public const uint SinboundMeltdownFirst = 40235;
        public const uint SinboundMeltdownRest = 40292;
        public const uint Speed = 40293;
        public const uint Quicken = 40294;
        public const uint Slow = 40295;
        public const uint ApocalypseCast = 40296;
        public const uint Apocalypse = 40297;
        public const uint DarkestDanceCast = 40181;
        public const uint DarkestDance = 40182;
        public const uint DarkestDanceKnockback = 40183;

        // -- P4 --
        public const uint AkhRhaiCast = 40237;
        public const uint AkhRhai = 40238;
        public const uint DarklitDragonsong = 40239;
        public const uint DarklitDragonsongOracle = 40301;
        public const uint PathOfLightCast = 40187;
        public const uint PathOfLight = 40190;
        public const uint HallowedWingsLeft = 40227;
        public const uint HallowedWingsRight = 40228;
        public const uint SomberDanceCast = 40283;
        public const uint SomberDanceFar = 40284;
        public const uint SomberDanceNear = 40285;
        public const uint CrystallizeTimeUsurper = 40240;
        public const uint CrystallizeTimeOracle = 40298;
        public const uint LongingOfTheLost = 40241;
        public const uint JoylessDragonsong = 40242;
        public const uint DrachenWandererDisappear = 40244;
        public const uint Maelstrom = 40299;
        public const uint TidalLightCast = 40251;
        public const uint TidalLightFirst = 40252;
        public const uint TidalLightRest = 40253;
        public const uint HallowedWingsCast = 40229;
        public const uint HallowedWingsKnockback = 40332;
        public const uint AkhMornUsurper = 40247;
        public const uint AkhMornUsurperHit = 40248;
        public const uint AkhMornOracle = 40302;
        public const uint AkhMornOracleHit = 40303;

        // -- P5 --
        public const uint FulgentBlade = 40306;
        public const uint PathOfLightFirst = 40307;
        public const uint PathOfDarknessFirst = 40118;
        public const uint PathOfLightRest = 40308;
        public const uint PathOfDarknessRest = 40309;
        public const uint AkhMornPandora = 40310;
        public const uint AkhMornPandoraLeft = 40311;
        public const uint AkhMornPandoraRight = 40312;
        public const uint ParadiseRegained = 40319;
        public const uint WingsDarkThenLight = 40233;
        public const uint WingsLightThenDark = 40313;
        public const uint WingsCleaveLight = 40314;
        public const uint WingsCleaveDark = 40315;
        public const uint WingsBusterLight = 39879;
        public const uint WingsBusterDark = 39880;
        public const uint ParadiseTowerExplosion = 40320;
        public const uint ParadiseTowerFailure = 40321;
        public const uint PolarizingStrikes = 40316;
        public const uint PolarizingPaths = 40234;
        public const uint CruelPathOfLight = 40317;
        public const uint CruelPathOfDarkness = 40318;
        public const uint CruelPathOfLightEcho = 40119;
        public const uint CruelPathOfDarknessEcho = 40120;
    }

    public static class StatusId
    {
        public const ushort ThinIce = 911;
        public const ushort Lightsteeped = 2257;
        public const ushort ChainsOfEverlastingLight = 4157;
        public const ushort CurseOfEverlastingLight = 4158;
        public const ushort WeightOfLight = 4159;
        public const ushort Return = 2452;
        public const ushort UnholyDarknessWaiting = 2454;
        public const ushort DarkFireWaiting = 2455;
        public const ushort ShadoweyeWaiting = 2456;
        public const ushort DarkEruptionWaiting = 2460;
        public const ushort DarkWaterWaiting = 2461;
        public const ushort DarkBlizzardWaiting = 2462;
        public const ushort DarkAeroWaiting = 2463;
        public const ushort ReturnWaiting = 2464;
        public const ushort QuietusWaiting = 4174;
        public const ushort Stun = 4163;
        // Param 0x15C / 0x10D picks the clockwise / counterclockwise StatusLoopVFX.
        public const ushort HourglassRotation = 2970;
        public const ushort Wyrmclaw = 3263;
        public const ushort Wyrmfang = 3264;
        public const ushort MagicVulnerabilityUp = 2941;
        public const ushort LightResistanceDown = 4164;
        public const ushort DarkResistanceDown = 3323;
    }

    // Thin Ice's param is the slide distance in tenths of a yalm, not a stack count.
    public const ushort ThinIceSlideParam = 320;
    public const ushort HourglassClockwiseParam = 0x15C;
    public const ushort HourglassCounterclockwiseParam = 0x10D;

    public static class TetherId
    {
        public const ushort ChainsOfEverlastingLight = 110;
        public const ushort CurseOfEverlastingLight = 111;
        public const ushort Slow = 133;
        public const ushort Quicken = 134;
    }

    public static class LockonId
    {
        public const uint Stack = 62;                // com_share0c
        public const uint Spread = 139;              // target_ae_s5f
        public const uint WaterCountdown = 184;      // m0581trg_dice0h
        public const uint FrigidStone = 345;         // tag_ae5m_8s_0v
        public const uint LuminousHammer = 375;      // target_ae_s7k1
    }

    public static class VfxId
    {
        public const uint ReturnMarker = 1013;       // d1049_returnmark_c0k1
    }

    public static class KnockbackId
    {
        public const uint HeavenlyStrike = 156;
        public const uint DarkestDance = 201;
        public const uint DarkAero = 139;
        public const uint HallowedWings = 108;
    }

    public static class VoiceLineId
    {
        public const uint TwinStillness = 8205521;
        public const uint TwinSilence = 8205522;
    }

    public static class EObjId
    {
        // ExportedSG 38437: sgvf_n4gw_b3555.sgb.
        public const uint HolyPuddle = 0x1EBC4F;
        // ExportedSG 38841: sgvf_w_btl_b3566.sgb.
        public const uint DragonPuddle = 0x1EBD41;
    }

    public static class TimelineId
    {
        // mon_sp/show/mon_sp002: the Drachen Wanderer's appearance.
        public const ushort DrachenWandererAppear = 4562;
        public const ushort HallowedWingsWindup = 136;
        public const ushort HallowedWingsHide = 4582;
        public const ushort HallowedWingsShow = 7780;
        public const ushort HallowedWingsSwing = 4568;
    }
}
