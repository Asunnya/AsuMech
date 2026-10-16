using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Scenarios.Dsr;

public class DsrConstants
{
    public const byte Level = 90;

    public static class Geometry
    {
        public const float ArenaHalfWidth = 22f;
        // UNVERIFIED: players stand alive at up to 20.85y; where the wall starts killing was never observed.
        public const float ThordanArenaRadius = 21.5f;
    }

    // Numbers north and letters south are the Hyperdimensional Slash prey spots.
    public static IReadOnlyList<Waymark> Phase1Waymarks =>
    [
        new(WaymarkSlot.A, new Vector3(-6.669f, 0f, 3.162f)),
        new(WaymarkSlot.B, new Vector3(-3.186f, 0f, 6.708f)),
        new(WaymarkSlot.C, new Vector3(3.22f, 0f, 6.667f)),
        new(WaymarkSlot.D, new Vector3(6.599f, 0f, 3.18f)),
        new(WaymarkSlot.One, new Vector3(-6.862f, 0f, -3.595f)),
        new(WaymarkSlot.Two, new Vector3(-3.159f, 0f, -6.721f)),
        new(WaymarkSlot.Three, new Vector3(3.441f, 0f, -6.881f)),
        new(WaymarkSlot.Four, new Vector3(6.664f, 0f, -3.598f)),
    ];

    public static IReadOnlyList<Waymark> NaurWaymarks =>
    [
        new(WaymarkSlot.A, new Vector3(0f, 0f, -9.5f)),
        new(WaymarkSlot.B, new Vector3(13.123f, 0f, -13.458f)),
        new(WaymarkSlot.C, new Vector3(9.5f, 0f, 0f)),
        new(WaymarkSlot.D, new Vector3(12.75f, 0f, 12.508f)),
        new(WaymarkSlot.One, new Vector3(0f, 0f, 9.5f)),
        new(WaymarkSlot.Two, new Vector3(-12.852f, 0f, 12.929f)),
        new(WaymarkSlot.Three, new Vector3(-9.5f, 0f, 0f)),
        new(WaymarkSlot.Four, new Vector3(-12.782f, 0f, -12.875f)),
    ];

    public static Vector3 Phase1Waymark(WaymarkSlot slot) => Phase1Waymarks.First(w => w.Slot == slot).Offset;

    // Compass degrees: 0 = north (-Z), 90 = east (+X).
    public static Vector3 AtBearing(float bearingDegrees, float radius)
    {
        var rad = bearingDegrees * MathF.PI / 180f;
        return new Vector3(radius * MathF.Sin(rad), 0f, -radius * MathF.Cos(rad));
    }

    public class BNpcBaseId
    {
        public const uint Dummy = 9020;
        public const uint Adelphel = 12601;
        public const uint Grinnaux = 12602;
        public const uint Charibert = 12603;
        public const uint Thordan = 12604;
        public const uint Zephirin = 12592;
        public const uint Janlenoux = 12632;
        public const uint Vellguine = 12633;
        public const uint Paulecrain = 12634;
        public const uint Ignasse = 12635;
        public const uint Hermenost = 12636;
        public const uint Guerrique = 12637;
        public const uint Haumeric = 12638;
        public const uint Noudenet = 12639;
        public const uint HolyComet = 12591;
        public const uint Haurchefant = 13117;
        public const uint SpearOfTheFury = 11810;
        public const uint Brightsphere = 13070;
        public const uint AetherialTear = 13071;
        public const uint Nidhogg = 12605;
        public const uint NidhoggDiver = 12606;
        public const uint AzurePrice = 12607;
        public const uint GildedPrice = 12608;
        public const uint LeftEye = 12609;
        public const uint RightEye = 12610;
        public const uint Ysayle = 13118;
        public const uint Estinien = 13119;
        public const uint Alphinaud = 13769;
        public const uint DarkThordan = 12611;
        public const uint Vedrfolnir = 12646;
        public const uint Darkscale = 12647;
        public const uint Vidofnir = 12648;
        public const uint DarkscaleDiver = 12630;
        public const uint VidofnirDiver = 12631;
        public const uint MeteorCircle = 13920;
        public const uint NidhoggP6 = 12612;
        public const uint Hraesvelgr = 12613;
        public const uint ScarletPrice = 13238;
    }

