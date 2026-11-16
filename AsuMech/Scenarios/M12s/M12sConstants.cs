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
        public const uint LindwurmP1 = 19195;
        public const uint SnakeHead = 19196;
        public const uint BloodVessel = 19199;
    }

    public static class BNpcNameId
    {
        public const uint LindwurmP1 = 14378;
        public const uint LindwurmP2 = 14379;
        public const uint BloodVessel = 14381;
    }

    public static class ActionId
    {
        public const uint TheFixer = 0xB4D7;
        public const uint GrotesquerieAct2 = 0xBEBE;
        public const uint GrotesquerieAct3 = 0xBEBF;
        public const uint PhagocyteSpotlight = 0xB4B6;
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
        public const uint Splattershed = 0xB9C4;
        public const uint SplattershedRaidwide = 0xB9C6;
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
    }

    public static class StatusId
    {
        public const ushort Bind = 2518;
        public const ushort DamageDown = 2911;
        public const ushort MagicVulnerabilityUp = 2941;
        public const ushort FirstInLine = 3004;
        public const ushort SecondInLine = 3005;
        public const ushort ThirdInLine = 3006;
        public const ushort FourthInLine = 3451;
        public const ushort DirectedGrotesquerie = 3558;
        public const ushort PoisonResistanceDownII = 3935;
        public const ushort BondsOfFleshAlpha = 4752;
        public const ushort UnbreakableFleshAlpha = 4753;
        public const ushort BondsOfFleshBeta = 4754;
        public const ushort UnbreakableFleshBeta = 4755;
        public const ushort MitoticPhase = 4764;
        public const ushort FateOfTheWurm = 4772;
    }

    // DirectedGrotesquerie's param: the way the growth points.
    public static class GrowthParam
    {
        public const int North = 0x436;
        public const int East = 0x437;
        public const int South = 0x438;
        public const int West = 0x439;
    }

    public static class TetherId
    {
        public const ushort Cell = 366;
    }

    public static class LockonId
    {
        public const uint CellChainSpawn = 657;
    }

    public static class ModelState
    {
        public const byte Coiled = 0x1C;
        public const byte Burrowed = 0x06;
        public const byte Normal = 0x00;
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
    }
}
