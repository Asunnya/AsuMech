using System;
using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// The markers a strat plays on, and so where Twintania is dragged and her neurolinks stay for the
// rest of the fight. Elemental's triangle (1 NW, 2 NE, 3 S) is the Aether one (N, SW, SE) mirrored
// across the NNW-SSE line, so a scenario's Aether spot maps to the strat's through Place.
public sealed class UcobMarkers
{
    public static readonly UcobMarkers Aether = new(AetherWaymarks, mirrored: false);
    public static readonly UcobMarkers Elemental = new(ElementalWaymarks, mirrored: true);

    private static readonly Vector2 MirrorAxis = new(MathF.Sin(MathF.PI * 5f / 6f), -MathF.Cos(MathF.PI * 5f / 6f));

    private readonly bool mirrored;

    private UcobMarkers(IReadOnlyList<Waymark> waymarks, bool mirrored)
    {
        Waymarks = waymarks;
        this.mirrored = mirrored;
    }

    public IReadOnlyList<Waymark> Waymarks { get; }

    public static UcobMarkers Of(IReadOnlyList<IScenarioAi> strats, int? selectedAi)
        => selectedAi is { } i && i >= 0 && i < strats.Count && strats[i] is IUcobStrat strat ? strat.Markers : Aether;

    public Vector2 Place(Vector2 aetherSpot)
        => mirrored ? 2f * Vector2.Dot(aetherSpot, MirrorAxis) * MirrorAxis - aetherSpot : aetherSpot;

    // A rotation as Placement uses it: facing (sin r, cos r) in X/Z.
    public float Face(float aetherRotation)
    {
        if (!mirrored) return aetherRotation;
        var facing = Place(new Vector2(MathF.Sin(aetherRotation), MathF.Cos(aetherRotation)));
        return MathF.Atan2(facing.X, facing.Y);
    }

    public Vector2[] Place(IReadOnlyList<Vector2> aetherSpots)
    {
        var spots = new Vector2[aetherSpots.Count];
        for (var i = 0; i < spots.Length; i++) spots[i] = Place(aetherSpots[i]);
        return spots;
    }
}

// A UCoB strat that plays on markers other than the Aether ones.
public interface IUcobStrat
{
    UcobMarkers Markers { get; }
}
