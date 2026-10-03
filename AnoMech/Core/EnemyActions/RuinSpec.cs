using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.EnemyActions;

// A family of statuses that kill on a repeated hit, keyed by how many times it takes: TOP's Come
// Ruin is (2, Twice-come Ruin), (3, Thrice-come Ruin). Each stack carried is 1/times of a death,
// counted across the whole family; the stack that completes a death kills instead of landing.
public sealed class RuinSpec
{
    private readonly IReadOnlyDictionary<int, ushort> statuses;
    // Weights in units of 1/death, so the sum stays in whole numbers.
    private readonly int death;

    public RuinSpec(params (int Times, ushort StatusId)[] statuses)
    {
        if (statuses.Length == 0 || statuses.Any(s => s.Times < 1))
            throw new ArgumentException("A ruin family needs at least one status, each lethal on a hit count of 1 or more.");
        this.statuses = statuses.ToDictionary(s => s.Times, s => s.StatusId);
        death = statuses.Aggregate(1, (lcm, s) => Lcm(lcm, s.Times));
    }

    internal ushort StatusId(int times)
        => statuses.TryGetValue(times, out var statusId)
            ? statusId
            : throw new ArgumentException($"No ruin status is lethal on hit {times}.", nameof(times));

    internal bool Overloads(SimCharacter target, int times)
    {
        var carried = statuses.Sum(s => death / s.Key * (target.FindStatus(s.Value)?.Stacks ?? 0));
        return carried + death / times >= death;
    }

    private static int Lcm(int a, int b) => a / Gcd(a, b) * b;

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
