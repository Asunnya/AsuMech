using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P1UtopianSky;

// Cyclonic Break and Utopian Sky each fire or lightning; three of the eight images cut a line each,
// one on every axis but the safe one; Sinbound Fire III lands on the tanks or on the healers.
public sealed class FruP1UtopianSkyState
{
    public bool CyclonicFire { get; }
    public bool UtopianFire { get; }
    // 0 N-S, 1 NE-SW, 2 E-W, 3 NW-SE.
    public int SafeAxis { get; }
    // Octants clockwise from north, one per unsafe axis.
    public IReadOnlyList<int> ActiveImages { get; }
    public bool FireOnTanks { get; }

    public FruP1UtopianSkyState(Rng rng, FruP1UtopianSkyStateOverrides overrides)
    {
        var cyclonicFire = rng.NextBool();
        var utopianFire = rng.NextBool();
        var safeAxis = rng.Next(4);
        var farEnds = Enumerable.Range(0, 4).Select(_ => rng.NextBool()).ToList();
        var fireOnTanks = rng.NextBool();
        CyclonicFire = overrides.CyclonicFire ?? cyclonicFire;
        UtopianFire = overrides.UtopianFire ?? utopianFire;
        SafeAxis = overrides.SafeAxis ?? safeAxis;
        ActiveImages = Enumerable.Range(0, 4).Where(axis => axis != SafeAxis)
            .Select(axis => axis + (farEnds[axis] ? 4 : 0)).ToList();
        FireOnTanks = overrides.FireOnTanks ?? fireOnTanks;
    }

    public static Vector3 Point(float octant, float radius)
        => new(MathF.Sin(octant * MathF.PI / 4) * radius, 0, -MathF.Cos(octant * MathF.PI / 4) * radius);

    public static Vector3 ImagePosition(int octant) => Point(octant, 20);

    public IEnumerable<PartyRole> FireTargets => FireOnTanks
        ? [PartyRole.MainTank, PartyRole.OffTank]
        : [PartyRole.RegenHealer, PartyRole.ShieldHealer];
}