    public class BNpcNameId
    {
        public const uint Dummy = 108;
        public const uint Adelphel = 3634;
        public const uint Grinnaux = 3639;
        public const uint Charibert = 3642;
        public const uint Thordan = 3632;
        public const uint Zephirin = 3633;
        public const uint Janlenoux = 3635;
        public const uint Vellguine = 3636;
        public const uint Paulecrain = 3637;
        public const uint Ignasse = 3638;
        public const uint Hermenost = 3640;
        public const uint Guerrique = 3641;
        public const uint Haumeric = 3643;
        public const uint Noudenet = 3644;
        public const uint HolyComet = 11321;
        public const uint Haurchefant = 1455;
        public const uint SpearOfTheFury = 11320;
        public const uint Brightsphere = 4385;
        public const uint AetherialTear = 3293;
        public const uint Nidhogg = 3458;
        public const uint Alphinaud = 4130;
        public const uint Estinien = 11312;
        public const uint Ysayle = 11313;
        public const uint AzurePrice = 11315;
        public const uint GildedPrice = 11316;
        public const uint LeftEye = 11317;
        public const uint RightEye = 11318;
        public const uint Darkscale = 3983;
        public const uint Vedrfolnir = 3984;
        public const uint Vidofnir = 11314;
        public const uint MeteorCircle = 4386;
        public const uint Hraesvelgr = 4954;
        public const uint ScarletPrice = 3459;
    }

    public class ActionId
    {
        public const uint ShiningBlade = 0x62CE;
        public const uint BrightFlare = 0x62CF;
        public const uint HoliestHallowing = 0x62D0;
        public const uint HolyShieldBash = 0x62D1;
        public const uint HolyBladedanceWindup = 0x62D2;
        public const uint HolyBladedance = 0x62D3;
        public const uint HoliestOfHoly = 0x62D4;
        public const uint Execution = 0x62D5;
        public const uint HyperdimensionalSlash = 0x62D6;
        public const uint HyperdimensionalSlashLine = 0x62D7;
        public const uint HyperdimensionalSlashCone = 0x63EE;
        public const uint EmptyDimension = 0x62DA;
        public const uint FullDimension = 0x62DB;
        public const uint FaithUnmoving = 0x62DC;
        public const uint Heavensblaze = 0x62DD;
        public const uint Heavensflame = 0x62DE;
        public const uint HeavensflameHit = 0x62DF;
        public const uint HolyChain = 0x62E0;
        public const uint PlanarPrison = 0x62E1;
        public const uint SpearOfTheFury = 0x62E2;
        public const uint Shockwave = 0x62E3;
        public const uint PureOfHeart = 0x62E4;
        public const uint BrightwingedFlight = 0x6316;
        public const uint Brightwing = 0x6319;
        public const uint Skyblind = 0x631A;

        public const uint ThordanAttack = 0x63BB;
        public const uint ThordanLeap = 0x63C4;
        public const uint AscalonsMight = 0x63C5;
        public const uint AncientQuaga = 0x63C6;
        public const uint HeavenlyHeel = 0x63C7;
        public const uint AscalonsMercyConcealed = 0x63C8;
        public const uint AscalonsMercyConcealedCone = 0x63C9;
        public const uint LightningStorm = 0x63CC;
        public const uint LightningStormHit = 0x63CD;
        public const uint DragonsRage = 0x63CE;
        public const uint DragonsRageHit = 0x63CF;
        public const uint StrengthOfTheWard = 0x63D3;
        public const uint SpiralThrust = 0x63D4;
        public const uint HeavyImpactWindup = 0x63D5;
        public const uint HeavyImpact = 0x63D6;
        public const uint HeavyImpactRing1 = 0x63D7;
        public const uint HeavyImpactRing2 = 0x63D8;
        public const uint HeavyImpactRing3 = 0x63D9;
        public const uint HeavyImpactRing4 = 0x63DA;
        public const uint DimensionalCollapseWindup = 0x63DB;
        public const uint DimensionalCollapse = 0x63DC;
        public const uint SkywardLeap = 0x63DD;
        public const uint ConvictionWindup = 0x63DE;
        public const uint Conviction = 0x63DF;
        public const uint EternalConviction = 0x63E0;
        public const uint SanctityOfTheWard = 0x63E1;
        public const uint SanctityShiningBlade = 0x63E2;
        public const uint SacredSever = 0x63E3;
        public const uint DragonsGaze = 0x63D0;
        public const uint DragonsGazeHit = 0x63D1;
        public const uint DragonsGlory = 0x63D2;
        public const uint HiemalStorm = 0x63E6;
        public const uint HiemalStormHit = 0x63E7;
        public const uint HolyComet = 0x63E8;
        public const uint HolyCometHit = 0x63E9;
        public const uint HolyImpact = 0x63EA;
        public const uint HeavensStake = 0x6FAE;
        public const uint HeavensStakeCircle = 0x6FAF;
        public const uint HeavensStakeDonut = 0x6FB0;
        public const uint SecondConvictionWindup = 0x6FEA;
        public const uint SecondConviction = 0x6FEB;
        public const uint FirstConvictionWindup = 0x737B;
        public const uint FirstConviction = 0x737C;
        public const uint UltimateEndArrival = 0x63BC;
        public const uint UltimateEnd = 0x63BD;
        public const uint UltimateEndHit = 0x63BE;
        public const uint BroadSwingWindup = 0x63BF;
        public const uint BroadSwingRightFirst = 0x63C0;
        public const uint BroadSwingLeftFirst = 0x63C1;
        public const uint BroadSwing = 0x63C2;
        public const uint AethericBurst = 0x63C3;
        public const uint KnightsOfTheRound = 0x63ED;

