using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;

namespace AsuMech.Scenarios.Ucob;

public class UcobConstants
{
    public const byte Level = 70;

    public static class Geometry
    {
        // The edge moves between phases: earlier phases stop knockbacks at ~24.5.
        public const float P5ArenaRadius = 21.8f;
        // UNVERIFIED: Twintania's death wall; nobody in the logs stood past 20.
        public const float P1ArenaRadius = 24.5f;

        public const float NeurolinkRadius = 2f; // UNVERIFIED
        public const float TwisterTriggerRadius = 2f; // UNVERIFIED
        public const float TwisterBlastRadius = 8f;
        public const float LunarDynamoInner = 6f; // UNVERIFIED
        // UNVERIFIED: someone 6.6y from her was still hit.
        public const float IronChariotRadius = 7f;
        public const float ThermionicBurstHalfAngle = MathF.PI / 16f;

        // Twintania's P1 neurolinks stay down for the rest of the fight, wherever she dropped them.
        public static readonly Vector2[] P3Neurolinks = [new(-0.16f, -8.40f), new(-8.51f, 5.74f), new(8.53f, 5.61f)];
    }

    // UNVERIFIED: one or two took lethal hits, five or more lived.
    public const int FireballMinimumStack = 4;

    // "Aether Markers": A/B/C on the rim, D and 1-4 inside. Scenario-local == world here,
    // since the arena centre is the zone origin.
    public static IReadOnlyList<Waymark> AetherWaymarks =>
    [
        new(WaymarkSlot.A,     new Vector3( 20.986f, 0f, -11.020f)),
        new(WaymarkSlot.B,     new Vector3(  9.315f, 0f,  21.971f)),
        new(WaymarkSlot.C,     new Vector3(-19.999f, 0f,  11.772f)),
        new(WaymarkSlot.D,     new Vector3(      0f, 0f,   9.000f)),
        new(WaymarkSlot.One,   new Vector3(      0f, 0f,  -8.000f)),
        new(WaymarkSlot.Two,   new Vector3( -8.000f, 0f,   5.000f)),
        new(WaymarkSlot.Three, new Vector3(  8.000f, 0f,   5.000f)),
        new(WaymarkSlot.Four,  new Vector3(      0f, 0f,   0.000f)),
    ];

    // The arena's SharedGroup EObjs (UcobZone), by the layout each binds to; the pieces are only
    // bound, never played.
    public class ArenaLayoutId
    {
        public const uint DeathWall = 0x6C7D48;
        public const uint P5Arena = 0x6D0C52;
        public const uint Piece1 = 0x6C641C;
        public const uint Piece2 = 0x6C0E8A;
        public const uint Piece3 = 0x6C0D4C;
        public const uint Piece4 = 0x6C0D4B;
    }

    public class BNpcBaseId
    {
        public const uint Dummy = 9020;
        public const uint Twintania = 8159;
        public const uint Oviform = 8160;
        public const uint NaelDeusDarnus = 8161;
        public const uint Firehorn = 8163;
        public const uint Iceclaw = 8164;
        public const uint Thunderwing = 8165;
        public const uint TailOfDarkness = 8166;
        public const uint FangOfLight = 8167;
        public const uint BahamutPrime = 0x1FE8;
        public const uint Phoenix = 8169;
        // The fight's generic invisible caster (x42 in the instance): AOE source for
        // everything the boss doesn't cast from its own body.
    }

    public class BNpcNameId
    {
        public const uint Twintania = 1482;
        public const uint Oviform = 2210;
        public const uint NaelDeusDarnus = 2612;
        public const uint Firehorn = 2630;
        public const uint Iceclaw = 2631;
        public const uint Thunderwing = 2632;
        public const uint FangOfLight = 6957;
        public const uint TailOfDarkness = 6958;
        public const uint BahamutPrime = 3210;
        public const uint Phoenix = 3204;
    }

    public class StatusId
    {
        public const ushort LohsDaih = 1437;
        public const ushort Burns = 0xFA;
        public const ushort Neurolink = 0x158;
        public const ushort TwintaniaNeurolink = 0x273;
        public const ushort SlashingResistanceDownII = 0x4F8;
        public const ushort ManaHypersensitivity = 0x59A;
        public const ushort Doom = 0xD2;
        public const ushort Firescorched = 0x1D0;
        public const ushort Icebitten = 0x1D1;
        public const ushort Thunderstruck = 0x1D2;
        public const ushort Transcendent = 0x1A2;
        public const ushort PhoenixsBlessing = 0x59C;
        public const ushort EarthResistanceDownII = 0x41D;
    }

    public const uint HeavensfallKnockback = 115;

    public class LockonId
    {
        public const uint Fireball = 0x75;
        public const uint Hatch = 0x76;
        public const uint Divebomb = 0x14;
        public const uint MegaflareStack = 0x27;
        public const uint EarthShaker = 0x28;
        public const uint LiquidHell = 0x29;
        // UNVERIFIED which dive each one marks.
        public const uint MegaflareDive = 0x29;
        public const uint TwistingDive = 0x2A;
    }

    public class EObjId
    {
        public const uint LiquidHell = 2001150;
        public const uint Neurolink = 2001151;
        public const uint Twister = 2001168;
        public const uint WingsOfSalvationCleanse = 2003412;
        public const uint HypernovaPuddle = 2003393;
    }

