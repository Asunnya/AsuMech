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
        public const uint LindwurmP2 = 19202;
        public const uint Lindschrat = 19204;
    }

    public static class BNpcNameId
    {
        public const uint LindwurmP1 = 14378;
        public const uint LindwurmP2 = 14379;
        public const uint Lindschrat = 14380;
    }

    public static class ActionId
    {
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
    }

    public static class StatusId
    {
        public const ushort DamageDown = 0xB5F;
        public const ushort FireResistanceDownII = 0xB79;
        public const ushort DarkResistanceDownII = 0xCFB;
    }

    public static class LockonId
    {
        public const uint DoubleSobat = 0x256;
    }
}
