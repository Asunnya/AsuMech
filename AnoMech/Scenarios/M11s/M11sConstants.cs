using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M11s;

public static class M11sConstants
{
    public const byte Level = 100;
    public const uint DirectorEventId = 0x800375CF;

    public static IReadOnlyList<Waymark> ToxicWaymarks =>
    [
        new(WaymarkSlot.A,     new Vector3(  0f,    0f, -10f)),
        new(WaymarkSlot.B,     new Vector3( 10f,    0f,   0f)),
        new(WaymarkSlot.C,     new Vector3(  0f,    0f,  10f)),
        new(WaymarkSlot.D,     new Vector3(-10f,    0f,   0f)),
        new(WaymarkSlot.One,   new Vector3(-16.25f, 0f, -12.5f)),
        new(WaymarkSlot.Two,   new Vector3( 16.25f, 0f, -12.5f)),
        new(WaymarkSlot.Three, new Vector3( 16.25f, 0f,  12.5f)),
        new(WaymarkSlot.Four,  new Vector3(-16.25f, 0f,  12.5f)),
    ];

    // Each light party's healer sits at index 1.
    public static readonly PartyRole[][] LightParties =
    [
        [PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.PhysRangedDps],
        [PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.MeleeDpsB, PartyRole.CasterDps],
    ];

    public static class BNpcBaseId
    {
        public const uint Tyrant = 19180;
        public const uint Axe = 19184;
        public const uint Scythe = 19185;
        public const uint Sword = 19186;
        public const uint Maelstrom = 19183;
        public const uint Comet = 19181;
        public const uint TyrantShade = 19178;
        public const uint Helper = 9020;
    }

    public static class EObjId
    {
        public const uint AtomicPuddle = 0x1EBF24;
    }

    public static class BNpcNameId
    {
        public const uint Tyrant = 14305;
        public const uint Maelstrom = 14307;
        public const uint Comet = 14306;
    }

    public static class ActionId
    {
        public const uint Attack = 0xB405;
        public const uint CrownOfArcadia = 0xB406;
        public const uint RawSteelTrophyAxe = 0xB422;
        public const uint RawSteelTrophyScythe = 0xB423;
        public const uint RawSteelAxe = 0xB40A;
        public const uint RawSteelAxeStack = 0xB40B;
        public const uint Impact = 0xB40C;
        public const uint RawSteelScythe = 0xB40D;
        public const uint RawSteelScytheFollow = 0xB40E;
        public const uint RawSteelScytheCone = 0xB40F;
        public const uint HeavyHitter = 0xB410;
        public const uint Reposition = 0xB404;
        public const uint TrophyWeapons = 0xB416;
        public const uint AssaultEvolved = 0xB417;
        public const uint AssaultEvolvedAxe = 0xB418;
        public const uint AssaultEvolvedScythe = 0xB419;
        public const uint AssaultEvolvedSword = 0xB41A;
        public const uint HeavyWeight = 0xB41B;
        public const uint SweepingVictory = 0xB41C;
        public const uint SharpTaste = 0xB41D;
        public const uint DashToAxe = 0xB543;
        public const uint DashToScythe = 0xB544;
        public const uint DashToSword = 0xB545;
        public const uint VoidStardust = 0xB412;
        public const uint Cometite = 0xB413;
        public const uint Comet = 0xB414;
        public const uint CrushingComet = 0xB415;
        public const uint DanceOfDominationTrophy = 0xB7BB;
        public const uint DanceOfDomination = 0xB41E;
        public const uint DanceOfDominationFirst = 0xB41F;
        public const uint DanceOfDominationRepeat = 0xB421;
        public const uint DanceOfDominationLast = 0xB7EA;
        public const uint EyeOfTheHurricane = 0xB424;
        public const uint HurricaneExplosion = 0xB420;
        public const uint HurricaneExplosionVisual = 0xB7BC;
        public const uint Charybdistopia = 0xB425;
        public const uint Charybdis = 0xB426;
        public const uint PowerfulGust = 0xB427;
        public const uint UltimateTrophyWeapons = 0xB7ED;
        public const uint AssaultApex = 0xB7EE;
        public const uint ImmortalReign = 0xB428;
        public const uint OneAndOnly = 0xB429;
        public const uint OneAndOnlyRaidwide = 0xB42A;
        public const uint GreatWallOfFire = 0xB42B;
        public const uint GreatWallOfFireLine = 0xB42C;
        public const uint GreatWallOfFireWall = 0xB42D;
        public const uint WallExplosion = 0xB42E;
        public const uint OrbitalOmen = 0xB432;
        public const uint OrbitalOmenLine = 0xB433;
        public const uint FireAndFury = 0xB42F;
        public const uint FireAndFuryFront = 0xB430;
        public const uint FireAndFuryBack = 0xB431;
        public const uint Meteorain = 0xB434;
        public const uint CosmicKiss = 0xB435;
        public const uint ForegoneFatality = 0xB436;
        public const uint UnmitigatedExplosion = 0xB437;
        public const uint CometExplosion = 0xB438;
        public const uint FearsomeFireball = 0xB439;
        public const uint FearsomeFireballLine = 0xB43A;
        public const uint TripleTyrannhilation = 0xB43C;
        public const uint Shockwave = 0xB43D;
        public const uint Flatliner = 0xB43F;
        public const uint FlatlinerKnockback = 0xBA90;
        public const uint MajesticMeteor = 0xB440;
        public const uint MajesticMeteorPuddle = 0xB441;
        public const uint MajesticMeteorain = 0xB442;
        public const uint MajesticMeteowrath = 0xB443;
        public const uint TowerExplosion = 0xB444;
        public const uint UnmitigatedTowerExplosion = 0xB445;
        public const uint FireBreath = 0xB446;
        public const uint FireBreathLine = 0xB447;
        public const uint MassiveMeteor = 0xB448;
        public const uint MassiveMeteorStack = 0xB449;
        public const uint EclipticStampede = 0xB452;
        public const uint MammothMeteor = 0xB453;
        public const uint AtomicImpact = 0xB454;
        public const uint EclipticMajesticMeteor = 0xB455;
        public const uint EclipticCosmicKiss = 0xB456;
        public const uint WeightyImpact = 0xB457;
        public const uint EclipticUnmitigatedExplosion = 0xB458;
        public const uint EclipticMeteowrath = 0xB459;
        public const uint TwoWayFireball = 0xB7BD;
        public const uint TwoWayFireballLine = 0xB7BE;
        public const uint HeartbreakKick = 0xB45D;
        public const uint HeartbreakKickTower = 0xB45E;
        public const uint Heartbreaker = 0xB462;
    }

