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
        public const uint PurpleOrb = 19200;
        public const uint GreenOrb = 19201;
    }

    public static class EObjId
    {
        public const uint BloodVessel = 0x1EBF29;
    }

    public static class BNpcNameId
    {
        public const uint LindwurmP1 = 14378;
        public const uint LindwurmP2 = 14379;
    }

    public static class ActionId
    {
        public const uint MortalSlayer = 0xB495;
        public const uint MortalSlayerGreen = 0xB496;
        public const uint ArmySlayerGreen = 0xB497;
        public const uint MortalSlayerPurple = 0xB498;
        public const uint ArmySlayerPurple = 0xB499;
        public const uint RavenousReach = 0xB469;
        public const uint RavenousReachCone = 0xB49D;
        public const uint PhagocyteSpotlight = 0xB49E;
        public const uint Burst = 0xB49F;
        public const uint DramaticLysis = 0xB4AA;
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
    }

    public static class StatusId
    {
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort DamageDown = 0xB5F;
        public const ushort PoisonResistanceDownII = 0xF5F;
        public const ushort BondsOfFleshAlpha = 0x1290;
        public const ushort UnbreakableFleshAlpha = 0x1291;
        public const ushort BurstingGrotesquerie = 0x1299;
        public const ushort RottingFlesh = 0x129B;
    }

    public static class LockonId
    {
        public const uint CellChain = 0x291;
        public const uint SlaughtershedStack = 0x13D;
        public const uint SlaughtershedSpread = 0x177;
    }

    public static class TetherId
    {
        public const ushort CellChain = 0x16E;
    }

    // The boss's model states (ActorControl 0x3F) while a mechanic is up.
    public static class ModelState
    {
        public const byte Idle = 0;
        public const byte CurtainCall = 4;
        public const byte Slaughtershed = 7;
        public const byte MortalSlayer = 0x1F;
        public const byte SnakeHeadOut = 5;
    }
}
