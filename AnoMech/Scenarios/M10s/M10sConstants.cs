using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Scenarios.M10s;

public static class M10sConstants
{
    public const byte Level = 100;
    public const uint DirectorEventId = 0x800375CD;

    public static IReadOnlyList<Waymark> ToxicWaymarks =>
    [
        new(WaymarkSlot.A,     new Vector3(  0.02f, 0f, -14.2f)),
        new(WaymarkSlot.B,     new Vector3( 14.84f, 0f,  -0.08f)),
        new(WaymarkSlot.C,     new Vector3(  0f,    0f,  14.4f)),
        new(WaymarkSlot.D,     new Vector3(-14.99f, 0f,  -0.1f)),
        new(WaymarkSlot.One,   new Vector3(-11.83f, 0f, -11.73f)),
        new(WaymarkSlot.Two,   new Vector3( 11.86f, 0f, -11.91f)),
        new(WaymarkSlot.Three, new Vector3( 11.95f, 0f,  12.02f)),
        new(WaymarkSlot.Four,  new Vector3(-11.76f, 0f,  11.61f)),
    ];

    public static class BNpcBaseId
    {
        public const uint RedHot = 19287;
        public const uint DeepBlue = 19288;
        public const uint SickSwell = 19290;
        public const uint XtremeAether = 19289;
        public const uint WateryGrave = 19136;
        public const uint Helper = 9020;
    }

    public static class BNpcNameId
    {
        public const uint RedHot = 14370;
        public const uint DeepBlue = 14369;
        public const uint SickSwell = 14372;
        public const uint XtremeAether = 14371;
        public const uint WateryGrave = 14373;
    }

    public static class EObjId
    {
        public const uint FloaterFire = 2015024;
        public const uint InfernoFire = 2015025;
        public const uint PyrotationFire = 2015026;
        public const uint CutbackFire = 2015027;
    }

    public static class ActionId
    {
        public const uint RedHotAttack = 0xBDFF;
        public const uint Leap = 0xB578;
        public const uint Landing = 0xB57C;
        public const uint HotImpact = 0xB5B6;
        public const uint DiversDare = 0xB5B8;
        public const uint FlameFloater = 0xB5BA;
        public const uint FlameFloaterFirst = 0xB5BB;
        public const uint FlameFloaterSecond = 0xB5BC;
        public const uint FlameFloaterThird = 0xB5BD;
        public const uint FlameFloaterFourth = 0xB5BE;
        public const uint AlleyOopInferno = 0xB5C0;
        public const uint AlleyOopInfernoSpread = 0xB5C1;
        public const uint Pyrotation = 0xB5C2;
        public const uint PyrotationStack = 0xB5C3;
        public const uint CutbackBlaze = 0xB5C9;
        public const uint CutbackBlazeCleave = 0xB5CA;
        public const uint DeepBlueAttack = 0xBE00;
        public const uint DeepBlueLeap = 0xB579;
        public const uint SickSwell = 0xB5CB;
        public const uint SickSwellWave = 0xB5CC;
        public const uint SickestTakeoff = 0xB5CD;
        public const uint SickestTakeoffLane = 0xB5CE;
        public const uint AwesomeSplash = 0xB5CF;
        public const uint AwesomeSlab = 0xB5D0;
        public const uint DoubleDip = 0xB5DD;
        public const uint DoubleDipFirst = 0xB5DE;
        public const uint DoubleDipSecond = 0xB5DF;
        public const uint ReverseAlleyOop = 0xB5E0;
        public const uint ReverseAlleyOopFirst = 0xB5E1;
        public const uint ReverseAlleyOopSecond = 0xB5E2;
        public const uint DeepImpact = 0xB5B7;
        public const uint DeepImpactHit = 0xADC6;
        public const uint DiversDareBlue = 0xB5B9;
        public const uint XtremeSpectacularRed = 0xB5D9;
        public const uint XtremeSpectacularBlue = 0xB5DA;
        public const uint XtremeSpectacular = 0xB5A4;
        public const uint XtremeSpectacularHit = 0xB5DC;
        public const uint XtremeSpectacularLast = 0xB7CA;
        public const uint EpicBrotherhood = 0xB57A;
        public const uint EpicBrotherhoodBlue = 0xB57B;
        public const uint InsaneAirRedFirst = 0xB897;
        public const uint InsaneAirBlueFirst = 0xB898;
        public const uint InsaneAirRed = 0xB899;
        public const uint InsaneAirBlue = 0xB89A;
        public const uint BlastingSnap = 0xB5EF;
        public const uint PlungingSnap = 0xB5F0;
        public const uint BlastingSnapCone = 0xB5F1;
        public const uint PlungingSnapCone = 0xB5F2;
        public const uint ReentryBlast = 0xB5F3;
        public const uint ReentryPlunge = 0xB5F4;
        public const uint ReentryBlastCone = 0xB5F5;
        public const uint ReentryPlungeCone = 0xB5F6;
        public const uint VerticalBlast = 0xB5F7;
        public const uint VerticalPlunge = 0xB5F8;
        public const uint VerticalBlastHit = 0xB5F9;
        public const uint VerticalPlungeHit = 0xB5FA;
        public const uint Firesnaking = 0xB381;
        public const uint Watersnaking = 0xB382;
        public const uint HotImpactOnFire = 0xB580;
        public const uint DeepVarial = 0xB891;
        public const uint DeepVarialCone = 0xB5D3;
        public const uint AwesomeSlabSmallWave = 0xB5D8;
        public const uint AwesomeSplashSmallWave = 0xB5D7;
        public const uint SteamBurst = 0xB5FB;
        public const uint HotAerial = 0xB5C4;
        public const uint HotAerialJump = 0xB91D;
        public const uint HotAerialFirst = 0xB91E;
        public const uint HotAerialSecond = 0xB91F;
        public const uint HotAerialThird = 0xB920;
        public const uint HotAerialFourth = 0xB921;
        public const uint DeepAerial = 0xB5E3;
        public const uint DeepAerialTower = 0xB5E4;
        public const uint UnmitigatedExplosion = 0xB5E5;
        public const uint XtremeWaveRedFirst = 0xB5C5;
        public const uint XtremeWaveBlueFirst = 0xB5C6;
        public const uint XtremeWaveRed = 0xB5C7;
        public const uint XtremeWaveBlue = 0xB5C8;
        public const uint XtremeWaveFire = 0xB5D1;
        public const uint XtremeWaveWater = 0xB5D2;
        public const uint ScathingSteam = 0xADC7;
        public const uint FlameFloaterSplit = 0xB5D4;
        public const uint FlameFloaterSplitLine = 0xB5BF;
        public const uint FreakyPyrotation = 0xB596;
        public const uint FreakyPyrotationStack = 0xB597;
        public const uint XtremeFiresnaking = 0xB5AE;
        public const uint XtremeWatersnaking = 0xB5AF;
        public const uint InsaneAirTwoRedFirst = 0xB5E6;
        public const uint InsaneAirTwoBlueFirst = 0xB5E7;
        public const uint InsaneAirTwoRed = 0xB5E8;
        public const uint InsaneAirTwoBlue = 0xB5E9;
        public const uint BailoutFire = 0xB5B0;
        public const uint BailoutWater = 0xB5B1;
    }

