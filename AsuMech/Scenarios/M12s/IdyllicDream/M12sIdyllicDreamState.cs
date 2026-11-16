using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.M12s.IdyllicDream;

public enum TowerKind { Earth, Fire, Wind, Doom }

// A tower on platform `Side` (-1 west, 1 east), the inner or outer pair, north (-1) or south (1).
public sealed record DreamTower(int Side, bool Inner, int ZSign, TowerKind Kind)
{
    public bool Triple => Kind is TowerKind.Wind or TowerKind.Doom;

    public Vector3 Centre => new(Side * (Inner ? M12sIdyllicDreamState.InnerTowerX : M12sIdyllicDreamState.OuterTowerX), 0f,
        ZSign * M12sIdyllicDreamState.TowerZ);
}

// Directions count clockwise from north: 0 N, 1 NE ... 7 NW.
public sealed class M12sIdyllicDreamState
{
    public const int Directions = 8;
    public const float PlayerCloneRadius = 14f;
    public const float BossCloneRadius = 10f;
    public const float MemoryCloneZ = 12f;
    public const float KickCloneZ = 4.5f;
    public const float PlatformCloneX = 14f;
    public const float TearCloneZ = -7.5f;
    public const float InnerTowerX = 9.76f;
    public const float OuterTowerX = 18.24f;
    public const float TowerZ = 4.24f;
    public const float TowerRadius = 3f;
    public const float SnakingKickRadius = 10f;
    // UNVERIFIED: the P2 floor and the two platforms Downfall leaves.
    public const float ArenaRadius = 20f;
    public const float PlatformInner = 6f;
    public const float PlatformOuter = 22f;
    public const float PlatformHalfLength = 10f;

    public IReadOnlyDictionary<PartyRole, int> CloneDirection { get; }
    // The player clones on the cardinals show first, and the Reenactment replays them first.
    public bool CardinalClonesFirst { get; }
    // The boss clones on the cardinals tether defamations, the ones on the intercards stacks; or the reverse.
    public bool CardinalBossClonesDefa { get; }
    // Who each boss clone's tether first lands on, by the clone's direction.
    public IReadOnlyList<PartyRole> FirstHolders { get; }
    // Replication's three clones: Snaking Kick at z ±4.5, the Power Gushers at z ±12.
    public float KickZ { get; }
    public float EastWestConesZ { get; }
    public bool TearTakesEastWestClone { get; }
    public int KickPlatform { get; }
    public bool SpreadOnSupports { get; }
    public IReadOnlyList<DreamTower> Towers { get; }

    public M12sIdyllicDreamState(Rng rng, M12sIdyllicDreamStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M12sIdyllicDreamStateOverrides();
        var directions = rng.Shuffle(Enumerable.Range(0, Directions)).ToList();
        var roles = Enum.GetValues<PartyRole>();
        var clones = roles.Select((r, i) => (r, directions[i])).ToDictionary(x => x.r, x => x.Item2);
        if (player is { } me && overrides.PlayerCloneDirection is { } wanted)
        {
            var holder = clones.First(kv => kv.Value == wanted).Key;
            (clones[me], clones[holder]) = (wanted, clones[me]);
        }
        CloneDirection = clones;
        var cardinalFirst = rng.NextBool();
        var cardinalDefa = rng.NextBool();
        FirstHolders = rng.Shuffle(roles).ToList();
        KickZ = rng.NextBool() ? KickCloneZ : -KickCloneZ;
        EastWestConesZ = rng.NextBool() ? -MemoryCloneZ : MemoryCloneZ;
        var tearEastWest = rng.NextBool();
        var kickPlatform = rng.NextBool() ? -1 : 1;
        var spreadSupports = rng.NextBool();
        Towers = [.. RollTowers(rng, -1), .. RollTowers(rng, 1)];
        CardinalClonesFirst = overrides.CardinalClonesFirst ?? cardinalFirst;
        CardinalBossClonesDefa = overrides.CardinalBossClonesDefa ?? cardinalDefa;
        TearTakesEastWestClone = overrides.TearTakesEastWestClone ?? tearEastWest;
        KickPlatform = overrides.KickPlatform ?? kickPlatform;
        SpreadOnSupports = overrides.SpreadOnSupports ?? spreadSupports;
    }

