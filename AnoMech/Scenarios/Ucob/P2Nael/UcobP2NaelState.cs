using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Ucob.P2Nael;

public enum NaelMove { LunarDynamo, IronChariot, ThermionicBeam, RavenDive }

public sealed record NaelQuote(string Text, NaelMove First, NaelMove Second);

public sealed class UcobP2NaelState
{
    // The first three quotes each pick from their own pair.
    public static readonly NaelQuote[][] QuoteChoices =
    [
        [
            new("O hallowed moon, take fire and scorch my foes!", NaelMove.LunarDynamo, NaelMove.ThermionicBeam),
            new("O hallowed moon, shine you the iron path!", NaelMove.LunarDynamo, NaelMove.IronChariot),
        ],
        [
            new("Take fire, O hallowed moon!", NaelMove.ThermionicBeam, NaelMove.LunarDynamo),
            new("Blazing path, lead me to iron rule!", NaelMove.ThermionicBeam, NaelMove.IronChariot),
        ],
        [
            new("From on high I descend, the hallowed moon to call!", NaelMove.RavenDive, NaelMove.LunarDynamo),
            new("From on high I descend, the iron path to walk!", NaelMove.RavenDive, NaelMove.IronChariot),
        ],
    ];

    public const float ThermionicBurstRange = 24f;
    public const float ThermionicBurstHalfAngle = MathF.PI / 16f; // UNVERIFIED
    public const float MeteorStreamRadius = 4f;
    public const float DalamudDiveRadius = 5f;
    public const float LunarDynamoInner = 6f; // UNVERIFIED
    // UNVERIFIED: someone 6.6y from her was still hit.
    public const float IronChariotRadius = 7f;
    public const float ThermionicBeamRadius = 4f;
    public const int ThermionicBeamMinimumStack = 4; // UNVERIFIED
    public const float RavenDiveRadius = 3f;
    public const float ThunderstruckRadius = 5f;
    public const float DragonFireballRadius = 4f;
    public const float WingsRadius = 4f;
    public const float CleanseRadius = 1.5f; // UNVERIFIED
    public const float DragonDistance = 24f;

    public static readonly int[] ThunderCounts = [2, 2, 2, 2];
    public static readonly int[] DoomCounts = [2, 3, 3];
    public static readonly float[][] DoomSeconds = [[6f, 10f], [6f, 10f, 16f], [6f, 10f, 16f]];
    public static readonly int[] WingsCounts = [2, 3, 3];
    public const int FireTethers = 4;
    // The second fire is taken alone; the rest are shared by the whole party.
    public const int FireTakenAlone = 1;

    public IReadOnlyList<NaelQuote> Quotes { get; }
    public IReadOnlyList<PartyRole> MeteorOrder { get; }
    public IReadOnlyList<PartyRole> IceOrder { get; }
    public IReadOnlyList<PartyRole> FireTargets { get; }
    public IReadOnlyList<IReadOnlyList<PartyRole>> ThunderTargets { get; }
    public IReadOnlyList<IReadOnlyList<PartyRole>> DoomTargets { get; }
    // Simplification: the puddles fall on players with neither that set's Doom nor its Thunderstruck.
    public IReadOnlyList<IReadOnlyList<PartyRole>> WingsTargets { get; }
    public IReadOnlyList<PartyRole> BeamTargets { get; }
    public PartyRole RavenDiveTarget { get; }
    // Bearings, in 45° steps clockwise from north, of the five dragons.
    public IReadOnlyList<int> DragonSlots { get; }

    public UcobP2NaelState(Rng rng, UcobP2NaelStateOverrides? overrides = null)
    {
        overrides ??= new UcobP2NaelStateOverrides();
        var roles = Enum.GetValues<PartyRole>();
        Quotes = QuoteChoices.Select((choices, i) => overrides.Quotes[i] is { } pick ? choices[pick] : rng.NextObj(choices)).ToList();
        MeteorOrder = overrides.MeteorOrder ?? rng.Shuffle(roles).ToList();
        IceOrder = overrides.IceOrder ?? rng.Shuffle(roles).ToList();
        FireTargets = overrides.FireTargets ?? RollFire(rng, overrides.FirePicks);
        ThunderTargets = overrides.ThunderTargets
            ?? ThunderCounts.Select((count, i) => Roll(rng, count, overrides.ThunderIncludes[i])).ToList();
        DoomTargets = overrides.DoomTargets
            ?? DoomCounts.Select((count, i) => Roll(rng, count, overrides.DoomIncludes[i])).ToList();
        WingsTargets = overrides.WingsTargets
            ?? WingsCounts.Select((count, set) =>
            {
                var busy = DoomTargets[set].Concat(set == 0 ? [] : ThunderTargets[set + 1]).ToHashSet();
                var pool = roles.Where(r => !busy.Contains(r)).ToArray();
                return (IReadOnlyList<PartyRole>)Enumerable.Range(0, count).Select(_ => rng.NextObj(pool)).ToList();
            }).ToList();
        BeamTargets = overrides.BeamTargets ?? Enumerable.Range(0, 3).Select(_ => rng.NextObj(roles)).ToList();
        RavenDiveTarget = overrides.RavenDiveTarget ?? rng.NextObj(roles);
        DragonSlots = overrides.DragonSlots ?? rng.Shuffle(Enumerable.Range(0, 8)).Take(5).ToList();
    }

    // UNVERIFIED: the third fire never tethers the one still Firescorched from the second.
    private static IReadOnlyList<PartyRole> RollFire(Rng rng, PartyRole?[] picks)
    {
        var roles = Enum.GetValues<PartyRole>();
        var fire = new List<PartyRole>();
        for (var i = 0; i < FireTethers; i++)
        {
            var pool = i == 2 ? roles.Where(r => r != fire[FireTakenAlone]).ToArray() : roles;
            fire.Add(picks[i] is { } pick && pool.Contains(pick) ? pick : rng.NextObj(pool));
        }
        return fire;
    }

    private static IReadOnlyList<PartyRole> Roll(Rng rng, int count, PartyRole? include)
    {
        var pool = rng.Shuffle(Enum.GetValues<PartyRole>()).Where(r => r != include).ToList();
        if (include is { } role) pool.Insert(0, role);
        return pool.Take(count).ToList();
    }

    public static Vector2 DragonPosition(int slot)
    {
        var angle = slot * MathF.PI / 4f;
        return new Vector2(MathF.Sin(angle), -MathF.Cos(angle)) * DragonDistance;
    }
}
