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
        public const uint Helper = 9020;
    }

    public static class BNpcNameId
    {
        public const uint RedHot = 14370;
        public const uint DeepBlue = 14369;
        public const uint SickSwell = 14372;
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
    }

    public static class TetherId
    {
        public const ushort FloaterSlack = 0x17A;
        public const ushort FloaterTaut = 0x17B;
        public const ushort WaveWall = 0x174;
    }

    public static class TimelineId
    {
        public const ushort Leave = 0x11DD;
        public const ushort Arrive = 0x11D2;
        public const ushort WaveRises = 0x2487;
    }

    public static class Geometry
    {
        public const float ArenaHalfWidth = 20f;
    }
}