    // Each platform has one tower of each kind; the inner pair and the outer pair each hold one triple.
    private static IEnumerable<DreamTower> RollTowers(Rng rng, int side)
    {
        var windInner = rng.NextBool();
        var earthInner = rng.NextBool();
        var innerTripleNorth = rng.NextBool();
        var outerTripleNorth = rng.NextBool();
        foreach (var inner in new[] { true, false })
        {
            var tripleZ = (inner ? innerTripleNorth : outerTripleNorth) ? -1 : 1;
            var triple = inner == windInner ? TowerKind.Wind : TowerKind.Doom;
            var simple = inner == earthInner ? TowerKind.Earth : TowerKind.Fire;
            yield return new DreamTower(side, inner, tripleZ, triple);
            yield return new DreamTower(side, inner, -tripleZ, simple);
        }
    }

    public static Vector3 OnRing(int direction, float radius)
    {
        var angle = direction * MathF.PI / 4f;
        return new Vector3(MathF.Sin(angle) * radius, 0f, -MathF.Cos(angle) * radius);
    }

    public static Vector3 PlayerCloneSpot(int direction) => OnRing(direction, PlayerCloneRadius);

    public static Vector3 BossCloneSpot(int direction) => OnRing(direction, BossCloneRadius);

    public static bool IsCardinal(int direction) => direction % 2 == 0;

    public bool IsDefaClone(int direction) => IsCardinal(direction) == CardinalBossClonesDefa;

    // The boss clones resolve in pairs, N/S first, then NE/SW, E/W and SE/NW.
    public static int ResolveSlot(int direction) => direction % 4;

    public IEnumerable<int> ReenactedFirst => Enumerable.Range(0, Directions).Where(d => IsCardinal(d) == CardinalClonesFirst);

    public PartyRole CloneOwner(int direction) => CloneDirection.First(kv => kv.Value == direction).Key;

    public bool IsSpreadRole(PartyRole role) => !role.IsDps() == SpreadOnSupports;

    public DreamTower TowerAt(int side, bool inner, int zSign) => Towers.First(t => t.Side == side && t.Inner == inner && t.ZSign == zSign);

    public Vector3 NorthSouthConesClone => new(0f, 0f, -EastWestConesZ);

    public Vector3 EastWestConesClone => new(0f, 0f, EastWestConesZ);

    public Vector3 KickClone => new(0f, 0f, KickZ);

    public Vector3 PlatformCone => new(-KickPlatform * PlatformCloneX, 0f, 0f);

    public Vector3 PlatformKick => new(KickPlatform * PlatformCloneX, 0f, 0f);

    // The platform clone keeps whichever cones the tear didn't take.
    public bool PlatformConesEastWest => !TearTakesEastWestClone;

    public static float[] ConeHeadings(bool eastWest) => eastWest ? [MathF.PI / 2f, -MathF.PI / 2f] : [0f, MathF.PI];

    public static bool InCones(Vector3 clone, bool eastWest, Vector2 p)
    {
        var dx = MathF.Abs(p.X - clone.X);
        var dz = MathF.Abs(p.Y - clone.Z);
        return eastWest ? dx > dz : dz > dx;
    }

    public bool InFirstReplay(Vector2 p)
        => InCones(EastWestConesClone, true, p) || InCones(NorthSouthConesClone, false, p)
           || Vector2.Distance(p, new Vector2(0f, KickZ)) < SnakingKickRadius;

    public static bool OnPlatform(Vector3 p)
        => MathF.Abs(p.Z) <= PlatformHalfLength && MathF.Abs(p.X) >= PlatformInner && MathF.Abs(p.X) <= PlatformOuter;
}
