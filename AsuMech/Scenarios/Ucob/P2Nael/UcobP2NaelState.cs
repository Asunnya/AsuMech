using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Ucob.P2Nael;

// Push: the party kills her phase off after Ravensbeak and she leaves. NoPush: the divebombs follow.
public enum NaelPush { Push, NoPush }

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

    public static readonly NaelQuote[] DivebombQuoteChoices =
    [
        new("Fleeting light! 'Amid a rain of stars, exalt you the red moon!'", NaelMove.MeteorStream, NaelMove.DalamudDive),
        new("Fleeting light! 'Neath the red moon, scorch you the earth!'", NaelMove.DalamudDive, NaelMove.ThermionicBeam),
    ];

    public const float ThermionicBurstRange = 24f;
    public const float ThermionicBurstHalfAngle = UcobConstants.Geometry.ThermionicBurstHalfAngle;
    public const float MeteorStreamRadius = 4f;
    public const float DalamudDiveRadius = 5f;
    public const float LunarDynamoInner = UcobConstants.Geometry.LunarDynamoInner;
    public const float IronChariotRadius = UcobConstants.Geometry.IronChariotRadius;
    public const float ThermionicBeamRadius = 4f;
    public const int ThermionicBeamMinimumStack = 4; // UNVERIFIED
    public const float RavenDiveRadius = 3f;
    public const float ThunderstruckRadius = 5f;
    public const float DragonFireballRadius = 4f;
    public const float WingsRadius = 4f;
    public const float CleanseRadius = 1.5f; // UNVERIFIED
    public const float DragonDistance = 24f;
    public const float CauterizeLength = 48f;
    public const float CauterizeHalfWidth = 10f;
    public const float HypernovaRadius = 5f;
    // UNVERIFIED: the puddle burns as wide as the hit that drops it.
    public const float HypernovaPuddleRadius = 5f;

    public static readonly int[] ThunderCounts = [2, 2, 2, 2];
    public static readonly int[] DoomCounts = [2, 3, 3];
    public static readonly float[][] DoomSeconds = [[6f, 10f], [6f, 10f, 16f], [6f, 10f, 16f]];
    public static readonly int[] WingsCounts = [2, 3, 3];
    public const int FireTethers = 4;
    // The second fire is taken alone; the rest are shared by the whole party.
    public const int FireTakenAlone = 1;
    public const int Divebombs = 3;
    public const int DivebombSpreadCount = 4;

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

    public NaelPush Push { get; }
    public NaelQuote DivebombQuote { get; }
    // Never the main tank, who holds Nael for her dive.
    public IReadOnlyList<PartyRole> DivebombTargets { get; }
    // Slots each divebomb sends, from north clockwise: the first two, the third, the last two.
    public IReadOnlyList<IReadOnlyList<int>> DiveDragons { get; }
    // UNVERIFIED: the spread and the stack spare the main tank, who takes the dive.
    public IReadOnlyList<PartyRole> DivebombSpreads { get; }
    public PartyRole DivebombStackTarget { get; }

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
        var rolledSlots = rng.Shuffle(Enumerable.Range(0, 8)).Take(5).ToList();
        DragonSlots = overrides.DragonSlots ?? rolledSlots;

        Push = overrides.Push;
        var rolledQuote = rng.NextObj(DivebombQuoteChoices);
        DivebombQuote = overrides.DivebombQuote is { } quote ? DivebombQuoteChoices[quote] : rolledQuote;
        DivebombTargets = RollDivebombs(rng, overrides.DivebombPicks);
        var clockwise = DragonSlots.Order().ToList();
        DiveDragons = [clockwise.Take(2).ToList(), clockwise.Skip(2).Take(1).ToList(), clockwise.Skip(3).ToList()];
        var notMainTank = roles.Where(r => r != PartyRole.MainTank).ToArray();
        var rolledSpreads = rng.Shuffle(notMainTank).Take(DivebombSpreadCount).ToList();
        DivebombSpreads = overrides.DivebombSpreads ?? rolledSpreads;
        var rolledStack = rng.NextObj(notMainTank);
        DivebombStackTarget = overrides.DivebombStackTarget ?? rolledStack;
    }

    private static IReadOnlyList<PartyRole> RollDivebombs(Rng rng, PartyRole?[] picks)
    {
        var kept = picks.Select(p => p is { } role && role != PartyRole.MainTank ? p : null).ToArray();
        for (var i = 0; i < kept.Length; i++)
            if (kept[i] is { } role && Array.IndexOf(kept, role) < i) kept[i] = null;
        var pool = rng.Shuffle(Enum.GetValues<PartyRole>().Where(r => r != PartyRole.MainTank)).Where(r => !kept.Contains(r)).ToList();
        var next = 0;
        return kept.Select(p => p ?? pool[next++]).ToList();
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
