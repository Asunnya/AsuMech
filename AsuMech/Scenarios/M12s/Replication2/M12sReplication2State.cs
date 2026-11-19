using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.Replication2;

public enum CloneMechanic
{
    Cone,
    Defamation,
    Stack,
}

public sealed class M12sReplication2State
{
    public const int PlayerClones = 8;
    public const int BossClones = 6;
    public const float PlayerCloneRadius = 14f;
    public const float BossCloneRadius = 10f;
    public static readonly string[] SpotNames = ["North", "Northeast", "East", "Southeast", "South", "Southwest", "West", "Northwest"];

    // The player clones show up in these pairs, which is also the order they replay in.
    public static readonly (int A, int B)[] ClonePairs = [(0, 4), (1, 5), (2, 6), (3, 7)];

    // Clockwise from north, one spot per role.
    public IReadOnlyDictionary<PartyRole, int> CloneSpot { get; }
    // Clockwise from north in sixths of a turn. UNVERIFIED: any order of two of each.
    public IReadOnlyList<CloneMechanic> CloneMechanics { get; }
    // Who each boss clone's tether, then the boss's, starts on before anyone takes it.
    public IReadOnlyList<PartyRole> FirstHolders { get; }
    public bool NetherwrathFar { get; }
    // Snaking Kick turns to the first of these still alive.
    public IReadOnlyList<PartyRole> KickOrder { get; }

    public M12sReplication2State(Rng rng, M12sReplication2StateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M12sReplication2StateOverrides();
        var spots = rng.Shuffle(Enumerable.Range(0, PlayerClones)).ToList();
        var roles = Enum.GetValues<PartyRole>();
        var cloneSpot = roles.Zip(spots).ToDictionary(p => p.First, p => p.Second);
        if (player is { } me && overrides.PlayerSpot is { } wanted && cloneSpot[me] != wanted)
        {
            var other = cloneSpot.First(kv => kv.Value == wanted).Key;
            (cloneSpot[me], cloneSpot[other]) = (wanted, cloneSpot[me]);
        }
        CloneSpot = cloneSpot;
        CloneMechanics = rng.Shuffle(CloneMechanic.Cone, CloneMechanic.Cone, CloneMechanic.Stack, CloneMechanic.Stack,
            CloneMechanic.Defamation, CloneMechanic.Defamation).ToList();
        FirstHolders = rng.Shuffle(roles).Take(BossClones + 1).ToList();
        var far = rng.NextBool();
        NetherwrathFar = overrides.NetherwrathFar ?? far;
        KickOrder = rng.Shuffle(roles).ToList();
    }

    public PartyRole RoleAt(int spot) => CloneSpot.First(kv => kv.Value == spot).Key;

    public static Vector3 PlayerClonePosition(int spot) => OnRing(spot * MathF.PI / 4f, PlayerCloneRadius);

    public static Vector3 BossClonePosition(int clone) => OnRing(clone * MathF.PI / 3f, BossCloneRadius);

    // `bearing` clockwise from north.
    private static Vector3 OnRing(float bearing, float radius) => new(radius * MathF.Sin(bearing), 0f, -radius * MathF.Cos(bearing));
}
