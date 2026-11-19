using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P1FallOfFaith;

// Four Fall of Faith tethers on four random players, each fire or lightning; Burnt Strike fire or
// lightning with its three towers on the west or the east edge, six soakers split 1/1/4, 1/2/3 or 2/2/2.
public sealed class FruP1FallOfFaithState
{
    public static readonly float[] FaithCastAt = [3f, 6.03f, 8.64f, 11.07f];
    public const float FaithCastSeconds = 9f;
    public static float FaithResolveAt(int k) => FaithCastAt[k] + FaithCastSeconds;
    public static float FaithHitAt(int k) => FaithResolveAt(k) + 4f;

    // North, middle, south.
    public static readonly int[][] TowerPatterns =
    [
        [1, 1, 4], [1, 4, 1], [4, 1, 1],
        [1, 2, 3], [1, 3, 2], [2, 1, 3], [2, 3, 1], [3, 1, 2], [3, 2, 1],
        [2, 2, 2],
    ];

    public IReadOnlyList<PartyRole> Tethered { get; }
    public IReadOnlyList<bool> FaithFire { get; }
    public bool TowersWest { get; }
    public bool TowersFire { get; }
    public IReadOnlyList<int> TowerSoakers { get; }

    public FruP1FallOfFaithState(Rng rng, PartyRole playerRole, FruP1FallOfFaithStateOverrides overrides)
    {
        var duties = overrides.Tether.Resolve(playerRole);
        var refused = duties.Where(d => d.Value == FallOfFaithTether.None).Select(d => d.Role).ToList();
        var forced = duties.Where(d => d.Value != FallOfFaithTether.None).ToList();
        var free = rng.Shuffle(Enum.GetValues<PartyRole>())
            .Where(r => !refused.Contains(r) && forced.All(f => f.Role != r))
            .Concat(refused).ToList();
        var slots = new PartyRole[4];
        var taken = new bool[4];
        foreach (var (role, slot) in forced)
        {
            var index = (int)slot - 1;
            if (taken[index]) { free.Insert(0, role); continue; }
            slots[index] = role;
            taken[index] = true;
        }
        for (var i = 0; i < 4; i++)
        {
            if (taken[i]) continue;
            slots[i] = free[0];
            free.RemoveAt(0);
        }
        Tethered = slots;
        var faithFire = Enumerable.Range(0, 4).Select(_ => rng.NextBool()).ToList();
        var towersWest = rng.NextBool();
        var towersFire = rng.NextBool();
        var towerPattern = rng.Next(TowerPatterns.Length);
        FaithFire = Enumerable.Range(0, 4).Select(k => overrides.FaithFire.ElementAtOrDefault(k) ?? faithFire[k]).ToList();
        TowersWest = overrides.TowersWest ?? towersWest;
        TowersFire = overrides.TowersFire ?? towersFire;
        TowerSoakers = TowerPatterns[overrides.TowerPattern ?? towerPattern];
    }

    public int FaithIndex(PartyRole role) => Array.IndexOf(Tethered.ToArray(), role);

    public float TowerSide => TowersWest ? -1f : 1f;

    public Vector3 Tower(int index) => index switch
    {
        0 => new(TowerSide * 13.11f, 0, -9.18f),
        1 => new(TowerSide * 16f, 0, 0),
        _ => new(TowerSide * 13.11f, 0, 9.18f),
    };
}
