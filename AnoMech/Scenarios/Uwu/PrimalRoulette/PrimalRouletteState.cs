using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Uwu.PrimalRoulette;

public enum Primal
{
    Garuda,
    Ifrit,
    Titan,
}

public sealed class PrimalRouletteState
{
    // Only these four orders have ever come up.
    public static readonly Primal[][] Orders =
    [
        [Primal.Ifrit, Primal.Titan, Primal.Garuda],
        [Primal.Titan, Primal.Ifrit, Primal.Garuda],
        [Primal.Garuda, Primal.Ifrit, Primal.Titan],
        [Primal.Ifrit, Primal.Garuda, Primal.Titan],
    ];

    public static readonly float[] SegmentStarts = [51.37f, 71.51f, 91.64f];
    public static readonly float[] ViscousDurations = [12f, 32f, 52f];

    public static readonly Vector2 North = new(0f, -17.5f);
    public static readonly Vector2 NorthWest = new(-12f, -12.5f);

    public IReadOnlyList<Primal> Order { get; }
    public IReadOnlyList<PartyRole> ViscousTargets { get; }

    public PrimalRouletteState(Rng rng, PartyRole? player, PrimalRouletteStateOverrides overrides)
    {
        Order = Orders[overrides.Order is { } fixedOrder && fixedOrder >= 0 && fixedOrder < Orders.Length ? fixedOrder : rng.Next(Orders.Length)];

        var targets = rng.Shuffle(Enumerable.Range(0, 8).Select(i => (PartyRole)i)).Take(3).ToList();
        if (overrides.ViscousOnPlayer && player is { } me && !targets.Contains(me))
            targets[rng.Next(3)] = me;
        ViscousTargets = targets;
    }

    // Ifrit's dash leaves the party on the north-west intercard; every other primal sends it back to 2.
    public Vector2 StartAnchor(int segment) =>
        segment > 0 && Order[segment - 1] == Primal.Ifrit ? NorthWest : North;

    public static Vector2 OtherAnchor(Vector2 anchor) => anchor == North ? NorthWest : North;

    public static Vector2 TowardCentre(Vector2 anchor, float radius) => Vector2.Normalize(anchor) * radius;
}
