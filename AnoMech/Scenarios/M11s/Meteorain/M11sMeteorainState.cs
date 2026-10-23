using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M11s.Meteorain;

public sealed class M11sMeteorainState
{
    public const int MeteorSets = 3;
    public const int Fireballs = 4;
    private const float ShadeDistance = 25f;
    private const float PairingTie = 1f;

    public IReadOnlyList<(int First, int Second)> ShadeSides { get; }
    // Who each Fearsome Fireball picks, first in this order who holds no meteor.
    public IReadOnlyList<PartyRole> FireballPriority { get; }

    // Filled as the fight goes: where each meteor came down.
    public List<Vector3> Meteors { get; } = [];

    public M11sMeteorainState(Rng rng, M11sMeteorainStateOverrides? overrides = null)
    {
        overrides ??= new M11sMeteorainStateOverrides();
        var sides = Enumerable.Range(0, MeteorSets).Select(_ =>
        {
            var pair = rng.Shuffle(Enumerable.Range(0, 4)).Take(2).ToList();
            return (pair[0], pair[1]);
        }).ToList();
        var priority = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => !r.IsTank())).ToList();
        ShadeSides = overrides.ShadeSides ?? sides;
        FireballPriority = priority;
    }

    // The shades tether the two meteors still standing nearest the boss, each shade the one that
    // pairs them shortest overall; on a near tie the first shade takes the meteor nearer it.
    // UNVERIFIED: which shade takes which meteor.
    public IReadOnlyList<(int Side, Vector3 Meteor)> TetheredMeteors(int set) => TetheredMeteors(set, Meteors);

    public IReadOnlyList<(int Side, Vector3 Meteor)> TetheredMeteors(int set, IEnumerable<Vector3> standing)
    {
        var nearest = standing.OrderBy(m => m.LengthSquared()).Take(2).ToList();
        var (a, b) = ShadeSides[set];
        if (nearest.Count == 0) return [];
        if (nearest.Count == 1) return [(a, nearest[0])];
        var straight = Vector3.Distance(ShadeSpot(a), nearest[0]) + Vector3.Distance(ShadeSpot(b), nearest[1]);
        var crossed = Vector3.Distance(ShadeSpot(a), nearest[1]) + Vector3.Distance(ShadeSpot(b), nearest[0]);
        var keepOrder = MathF.Abs(straight - crossed) < PairingTie
            ? Vector3.Distance(ShadeSpot(a), nearest[0]) <= Vector3.Distance(ShadeSpot(a), nearest[1])
            : straight < crossed;
        return keepOrder ? [(a, nearest[0]), (b, nearest[1])] : [(a, nearest[1]), (b, nearest[0])];
    }

    public static Vector3 ShadeSpot(int side) => side switch
    {
        0 => new Vector3(0f, 0f, -ShadeDistance),
        1 => new Vector3(ShadeDistance, 0f, 0f),
        2 => new Vector3(0f, 0f, ShadeDistance),
        _ => new Vector3(-ShadeDistance, 0f, 0f),
    };
}