        public const uint FinalChorus = 0x6708;
        public const uint Geirskogul = 0x670A;
        public const uint DrachenlanceWindup = 0x670B;
        public const uint Drachenlance = 0x670C;
        public const uint DiveFromGrace = 0x670D;
        public const uint DarkHighJump = 0x670E;
        public const uint DarkSpineshatterDive = 0x670F;
        public const uint DarkElusiveJump = 0x6710;
        public const uint DarkdragonDive = 0x6711;
        public const uint GnashAndLash = 0x6712;
        public const uint LashAndGnash = 0x6713;
        public const uint EyeOfTheTyrant = 0x6714;
        public const uint GnashingWheel = 0x6715;
        public const uint LashingWheel = 0x6716;
        public const uint DarkdragonDiveOne = 0x6717;
        public const uint DarkdragonDiveTwo = 0x6718;
        public const uint DarkdragonDiveThree = 0x6719;
        public const uint DarkdragonDiveFour = 0x671A;
        public const uint DarkdragonDiveFailed = 0x671B;
        public const uint SoulTether = 0x671C;
        public const uint NidhoggAttack = 0x6730;
        public const uint RevengeOfTheHorde = 0x7436;

        public const uint Resentment = 0x68BA;
        public const uint SteepInRage = 0x68BD;
        public const uint Hatebound = 0x68BE;
        public const uint FlareStar = 0x68BF;
        public const uint FlareStarUnpopped = 0x68C0;
        public const uint FlareNova = 0x68C1;
        public const uint FlareNovaUnpopped = 0x68C2;
        public const uint MirageDiveWindup = 0x68C3;
        public const uint MirageDive = 0x68C4;
        public const uint SoulOfFriendship = 0x68C5;
        public const uint SoulOfDevotion = 0x68C6;
        public const uint EyesDrift = 0x717A;

