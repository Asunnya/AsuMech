using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M11s.ArenaSplit;

// A Meteowrath tether: the shade on `Side` (-1 west, 1 east) at `Z` holds `Role`.
public sealed record WrathTether(PartyRole Role, int Side, float Z);

public sealed class M11sArenaSplitState
{
    public const int Rounds = 2;
    public const float ShadeX = 31f;
    public const float NearShadeZ = 5f;
    public const float FarShadeZ = 15f;
    public const float PlatformInner = 6f;
    public const float PlatformOuter = 26f;
    public const float PlatformCentre = 16f;
    public const float PlatformHalfLength = 20f;

    // Each round's meteor lines run north-south at x -21 and 11, or -11 and 21.
    public IReadOnlyList<bool> LinesWestOuter { get; }
    // Each round one wall's shades stand at z ±5 and the other's at ±15; the second round swaps them.
    public IReadOnlyList<bool> WestShadesNear { get; }
    // Four tethers a round; the second round tethers the four the first left free.
    public IReadOnlyList<IReadOnlyList<WrathTether>> Tethers { get; }
    public int Avalanche { get; }

    public M11sArenaSplitState(Rng rng, M11sArenaSplitStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M11sArenaSplitStateOverrides();
        var lines0 = rng.NextBool();
        var lines1 = rng.NextBool();
        var westNear = rng.NextBool();
        var first = rng.Shuffle(Enum.GetValues<PartyRole>()).Take(4).ToList();
        if (player is { } me && overrides.PlayerTetherRound is { } round)
        {
            var others = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => r != me)).ToList();
            first = round == 1 ? [me, .. others.Take(3)] : others.Take(4).ToList();
        }
        var second = Enum.GetValues<PartyRole>().Except(first).ToList();
        var avalanche = rng.Next(4);
        LinesWestOuter = [overrides.FirstLinesWestOuter ?? lines0, overrides.SecondLinesWestOuter ?? lines1];
        WestShadesNear = [westNear, !westNear];
        Tethers = [AssignShades(rng, first, westNear, false), AssignShades(rng, second, !westNear, true)];
        Avalanche = overrides.Avalanche ?? avalanche;
    }

    // The second round keeps the healers' shades on opposite walls so each ends on its own platform
    // for Massive Meteor.
    private static IReadOnlyList<WrathTether> AssignShades(Rng rng, List<PartyRole> roles, bool westNear, bool splitHealers)
    {
        var westZ = westNear ? NearShadeZ : FarShadeZ;
        var eastZ = westNear ? FarShadeZ : NearShadeZ;
        (int Side, float Z)[] shades = [(-1, -westZ), (-1, westZ), (1, -eastZ), (1, eastZ)];
        var orders = rng.Shuffle(Permutations(4)).ToList();
        foreach (var order in orders)
        {
            var tethers = roles.Select((r, i) => new WrathTether(r, shades[order[i]].Side, shades[order[i]].Z)).ToList();
            var healers = tethers.Where(t => t.Role is PartyRole.RegenHealer or PartyRole.ShieldHealer).ToList();
            if (!splitHealers || healers.Count < 2 || healers[0].Side != healers[1].Side) return tethers;
        }
        return roles.Select((r, i) => new WrathTether(r, shades[i].Side, shades[i].Z)).ToList();
    }

    private static IEnumerable<int[]> Permutations(int n)
    {
        if (n == 1) { yield return [0]; yield break; }
        foreach (var rest in Permutations(n - 1))
            for (var at = 0; at < n; at++)
            {
                var list = rest.ToList();
                list.Insert(at, n - 1);
                yield return list.ToArray();
            }
    }

    public WrathTether? TetherOf(int round, PartyRole role) => Tethers[round].FirstOrDefault(t => t.Role == role);

    public static Vector3 ShadeSpot(WrathTether tether) => new(tether.Side * ShadeX, 0f, tether.Z);

    public int[] LineXs(int round) => LinesWestOuter[round] ? [-21, 11] : [-11, 21];

    // Where on a platform (-1 west, 1 east) the meteor lines leave room, near the middle of the platform.
    public float SafeX(int round, int side) => side < 0
        ? (LinesWestOuter[round] ? -15f : -17f)
        : (LinesWestOuter[round] ? 17f : 15f);

    public int AvalancheSafeSide => Avalanche < 2 ? 1 : -1;
    public int AvalancheSafeZ => Avalanche is 0 or 3 ? -1 : 1;

    public static bool OnPlatform(Vector3 p)
        => MathF.Abs(p.Z) <= PlatformHalfLength && MathF.Abs(p.X) >= PlatformInner && MathF.Abs(p.X) <= PlatformOuter;
}
