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
        public const uint DramaticLysis = 0xB4AA;
        public const uint FourthWallFusion = 0xB4AE;
        public const uint HemorrhagicProjection = 0xB4AF;
        public const uint Burst = 0xB49F;
        public const uint VisceralBurst = 0xB4D6;
        public const uint Act1FourthWallFusion = 0xB9B9;
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
    }

    public static class LockonId
    {
        public const uint Stack = 0xA1;
        public const uint TankBuster = 0x158;
    }
}