    public static class StatusId
    {
        public const ushort FirstInLine = 0xBBC;
        public const ushort SecondInLine = 0xBBD;
        public const ushort ThirdInLine = 0xBBE;
        public const ushort FourthInLine = 0xD7B;
        public const ushort FireResistanceDownII = 0xB79;
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort Burns = 0xBF9;
        public const ushort DirectionalDisregard = 0xEE0;
        // Its param is the orb count Deep Blue shows: 0x3ED two, 0x3EE eight.
        public const ushort Orbs = 0x808;
        public const ushort DamageDown = 0xB5F;
        public const ushort Firesnaking = 0x136E;
        public const ushort Watersnaking = 0x136F;
        public const ushort WateryGrave = 0x12DD;
        public const ushort Stun = 0xA60;
        public const ushort XtremeFiresnaking = 0x12DB;
        public const ushort XtremeWatersnaking = 0x12DC;
    }

    public static class LockonId
    {
        public const uint Spread = 0x294;
        public const uint SharedBuster = 0x103;
        public const uint FireWave = 0x27C;
        public const uint WaterWave = 0x27B;
        public const uint FreakyPyrotation = 0x293;
        public const uint Pyrotation = 0x29A;
    }

    public static class TetherId
    {
        public const ushort FloaterSlack = 0x17A;
        public const ushort FloaterTaut = 0x17B;
        public const ushort WaveWall = 0x174;
        public const ushort WaveSlack = 0x39;
        public const ushort WaveTaut = 0x11;
    }

    public static class TimelineId
    {
        public const ushort Leave = 0x11DD;
        public const ushort Arrive = 0x11D2;
        public const ushort RedHotArrive = 0x11D1;
        public const ushort AetherRises = 0x11D4;
        public const ushort WaveRises = 0x2487;
    }

    public static class MapEffect
    {
        public const byte ArenaSlot = 0x00;
        public const uint SpectacularOn = 0x00800040;
        public const uint SpectacularOff = 0x02000100;
        public const byte OrbsSlot = 0x04;
        public const uint OneOrb = 0x00800040;
        public const uint FourOrbs = 0x08000400;
        public const uint OrbsOff = 0x10000004;
        public const byte TowerSlot = 0x01;
        public const uint TowerShows = 0x00200010;
        public const uint TowerResolves = 0x00800040;
        public const byte GraveSlot = 0x17;
        public const uint GraveRises = 0x00020001;
        public const uint GraveSteams = 0x00080001;
        public const uint GraveBreaks = 0x00400004;
    }

    public static class Geometry
    {
        public const float ArenaHalfWidth = 20f;
    }
}