    public static class StatusId
    {
        public const ushort PhysicalVulnerabilityUp = 0xB7C;
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort DamageDown = 0xB5F;
        public const ushort FireResistanceDownII = 0xB79;
    }

    public static class LockonId
    {
        public const uint Stack = 0xA1;
        public const uint Spread = 0x8B;
        public const uint Meteor = 0xF4;
        public const uint FireballLine = 0x20D;
        public const uint MassiveMeteor = 0x131;
        public const uint AtomicImpact = 0x1E;
    }

    public static class TetherId
    {
        public const ushort ForegoneFatality = 0x164;
        public const ushort MeteowrathSlack = 0x39;
        public const ushort MeteowrathStretched = 0xF9;
    }

    public static class TimelineId
    {
        public const ushort SwordAppears = 0x11D1;
        public const ushort AxeAppears = 0x11D2;
        public const ushort ScytheAppears = 0x11D3;
        public const ushort SwordStrikes = 0x11DD;
        public const ushort AxeStrikes = 0x11DE;
        public const ushort ScytheStrikes = 0x11DF;
    }

    public static class MapEffect
    {
        // Orbital Omen's portals: slots 2-5 on the north wall (x -15, -5, 5, 15), 6-9 on the east (z -15, -5, 5, 15).
        public const byte NorthPortalSlot = 0x02;
        public const byte EastPortalSlot = 0x06;
        public const uint PortalShows = 0x00020001;
        public const uint PortalFires = 0x00200010;
        public const uint PortalOff = 0x00080004;
        // Foregone Fatality's shades come out of slots 0x12-0x15: north, east, south, west.
        public const byte ShadeSlot = 0x12;
        public const byte ArenaSlot = 0x00;
        public const uint ArenaSplits = 0x40004000;
        public const uint MiddleFalls = 0x00020001;
        public const uint ArenaRestored = 0x08000004;
        // Arena Split's towers (0x0E-0x11) and their floor marks (0x0A-0x0D).
        public const byte TowerSlot = 0x0E;
        public const byte TowerMarkSlot = 0x0A;
        public const uint TowerMarkShows = 0x00020004;
        // A tower lights up for one soaker and fills for two (UNVERIFIED: that only a filled one draws its meteor down).
        public const uint TowerOneIn = 0x00200010;
        public const uint TowerFilled = 0x00800040;
        public const uint TowerEmptied = 0x01000001;
        // Majestic Meteorain's portals: 0x16-0x19 for lines at x -21, -11, 11, 21.
        public const byte MeteorainSlot = 0x16;
        // Meteowrath's shades: 0x1A-0x1D on the west wall (z -15, -5, 5, 15), 0x1E-0x21 on the east.
        public const byte WrathShadeSlot = 0x1A;
        // Ecliptic Stampede's shades on the four cardinals.
        public const byte EclipticShadeSlot = 0x22;
    }

    public static class Geometry
    {
        public const float ArenaHalfWidth = 20f;
        public const float WeaponRing = 12f;
    }
}
