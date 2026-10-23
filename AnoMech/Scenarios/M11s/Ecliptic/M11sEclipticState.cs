using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M11s.Ecliptic;

public sealed class M11sEclipticState
{
    public const float TowerOffset = 7.78f;
    public const float ShadeDistance = 25f;
    public const float CornerOffset = 18f;

    // Intercards clockwise from north: 0 north-east, 1 south-east, 2 south-west, 3 north-west.
    public static Vector3 Intercard(int index, float offset) => index switch
    {
        0 => new Vector3(offset, 0f, -offset),
        1 => new Vector3(offset, 0f, offset),
        2 => new Vector3(-offset, 0f, offset),
        _ => new Vector3(-offset, 0f, -offset),
    };

    // Cardinals: 0 north, 1 east, 2 south, 3 west.
    public static Vector3 Cardinal(int index, float distance) => index switch
    {
        0 => new Vector3(0f, 0f, -distance),
        1 => new Vector3(distance, 0f, 0f),
        2 => new Vector3(0f, 0f, distance),
        _ => new Vector3(-distance, 0f, 0f),
    };

    public bool MammothNortheast { get; }
    // The two intercards whose tower takes one (Cosmic Kiss); the other two take two.
    public IReadOnlyList<int> SingleTowers { get; }
    // Which of a pair tower's two soakers, in role order, gets the tether.
    public IReadOnlyList<bool> PairTetherFirst { get; }
    // The cardinal shade that tethers each intercard tower's soaker.
    public IReadOnlyList<int> ShadeForTower { get; }
    public IReadOnlyList<PartyRole> MarkPriority { get; }

    public M11sEclipticState(Rng rng, M11sEclipticStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M11sEclipticStateOverrides();
        var mammoth = rng.NextBool();
        var singles = rng.Shuffle(Enumerable.Range(0, 4)).Take(2).OrderBy(i => i).ToList();
        var pairFirst = new[] { rng.NextBool(), rng.NextBool() };
        var shades = rng.Shuffle(Enumerable.Range(0, 4)).ToList();
        var priority = rng.Shuffle(Enum.GetValues<PartyRole>()).ToList();
        if (player is { } me && overrides.PlayerMarked is { } marked)
        {
            priority.Remove(me);
            if (marked) priority.Insert(0, me);
            else priority.Add(me);
        }
        MammothNortheast = overrides.MammothNortheast ?? mammoth;
        SingleTowers = singles;
        PairTetherFirst = pairFirst;
        ShadeForTower = shades;
        MarkPriority = priority;
    }

    public IEnumerable<Vector3> MammothSpots() => MammothNortheast
        ? [Intercard(0, 15f), Intercard(2, 15f)]
        : [Intercard(3, 15f), Intercard(1, 15f)];

    // The intercards the meteors leave alone.
    public IReadOnlyList<int> SafeCorners => MammothNortheast ? [3, 1] : [0, 2];

    public bool IsSingleTower(int index) => SingleTowers.Contains(index);

    // A tether stretches across the arena from its shade, then turns clockwise into the corner.
    public static int CornerForShade(int shade) => shade switch
    {
        0 => 2,
        1 => 3,
        2 => 0,
        _ => 1,
    };
}
