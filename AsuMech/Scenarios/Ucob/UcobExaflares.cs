using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Ucob;

// One rolling eruption: where it lands, when it lands, and when its lane arrow went up
// (KnownAt): bots may only dodge what has already been telegraphed.
public sealed record ExaflareHit(Vector3 Position, float Time, float KnownAt);

// One lane of fire: six eruptions marching Advance yalms along Rotation, starting at Start.
public sealed record ExaflareLine(Vector3 Start, float Rotation, float TelegraphAt, IReadOnlyList<ExaflareHit> Hits);

// One Exaflare cast of Bahamut Prime and the pattern it resolves to.
//
// Six parallel lanes cross the arena, all travelling the same way, spaced 8y apart at
// perpendicular offsets -20/-12/-4/+4/+12/+20. Each lane erupts six times, 8y apart, from 20y
// behind the arena to 20y past it. They fire as three pairs 3s apart, and which two lanes make
// up a pair is randomized per set — so the dodge is "stand on a lane that has already burned,
// or on one that is not in this pair", never "find the gap" (adjacent lanes are 8y apart and
// each blast is 6y, so the lanes overlap).
//
// Each lane's first eruption is the release of its own arrow-omen cast (ExaflareFirst), so the
// telegraph and hit 1 cannot drift apart; the five follow-ups are instant ExaflareRest casts.
// The second eruption trails the first by ~1.6s, longer than the 1.476s between later steps.
public sealed class UcobExaflarePattern
{
    public const float HitRadius = 6f;              // ExaflareFirst/Rest EffectRange
    public const float Advance = 8f;
    public const float RollInterval = 1.476f;
    public const int HitsPerLine = 6;
    public const float StartDistance = 20f;
    public const float TelegraphSeconds = 3.97f;    // arrow omen up to its own release (the first eruption)
    public const float SecondHitSeconds = 5.58f;    // arrow omen up to the second eruption, the first ExaflareRest

    public const int PairCount = 3;
    public const float PairInterval = 3f;
    public const float TelegraphAfterCast = 2.01f;

    // Perpendicular offsets of the six lanes, in firing-agnostic order.
    public static readonly IReadOnlyList<float> LaneOffsets = [-20f, -12f, -4f, 4f, 12f, 20f];

    public float BossCastAt { get; }
    public float FirstTelegraphAt => BossCastAt + TelegraphAfterCast;
    public Direction Direction { get; }

    // The lanes in firing order, two per pair.
    public IReadOnlyList<float> LaneOrder { get; }

    // Set axes: Travel is where the lanes roll, Lateral the axis the lane offsets sit on.
    public Vector3 Travel { get; }
    public Vector3 Lateral { get; }
    public IReadOnlyList<ExaflareLine> Lines { get; }
    public IReadOnlyList<ExaflareHit> Hits { get; }
    public float LastHitAt { get; }

    public static UcobExaflarePattern Roll(Rng rng, float bossCastAt, Direction? direction = null, IReadOnlyList<float>? laneOrder = null)
    {
        var rolledDirection = direction ?? rng.NextDirection();
        var order = laneOrder ?? rng.Shuffle(LaneOffsets.ToArray()).ToList();
        return new UcobExaflarePattern(bossCastAt, rolledDirection, order);
    }

    public UcobExaflarePattern(float bossCastAt, Direction direction, IReadOnlyList<float> laneOrder)
    {
        BossCastAt = bossCastAt;
        Direction = direction;
        LaneOrder = laneOrder;

        var theta = Direction.RadiansFromNorth;
        Travel = new Vector3(MathF.Sin(theta), 0f, -MathF.Cos(theta));
        Lateral = new Vector3(MathF.Cos(theta), 0f, MathF.Sin(theta));
        // Placement rotation faces +Z at 0, so a compass bearing is its mirror.
        var rotation = MathF.PI - theta;

        var lines = new List<ExaflareLine>(laneOrder.Count);
        var hits = new List<ExaflareHit>(laneOrder.Count * HitsPerLine);
        for (var i = 0; i < laneOrder.Count; i++)
        {
            var line = BuildLine(rotation, laneOrder[i], FirstTelegraphAt + i / 2 * PairInterval);
            lines.Add(line);
            hits.AddRange(line.Hits);
        }

        Lines = lines;
        Hits = hits;
        LastHitAt = FirstTelegraphAt + (PairCount - 1) * PairInterval
                    + SecondHitSeconds + (HitsPerLine - 2) * RollInterval;
    }

    private ExaflareLine BuildLine(float rotation, float offset, float telegraphAt)
    {
        var start = Travel * -StartDistance + Lateral * offset;
        var hits = new List<ExaflareHit>(HitsPerLine);
        for (var i = 0; i < HitsPerLine; i++)
            hits.Add(new ExaflareHit(
                start + Travel * (Advance * i),
                telegraphAt + (i == 0 ? TelegraphSeconds : SecondHitSeconds + (i - 1) * RollInterval),
                telegraphAt));

        return new ExaflareLine(start, rotation, telegraphAt, hits);
    }

    // The boss cast and every lane: the arrow omen rides ExaflareFirst's own cast at the lane's
    // first step, and every later step is an instant cast.
    public void Schedule(SimWorld world, Func<SimEnemy?> bahamut)
    {
        world.Events.Add(BossCastAt, () => bahamut()?.Cast(UcobActions.Exaflare));
        foreach (var line in Lines)
        {
            world.Events.Add(line.TelegraphAt, () => bahamut()?.CastFromHelper(UcobActions.ExaflareFirst, at: new Placement(line.Start, line.Rotation)));
            foreach (var hit in line.Hits.Skip(1))
                world.Events.Add(hit.Time, () => bahamut()?.CastFromHelper(UcobActions.ExaflareRest, at: new Placement(hit.Position, line.Rotation)));
        }
    }
}