        public const uint AscalonsMercyRevealed = 0x63CA;
        public const uint AscalonsMercyRevealedCone = 0x63CB;
        public const uint AltarFlare = 0x63E4;
        public const uint AltarFlareEruption = 0x63E5;
        public const uint Incarnation = 0x6B86;
        public const uint DragonsEye = 0x6B87;
        public const uint WrathOfTheHeavens = 0x6B89;
        public const uint SpiralPierce = 0x6B8A;
        public const uint TwistingDive = 0x6B8B;
        public const uint Twister = 0x6B8C;
        public const uint CauterizeDarkscale = 0x6B8D;
        public const uint CauterizeVidofnir = 0x6B8E;
        public const uint ChainLightning = 0x6B8F;
        public const uint ChainLightningHit = 0x6B90;
        public const uint LiquidHeaven = 0x6B91;
        public const uint DeathOfTheHeavens = 0x6B92;
        public const uint WrathSkywardLeap = 0x72A2;
        public const uint DeathSpearOfTheFury = 0x6B93;
        public const uint Deathstorm = 0x6B94;
        public const uint WingsOfSalvation = 0x6B95;
        public const uint WingsOfSalvationPuddle = 0x6B96;
        public const uint HolyMeteor = 0x6B97;
        public const uint HraesvelgrAttack = 0x6D22;
        public const uint HallowedWingsNorthTanksNear = 0x6D26;
        public const uint HallowedWingsNorthTanksFar = 0x6D27;
        // UNVERIFIED: the south-wing casts were never seen; assumed to mirror 6D26/6D27.
        public const uint HallowedWingsSouthTanksNear = 0x6D23;
        public const uint HallowedWingsSouthTanksFar = 0x6D24;
        public const uint HallowedWingsSouth = 0x6D25;
        public const uint HallowedWingsNorth = 0x6D28;
        public const uint HallowedPlume = 0x6D29;
        public const uint NidhoggP6Attack = 0x6D2A;
        public const uint HotWing = 0x6D2B;
        public const uint HotWingSides = 0x6D2C;
        public const uint HotTail = 0x6D2D;
        public const uint HotTailMiddle = 0x6D2E;
        public const uint MortalVow = 0x6D30;
        public const uint MortalVowPass = 0x6D31;
        public const uint DreadWyrmsbreath = 0x6D32;
        public const uint DreadWyrmsbreathGlowing = 0x6D33;
        public const uint GreatWyrmsbreath = 0x6D34;
        public const uint GreatWyrmsbreathGlowing = 0x6D35;
        public const uint FlameBreath = 0x6D36;
        public const uint IceBreath = 0x6D37;
        public const uint SwirlingBlizzard = 0x6D38;
        public const uint DarkOrb = 0x6D39;
        public const uint HolyOrb = 0x6D3A;
        public const uint DarkBreath = 0x6D3B;
        public const uint HolyBreath = 0x6D3C;
        public const uint StaggeringBreath = 0x6D3D;
        public const uint CauterizeNidhogg = 0x6D3E;
        public const uint CauterizeHraesvelgr = 0x6D3F;
        public const uint AkhAfahHraesvelgr = 0x6D41;
        public const uint AkhAfahHraesvelgrStack = 0x6D42;
        public const uint AkhAfahNidhogg = 0x6D43;
        public const uint AkhAfahNidhoggStack = 0x6D44;
        public const uint WrothFlames = 0x6D45;
        public const uint AkhMorn = 0x6D46;
        public const uint AkhMornFollowUp = 0x6D47;
        public const uint FlameBlast = 0x6729;
        public const uint SpreadingFlames = 0x742B;
        public const uint Touchdown = 0x6D40;
        public const uint TouchdownHit = 0x70E7;
        public const uint DragonsRevengeOfTheHorde = 0x6D21;
        public const uint EntangledFlames = 0x742C;

        public const uint Interject = 7538;
        public const uint HeadGraze = 7551;
        public const uint ArmsLength = 7548;
        public const uint Surecast = 7559;
    }

    public class StatusId
    {
        public const ushort Stun = 0x95;
        public const ushort BurningChains = 0x301;
        public const ushort LightResistanceDown = 0x8E6;
        public const ushort BrightwingedFortitude = 0xA64;
        public const ushort Skyblind = 0xA65;
        public const ushort PlanarImprisonment = 0xA66;
        public const ushort BrightwingedFury = 0xA68;
        public const ushort FireResistanceDownII = 0xB56;
        public const ushort IceResistanceDownII = 0xB57;
        public const ushort LightningResistanceDownII = 0xBB6;
        public const ushort PhysicalVulnerabilityUp = 0xB7C;
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort DownForTheCount = 0xC5D;
        public const ushort DamageDown = 0xC5E;
        public const ushort SlashingResistanceDown = 0xC3A;
        public const ushort Hysteria = 0x128;
        public const ushort Prey = 0x232;
        public const ushort Burns = 0xB81;
        public const ushort Frostbite = 0xB82;
        public const ushort HighJumpTarget = 0xAC3;
        public const ushort SpineshatterDiveTarget = 0xAC4;
        public const ushort ElusiveJumpTarget = 0xAC5;
        public const ushort FirstInLine = 0xBBC;
        public const ushort SecondInLine = 0xBBD;
        public const ushort ThirdInLine = 0xBBE;
        public const ushort Clawbound = 0xAD7;
        public const ushort Fangbound = 0xAD8;
        public const ushort BoundAndDetermined = 0xAD9;
        public const ushort SoulOfFriendship = 0xABC;
        public const ushort SoulOfDevotion = 0xABD;
        public const ushort Bleeding = 0xB87;
        public const ushort PiercingResistanceDown = 0xC3B;
        public const ushort Thunderstruck = 0xB11;
        public const ushort Doom = 0xBA0;
        public const ushort MortalVow = 0xB50;
        public const ushort MortalAtonement = 0xB51;
        public const ushort Boiling = 0xB52;
        public const ushort Freezing = 0xB53;
        public const ushort SpreadingFlames = 0xAC6;
        public const ushort Pyretic = 0x3C0;
        public const ushort DeepFreeze = 0xD98;
        public const ushort EntangledFlames = 0xAC7;
    }

