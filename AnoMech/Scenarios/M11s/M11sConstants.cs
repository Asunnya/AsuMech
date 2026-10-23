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
        public const uint Helper = 9020;
    }

    public static class BNpcNameId
    {
        public const uint Tyrant = 14305;
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
    }

    public static class StatusId
    {
        public const ushort PhysicalVulnerabilityUp = 0xB7C;
        public const ushort MagicVulnerabilityUp = 0xB7D;
    }

    public static class LockonId
    {
        public const uint Stack = 0xA1;
        public const uint Spread = 0x8B;
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

    public static class Geometry
    {
        public const float ArenaHalfWidth = 20f;
        public const float WeaponRing = 12f;
    }
}
