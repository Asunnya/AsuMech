using System;
using System.Collections.Generic;
using System.Numerics;

namespace AsuMech.Scenarios.M12s.Replication1;

// Cardinals count clockwise from north (0 N, 1 E, 2 S, 3 W), intercardinals from north-east (0 NE, 1 SE, 2 SW, 3 NW).
public sealed class M12sReplication1State
{
    public const float CloneRing = 14f;
    public const float OuterLine = 14f;
    public const float InnerLine = 7.07f;
    public const float SnakingKickTurn = 2.6f;

    // The fire clone's cardinal; the dark clone stands opposite, the two cleave clones on the other axis.
    public int FireCardinal { get; }
    public int DarkCardinal => (FireCardinal + 2) % 4;
    // Both sets cleave the same way: north and south, or east and west.
    public bool VerticalCleaves { get; }
    // The second set lines up fire, dark, fire, dark across one diagonal; this is the outer fire clone's end.
    public int OuterFireIntercard { get; }
    public int OuterDarkIntercard => (OuterFireIntercard + 2) % 4;
    // UNVERIFIED: what turns the boss to either side of north; both have been seen.
    public float SnakingKickHeading { get; }
    // Quarter turns from Double Sobat's kick to the follow-up, set by which of its four kicks the boss uses.
    public int SobatTurns { get; }

    public M12sReplication1State(Rng rng, M12sReplication1StateOverrides? overrides = null)
    {
        overrides ??= new M12sReplication1StateOverrides();
        var fire = rng.Next(4);
        var vertical = rng.NextBool();
        var outerFire = rng.Next(4);
        var kickEast = rng.NextBool();
        var turns = rng.Next(4);
        FireCardinal = overrides.FireCardinal ?? fire;
        VerticalCleaves = overrides.VerticalCleaves ?? vertical;
        OuterFireIntercard = overrides.OuterFireIntercard ?? outerFire;
        SnakingKickHeading = (overrides.SnakingKickEast ?? kickEast) ? SnakingKickTurn : -SnakingKickTurn;
        SobatTurns = overrides.SobatTurns ?? turns;
    }

    public Vector3 FireClone => Cardinal(FireCardinal) * CloneRing;
    public Vector3 DarkClone => Cardinal(DarkCardinal) * CloneRing;
    public IReadOnlyList<Vector3> CleaveClones => [Cardinal((FireCardinal + 1) % 4) * CloneRing, Cardinal((FireCardinal + 3) % 4) * CloneRing];

    public Vector3 OuterFire => Intercard(OuterFireIntercard) * OuterLine;
    public Vector3 InnerFire => Intercard(OuterDarkIntercard) * InnerLine;
    public Vector3 OuterDark => Intercard(OuterDarkIntercard) * OuterLine;
    public Vector3 InnerDark => Intercard(OuterFireIntercard) * InnerLine;
    public IReadOnlyList<Vector3> SecondCleaveClones =>
    [
        Intercard((OuterFireIntercard + 1) % 4) * OuterLine, Intercard((OuterFireIntercard + 1) % 4) * InnerLine,
        Intercard((OuterFireIntercard + 3) % 4) * InnerLine, Intercard((OuterFireIntercard + 3) % 4) * OuterLine,
    ];

    public IReadOnlyList<float> CleaveHeadings => VerticalCleaves ? [0f, MathF.PI] : [MathF.PI / 2f, -MathF.PI / 2f];

    public static Vector3 Cardinal(int index) => index switch
    {
        0 => new Vector3(0f, 0f, -1f),
        1 => new Vector3(1f, 0f, 0f),
        2 => new Vector3(0f, 0f, 1f),
        _ => new Vector3(-1f, 0f, 0f),
    };

    public static Vector3 Intercard(int index)
    {
        var east = index is 0 or 1 ? 1f : -1f;
        var south = index is 1 or 2 ? 1f : -1f;
        return Vector3.Normalize(new Vector3(east, 0f, south));
    }

    public static Vector3 Toward(float heading) => new(MathF.Sin(heading), 0f, MathF.Cos(heading));
}
