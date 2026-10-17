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
        public const uint Helper = 9020;
    }

    public static class BNpcNameId
    {
        public const uint RedHot = 14370;
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
    }

    public static class TetherId
    {
        public const ushort FloaterSlack = 0x17A;
        public const ushort FloaterTaut = 0x17B;
    }

    public static class Geometry
    {
        public const float ArenaHalfWidth = 20f;
    }
}
