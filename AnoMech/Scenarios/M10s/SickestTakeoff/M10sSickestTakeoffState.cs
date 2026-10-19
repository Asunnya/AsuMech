using System.Collections.Generic;
using System.Numerics;

namespace AnoMech.Scenarios.M10s.SickestTakeoff;

public sealed class M10sSickestTakeoffState
{
    public const float LaneOffset = 15f;

    public int WaveFrom { get; }
    public int Lane { get; }
    public bool EightOrbs { get; }
    public bool DoubleDip { get; }

    // Where each protean helper aimed its first cone, by helper.
    public List<float> FirstCones { get; } = [];

    public M10sSickestTakeoffState(Rng rng, M10sSickestTakeoffStateOverrides? overrides = null)
    {
        overrides ??= new M10sSickestTakeoffStateOverrides();
        var wave = rng.Next(4);
        WaveFrom = overrides.WaveFrom ?? wave;
        var lane = rng.Next(3) - 1;
        Lane = overrides.Lane ?? lane;
        var eight = rng.NextBool();
        EightOrbs = overrides.EightOrbs ?? eight;
        var doubleDip = rng.NextBool();
        DoubleDip = overrides.DoubleDip ?? doubleDip;
    }

    // The way the wave pushes, away from the wall it rises at.
    public Vector2 Push => WaveFrom switch
    {
        0 => new Vector2(0f, 1f),
        1 => new Vector2(-1f, 0f),
        2 => new Vector2(0f, -1f),
        _ => new Vector2(1f, 0f),
    };

    // Left, for someone facing back toward the wave.
    public Vector2 Left => new(-Push.Y, Push.X);

    public float PushHeading => System.MathF.Atan2(Push.X, Push.Y);

    public Vector3 FromWaveFrame(float forward, float left)
    {
        var p = Push * forward + Left * left;
        return new Vector3(p.X, 0f, p.Y);
    }

    public Vector3 LaneStart => FromWaveFrame(-25f, Lane * LaneOffset);
}
