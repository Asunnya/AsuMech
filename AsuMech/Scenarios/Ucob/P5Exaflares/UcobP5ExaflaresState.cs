using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;

namespace AsuMech.Scenarios.Ucob.P5Exaflares;

// Per-run randomization: one Exaflare set (UcobExaflarePattern) cast 3s in.
public sealed class UcobP5ExaflaresState
{
    public const float HitRadius = UcobExaflarePattern.HitRadius;
    public const float BossCastAt = 3f;
    public const float FirstTelegraphAt = BossCastAt + UcobExaflarePattern.TelegraphAfterCast;
    public static IReadOnlyList<float> LaneOffsets => UcobExaflarePattern.LaneOffsets;

    public UcobExaflarePattern Pattern { get; }
    public Direction Direction => Pattern.Direction;
    public IReadOnlyList<float> LaneOrder => Pattern.LaneOrder;
    public Vector3 Travel => Pattern.Travel;
    public Vector3 Lateral => Pattern.Lateral;
    public IReadOnlyList<ExaflareLine> Lines => Pattern.Lines;
    public IReadOnlyList<ExaflareHit> Hits => Pattern.Hits;
    public float LastHitAt => Pattern.LastHitAt;

    public UcobP5ExaflaresState(Rng rng, UcobP5ExaflaresStateOverrides overrides)
    {
        Pattern = UcobExaflarePattern.Roll(rng, BossCastAt, overrides.Direction, overrides.LaneOrder);
    }

    private UcobP5ExaflaresState(UcobExaflarePattern pattern)
    {
        Pattern = pattern;
    }

    public static UcobP5ExaflaresState? FromNetworkReplay(float directionRadians, IReadOnlyList<float> laneOrder)
    {
        if (laneOrder.Count != LaneOffsets.Count) return null;
        if (!float.IsFinite(directionRadians) || laneOrder.Any(o => !LaneOffsets.Contains(o))) return null;
        return new UcobP5ExaflaresState(new UcobExaflarePattern(BossCastAt, new Direction(directionRadians), laneOrder));
    }
}
