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
        public const uint LindwurmP2 = 19202;
        public const uint Lindschrat = 19204;
        public const uint Understudy = 19210;
        public const uint BlackHole = 19205;
        public const uint WaterSphere = 19206;
        public const uint AeroSphere = 19207;
        public const uint ThunderSphere = 19208;
        public const uint FireSphere = 19209;
    }

    public static class BNpcNameId
    {
        public const uint LindwurmP1 = 14378;
        public const uint LindwurmP2 = 14379;
        public const uint Lindschrat = 14380;
        public const uint ManaSphere = 14382;
        public const uint Understudy = 14383;
    }

    public static class ActionId
    {
        public const uint Replication = 0xB4D8;
        public const uint Dash = 0xB4D9;
        public const uint Staging = 0xB4E1;
        public const uint FirefallSplashCast = 0xB4E3;
        public const uint FirefallSplashJump = 0xB4E4;
        public const uint ScaldingWaves = 0xB4E5;
        public const uint ManaBurstVisual = 0xB4E6;
        public const uint ManaBurst = 0xB4E7;
        public const uint HeavySlam = 0xB4E8;
        public const uint GrotesquerieVisual = 0xB4E9;
        public const uint Grotesquerie = 0xB4EA;
        public const uint HemorrhagicProjection = 0xB4EB;
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
        public const uint SnakingKick = 0xB527;

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
        public const uint DramaticLysis = 0xB507;
        public const uint UnmitigatedExplosion = 0xB508;
        public const uint NetherworldNear = 0xB52B;
        public const uint NetherworldFar = 0xB52C;
        public const uint WailingWave = 0xB52D;
        public const uint BlackHolePulse = 0xBCB0;

        public const uint DoubleSobat = 0xB520;
        // The buster's four variants, each turning the repeat by its own angle.
        public const uint DoubleSobatBuster = 0xB521;
        public const uint DoubleSobatRepeat = 0xB525;
        public const uint EsotericFinisher = 0xB526;
        public const uint ArcadiaAflame = 0xB528;
    }

    public static class StatusId
    {
        public const ushort MagicVulnerabilityUp = 2941;
        public const ushort MutationAlpha = 4769;
        public const ushort MutatingCells = 4770;
        public const ushort MutationBeta = 4771;
    }

    public static class TetherId
    {
        public const ushort CloneCone = 0x16F;
        public const ushort CloneDefamation = 0x170;
        public const ushort CloneStack = 0x171;
        public const ushort Locked = 0x175;
        public const ushort BossJump = 0x176;
    }

    public static class LockonId
    {
        public const uint DoubleSobat = 0x256;
    }

    public static class BattleTalk
    {
        public const uint Speaker = 0x330A;
        public const uint NoActor = 0xE0000000;
    }
}
