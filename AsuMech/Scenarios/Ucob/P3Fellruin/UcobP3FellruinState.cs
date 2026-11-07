using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob.P3Fellruin;

public sealed class UcobP3FellruinState
{
    // Nael's third move is always the Meteor Streams.
    public static readonly NaelQuote[] QuoteChoices =
    [
        new("From hallowed moon I descend, a rain of stars to bring!", NaelMove.LunarDynamo, NaelMove.RavenDive),
        // UNVERIFIED: only the first was seen; this one is the guide's.
        new("From on high I descend, the moon and stars to bring!", NaelMove.RavenDive, NaelMove.LunarDynamo),
    ];

    public const float BossDistance = 10f;
    // UNVERIFIED: the three always take these bearings, between the neurolinks; only one layout was seen.
    public static readonly float[] BossBearings = [60f, 180f, 300f];
    public const int MeteorStreams = 4;

    public NaelQuote Quote { get; }
    public int BahamutSlot { get; }
    public bool NaelClockwiseOfBahamut { get; }
    public PartyRole RavenDiveTarget { get; }
    public IReadOnlyList<PartyRole> MeteorTargets { get; }

    public UcobP3FellruinState(Rng rng, UcobP3FellruinStateOverrides? overrides = null)
    {
        overrides ??= new UcobP3FellruinStateOverrides();
        var quote = rng.Next(QuoteChoices.Length);
        var slot = rng.Next(BossBearings.Length);
        var naelClockwise = rng.NextBool();
        var diver = rng.NextRole();
        var meteors = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        Quote = QuoteChoices[overrides.Quote ?? quote];
        BahamutSlot = overrides.BahamutSlot ?? slot;
        NaelClockwiseOfBahamut = overrides.NaelClockwiseOfBahamut ?? naelClockwise;
        RavenDiveTarget = overrides.RavenDiveTarget ?? diver;
        if (overrides.MeteorIncludes is { } picked)
        {
            meteors.Remove(picked);
            meteors.Insert(0, picked);
        }
        MeteorTargets = overrides.MeteorTargets ?? meteors.Take(MeteorStreams).ToList();
    }

    public float BahamutBearing => BossBearings[BahamutSlot];
    public float NaelBearing => BahamutBearing + (NaelClockwiseOfBahamut ? 120f : -120f);
    public float TwintaniaBearing => BahamutBearing + (NaelClockwiseOfBahamut ? -120f : 120f);

    public Vector2 Bahamut => OnBearing(BahamutBearing, BossDistance);
    public Vector2 Nael => OnBearing(NaelBearing, BossDistance);
    public Vector2 Twintania => OnBearing(TwintaniaBearing, BossDistance);

    public static Vector2 NeurolinkNearest(float bearing)
        => Geometry.P3Neurolinks.MinBy(n => Vector2.Distance(Vector2.Normalize(n), OnBearing(bearing, 1f)));

    // Degrees clockwise from north; north is -Z.
    public static Vector2 OnBearing(float degrees, float distance)
    {
        var r = degrees * MathF.PI / 180f;
        return new Vector2(MathF.Sin(r), -MathF.Cos(r)) * distance;
    }

    public static float FacingOf(Vector2 from, Vector2 to) => MathF.Atan2(to.X - from.X, to.Y - from.Y);
}