    public class TetherId
    {
        public const ushort Fireball = 5;
        public const ushort TempestWing = 4;
    }

    // Lohs Daih is a Transfiguration status: its param is the Transformation row the boss
    // takes on, not a stack count (param 1 turns him into a toad).
    public const int LohsDaihTransformation = 73;

    public class TimelineId
    {
        public const ushort NaelLeaves = 0x1E39;
        public const ushort DragonLeaves = 0x008F;
        public const ushort PhoenixAppears = 0x11DE;
        public const ushort BahamutWatchesPhoenix = 0x0C91;
        public const ushort PhoenixRises = 0x1E43;
        public const ushort PhoenixLeaves = 0x1E39;
        public const ushort BahamutInnerRage = 0x11D2;
        public const ushort BahamutLeaves = 0x1E39;
        public const ushort TwintaniaLeaves = 0x1E3A;
        public const ushort DragonArrives = 0x1E43;
        public const ushort TwintaniaArrives = 0x1E44;
        // UNVERIFIED in the sim: Bahamut Prime, Nael and Twintania leave and land between trios.
        public const ushort TrioLeave = 0x1E39;
        public const ushort TwintaniaTrioLeave = 0x1E3A;
        public const ushort TrioLand = 0x1E43;
        public const ushort TwintaniaTrioLand = 0x1E44;
    }

    public const uint MusicDirector = 0x80000004;
    public const uint HeavensfallTrioMusic = 0x176F;

    public class ActionId
    {
        public const uint AutoAttack = 9936;
        public const uint Plummet = 0x26A8;
        public const uint DeathSentence = 0x26A9;
        public const uint Twister = 0x26AA;
        public const uint TwisterBlast = 0x26AB;
        public const uint Fireball = 0x26AC;
        public const uint LiquidHell = 0x26AD;
        public const uint Generate = 0x26AE;
        public const uint Hatch = 0x26AF;
        public const uint DeepHatch = 0x26B0;
        public const uint BahamutsClaw = 0x26B5;
        public const uint Ravensbeak = 0x26B6;
        public const uint Heavensfall = 0x26B8;
        public const uint ThermionicBurst = 0x26B9;
        public const uint IronChariot = 0x26BB;
        public const uint LunarDynamo = 0x26BC;
        public const uint ThermionicBeam = 0x26BD;
        public const uint RavenDive = 0x26BE;
        public const uint MeteorStream = 0x26C0;
        public const uint DalamudDive = 0x26C1;
        public const uint BahamutsFavor = 0x26C2;
        public const uint DragonFireball = 0x26C5;
        public const uint Iceball = 0x26C6;
        public const uint ChainLightning = 0x26C7;
        public const uint ChainLightningSplash = 0x26C8;
        public const uint Deathstorm = 0x26C9;
        public const uint WingsOfSalvation = 0x26CA;
        public const uint Hypernova = 0x26BF;
        public const uint CauterizeFirehorn = 0x26CB;
        public const uint CauterizeIceclaw = 0x26CC;
        public const uint CauterizeThunderwing = 0x26CD;
        public const uint CauterizeTailOfDarkness = 0x26CE;
        public const uint CauterizeFangOfLight = 0x26CF;
        public const uint Exaflare = 9967;       // 0x26EF, boss-body cast, no AOE of its own
        public const uint ExaflareFirst = 9968;  // 0x26F0, helper, 4.0s cast, arrow omen, 6y circle
        public const uint ExaflareRest = 9969;   // 0x26F1, helper, instant, 6y circle
        public const uint AkhMorn = 0x26EA;
        public const uint AkhMornFollowUp = 0x26EB;
        public const uint MornAfah = 0x26EC;
        public const uint MornAfahEnrage = 0x26ED;
        public const uint FlamesOfRebirth = 0x26F2;
        // Unnamed in the sheet: Bahamut Prime's flight from the edge to the centre before Lohs Daih.
        public const uint GoldenDescent = 0x2707;
        public const uint TwistingDive = 0x26B2;
        public const uint LunarDive = 0x26C3;
        public const uint FlareBreath = 0x26D4;
        public const uint Gigaflare = 0x26D6;
        public const uint TempestWing = 0x26D7;
        public const uint TempestWingBlast = 0x26D8;
        public const uint EarthShaker = 0x26D9;
        public const uint EarthShakerCone = 0x26DA;
        public const uint MegaflareSpread = 0x26DC;
        public const uint MegaflarePuddle = 0x26DD;
        public const uint MegaflareStack = 0x26DE;
        public const uint MegaflareTower = 0x26DF;
        public const uint MegaflareStrike = 0x26E0;
        public const uint MegaflareDive = 0x26E1;
        public const uint QuickmarchTrio = 0x26E2;
        public const uint BlackfireTrio = 0x26E3;
        public const uint AethericProfusion = 0x26B1;
        public const uint HeavensfallCast = 0x26B7;
        public const uint Flatten = 0x26D5;
        public const uint TempestWingSplash = 0x26D8;
        public const uint FellruinTrio = 0x26E4;
        public const uint HeavensfallTrio = 0x26E5;
    }
}