    public class TetherId
    {
        public const ushort BurningChains = 0x09;
        public const ushort PlanarPrison = 0x35;
        public const ushort HolyShieldBash = 0x54;
        public const ushort SoulTether = 0x54;
        public const ushort Soul = 0x0C;
        public const ushort Fangbound = 0x33;
        public const ushort Clawbound = 0x34;
        public const ushort LeftEyeToEstinien = 0xB2;
        public const ushort RightEyeToEstinien = 0xB3;
        public const ushort SpiralPierce = 0x05;
        public const ushort DragonToThordan = 0xA5;
        public const ushort DragonHpLink = 0x02;
        public const ushort NidhoggBreath = 0xC2;
        public const ushort HraesvelgrBreath = 0xC3;
        public const ushort BreathTooClose = 0xC4;
    }

    public class EObjId
    {
        public const uint PlanarPrison = 0x1EB681;
        public const uint IcePuddle = 0x1EB682;
        public const uint FirePuddle = 0x1EB686;
        public const uint Twister = 0x1E8910;
        public const uint LiquidHeaven = 0x1EB684;
        public const uint WingsOfSalvation = 0x1EB685;
    }

    public class LockonId
    {
        public const uint HyperdimensionalSlash = 0xEA;
        public const uint SkywardLeap = 0x14A;
        public const uint OneSword = 0x32;
        public const uint TwoSwords = 0x33;
        public const uint MeteorPrey = 0x11D;
        public const uint Circle = 0x119;
        public const uint Triangle = 0x11A;
        public const uint Square = 0x11B;
        public const uint Cross = 0x11C;
        public const uint FirstInLine = 0x13F;
        public const uint SecondInLine = 0x140;
        public const uint ThirdInLine = 0x141;
        public const uint SoulOfFriendship = 0x11E;
        public const uint SoulOfDevotion = 0x11F;
        public const uint WrathDefamation = 0x0E;
        public const uint Divebomb = 0x14;
    }

    // UNVERIFIED: replayed from the log's director traffic at each phase change; whether they
    // switch the arena scenery client-side was never observed.
    public static class ArenaDirector
    {
        public const uint Layout = 0x80000016;
        public const uint MapChange = 0x8000001F;
        public const uint Music = 0x80000004;
        public const uint KnightsLayout = 0x01;
        public const uint PrisonLayout = 0x14;
        public const uint ThordanLayout = 0x1E;
        public const uint WardLayout = 0x25;
        public const uint SanctityLayout = 0x2F;
        public const uint UltimateEndLayout = 0x39;
        public const uint NidhoggLayout = 0x48;
        public const uint EyesLayout = 0x5A;
        public const uint WrathLayout = 0x64;
        public const uint DoubleDragonsLayout = 0x2D;
        public const uint SecondWyrmsbreathLayout = 0x3C;
        public const uint KnightsMap = 758;
        public const uint ThordanMap = 765;
        public const uint ThordanMusic = 0x1AF3;
        // MapEffect slot 8, sgbg_r1fz_e0_hreas01 (layer e00_nidhogg), lit as Nidhogg arrives.
        public const byte NidhoggSlot = 8;
        public const uint NidhoggSlotOn = 0x00020001;
    }

    // MapEffect slots 0-7 are the giant eyes at r40, slot i at bearing 45*i.
    public static class EyeEffect
    {
        public const uint Opens = 0x00020001;
        public const uint Glares = 0x00100001;
        public const uint Closes = 0x00400020;
    }

    // UNVERIFIED as timelines: the log sends them as ActorControl 0x197 on the knights.
    public class TimelineId
    {
        public const ushort KnightAppear = 0x11D2;
        public const ushort KnightVanish = 0x11DD;
        public const ushort WarpStart = 0x1E39;
        public const ushort WarpEnd = 0x1E43;
        public const ushort EyeAppear = 0x1E46;
        public const ushort AllyAppear = 0x1E4C;
        public const ushort DragonAppear = 0x1E44;
    }

    public class KnockbackId
    {
        public const uint Execution = 111;
        public const uint FaithUnmoving = 169;
    }
}
