using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.ArenaSplit;

public sealed class M10sArenaSplitState
{
    public const float LaneOffset = 15f;
    public const float DeepBlueOffCentre = 12f;
    public static readonly int[] WaveWalls = [0, 1, 3];

    // -1 when Deep Blue fires its proteans from the west half, +1 from the east.
    public int DeepBlueSide { get; }
    public bool DoubleDip { get; }
    // Wall the wave comes from: 0 north, 1 east, 3 west.
    public int WaveFrom { get; }
    public int Lane { get; }
    public IReadOnlySet<PartyRole> PyrotationTargets { get; }

    // Bearings Deep Blue's helpers aimed each set of cones at.
    public List<float> FirstCones { get; } = [];
    public List<float> SecondCones { get; } = [];
    public List<Vector3> InfernoPuddles { get; } = [];

    public M10sArenaSplitState(Rng rng, M10sArenaSplitStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M10sArenaSplitStateOverrides();
        var west = rng.NextBool();
        DeepBlueSide = (overrides.DeepBlueWest ?? west) ? -1 : 1;
        var doubleDip = rng.NextBool();
        DoubleDip = overrides.DoubleDip ?? doubleDip;
        var wave = WaveWalls[rng.Next(WaveWalls.Length)];
        WaveFrom = overrides.WaveFrom ?? wave;
        var side = rng.NextBool() ? 1 : -1;
        Lane = WaveFrom == 0 ? 0 : side;

        var targets = new HashSet<PartyRole>();
        foreach (var (first, second) in Pairs)
        {
            var pick = rng.NextBool() ? first : second;
            if (player is { } me && overrides.PlayerPyrotation is { } marked && (me == first || me == second))
                pick = marked == (me == first) ? first : second;
            targets.Add(pick);
        }
        PyrotationTargets = targets;
    }

    // UNVERIFIED: Freaky Pyrotation marks one player of each pair.
    public static readonly (PartyRole, PartyRole)[] Pairs =
    [
        (PartyRole.MainTank, PartyRole.PhysRangedDps),
        (PartyRole.OffTank, PartyRole.CasterDps),
        (PartyRole.RegenHealer, PartyRole.MeleeDpsA),
        (PartyRole.ShieldHealer, PartyRole.MeleeDpsB),
    ];

    public static PartyRole Partner(PartyRole role)
    {
        foreach (var (first, second) in Pairs)
        {
            if (first == role) return second;
            if (second == role) return first;
        }
        return role;
    }

    public static int GroupSide(PartyRole role) =>
        role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.PhysRangedDps ? -1 : 1;

    public Vector3 DeepBlueSpot => new(DeepBlueOffCentre * DeepBlueSide, 0f, 0f);

    // The way the wave pushes, away from the wall it rises at.
    public Vector2 Push => WaveFrom switch
    {
        0 => new Vector2(0f, 1f),
        1 => new Vector2(-1f, 0f),
        _ => new Vector2(1f, 0f),
    };

    // Left, for someone facing back toward the wave.
    public Vector2 Left => new(-Push.Y, Push.X);

    public float PushHeading => MathF.Atan2(Push.X, Push.Y);

    public Vector3 FromWaveFrame(float forward, float left)
    {
        var p = Push * forward + Left * left;
        return new Vector3(p.X, 0f, p.Y);
    }

    public Vector3 LaneCentre => FromWaveFrame(0f, Lane * LaneOffset);

    public Vector3 LaneStart => FromWaveFrame(-25f, Lane * LaneOffset);
}
