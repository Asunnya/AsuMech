using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.Act3;

public enum Growth { North, East, South, West }

// Each growth points one way for one support and one DPS.
public sealed class M12sAct3State
{
    public const float PlatformHalf = 5f;
    public const float ProjectionAcross = 30f;
    public const float ProjectionAlong = 20f;

    public IReadOnlyDictionary<PartyRole, Growth> Growths { get; }
    public bool CardinalPlatforms { get; }

    public M12sAct3State(Rng rng, M12sAct3StateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M12sAct3StateOverrides();
        var supports = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => !r.IsDps())).ToList();
        var dps = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => r.IsDps())).ToList();
        var cardinal = rng.NextBool();
        if (player is { } me && overrides.PlayerGrowth is { } growth and >= 0 and <= 3)
        {
            var group = me.IsDps() ? dps : supports;
            var at = group.IndexOf(me);
            (group[at], group[growth]) = (group[growth], group[at]);
        }
        var growths = new Dictionary<PartyRole, Growth>();
        for (var i = 0; i < 4; i++)
        {
            growths[supports[i]] = (Growth)i;
            growths[dps[i]] = (Growth)i;
        }
        Growths = growths;
        CardinalPlatforms = overrides.CardinalPlatforms ?? cardinal;
    }

    public static int GrowthParam(Growth growth) => growth switch
    {
        Growth.North => M12sConstants.GrowthParam.North,
        Growth.East => M12sConstants.GrowthParam.East,
        Growth.South => M12sConstants.GrowthParam.South,
        _ => M12sConstants.GrowthParam.West,
    };

    // Where a growth throws its tower from the player standing at `from`.
    public static Vector2 Projected(Vector2 from, Growth growth) => from + growth switch
    {
        Growth.North => new Vector2(0f, -ProjectionAlong),
        Growth.East => new Vector2(ProjectionAcross, 0f),
        Growth.South => new Vector2(0f, ProjectionAlong),
        _ => new Vector2(-ProjectionAcross, 0f),
    };

    public IReadOnlyList<Vector2> Platforms => CardinalPlatforms
        ? [new(0f, -10f), new(15f, 0f), new(0f, 10f), new(-15f, 0f)]
        : [new(15f, -10f), new(15f, 10f), new(-15f, 10f), new(-15f, -10f)];

    public IReadOnlyList<Vector2> Holes => CardinalPlatforms
        ? [new(15f, -10f), new(15f, 10f), new(-15f, 10f), new(-15f, -10f)]
        : [new(0f, -10f), new(15f, 0f), new(0f, 10f), new(-15f, 0f)];

    public static bool OnSquare(IReadOnlyList<Vector2> squares, Vector2 p)
        => squares.Any(c => MathF.Abs(p.X - c.X) <= PlatformHalf && MathF.Abs(p.Y - c.Y) <= PlatformHalf);
}
