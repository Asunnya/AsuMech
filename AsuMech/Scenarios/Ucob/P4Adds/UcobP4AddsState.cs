using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using static AsuMech.Core.Game.Party.PartyRole;
using static AsuMech.Scenarios.Ucob.NaelMove;

namespace AsuMech.Scenarios.Ucob.P4Adds;

public sealed class UcobP4AddsState
{
    public const int Generates = 2;
    public const int HatchCount = 3;
    public const int TwisterCasts = 4;
    public const int Quotes = 2;

    // The second quote always opens the other way from the first.
    public static readonly NaelTripleQuote[] InQuotes =
    [
        new("From hallowed moon I bare iron, in my descent to wield!", LunarDynamo, IronChariot, RavenDive),
        new("From hallowed moon I descend, upon burning earth to tread!", LunarDynamo, RavenDive, ThermionicBeam),
    ];
    public static readonly NaelTripleQuote[] OutQuotes =
    [
        new("Unbending iron, take fire and descend!", IronChariot, ThermionicBeam, RavenDive),
        new("Unbending iron, descend with fiery edge!", IronChariot, RavenDive, ThermionicBeam),
    ];
    public static readonly NaelTripleQuote[] QuoteChoices = [.. InQuotes, .. OutQuotes];

    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    // Holds her until the swap after Death Sentence; the other tank holds Nael.
    public PartyRole TwintaniaTank { get; }
    public IReadOnlyList<NaelTripleQuote> NaelQuotes { get; }
    public IReadOnlyList<IReadOnlyList<PartyRole>> HatchTargets { get; }
    public IReadOnlyList<IReadOnlyList<PartyRole>> TwisterTargets { get; }
    public IReadOnlyList<PartyRole> RavenDiveTargets { get; }
    public IReadOnlyList<PartyRole> BeamTargets { get; }

    public UcobP4AddsState(Rng rng, UcobP4AddsStateOverrides? overrides = null)
    {
        overrides ??= new UcobP4AddsStateOverrides();
        var roles = Enum.GetValues<PartyRole>();
        TwintaniaTank = overrides.TwintaniaTank == OffTank ? OffTank : MainTank;
        var rolledFirst = rng.NextObj(QuoteChoices);
        var first = overrides.FirstQuote is { } pick and >= 0 and < 4 ? QuoteChoices[pick] : rolledFirst;
        var other = InQuotes.Contains(first) ? OutQuotes : InQuotes;
        var rolledSecond = rng.Next(other.Length);
        NaelQuotes = [first, other[overrides.SecondQuote is { } index and (0 or 1) ? index : rolledSecond]];
        var rolledHatches = Enumerable.Range(0, Generates).Select(set => RollHatch(rng, overrides.HatchIncludes[set])).ToList();
        HatchTargets = overrides.HatchTargets ?? rolledHatches;
        var rolledTwisters = Enumerable.Range(0, TwisterCasts).Select(_ => (IReadOnlyList<PartyRole>)rng.Shuffle(roles).Take(AddsTwisters.Count).ToList()).ToList();
        TwisterTargets = overrides.TwisterTargets ?? rolledTwisters;
        RavenDiveTargets = overrides.RavenDiveTargets.Select(pin => (pin, rolled: rng.NextObj(roles))).Select(r => r.pin ?? r.rolled).ToList();
        BeamTargets = overrides.BeamTargets.Select(pin => (pin, rolled: rng.NextObj(roles))).Select(r => r.pin ?? r.rolled).ToList();
    }

    public PartyRole NaelTank => TwintaniaTank == MainTank ? OffTank : MainTank;

    private static IReadOnlyList<PartyRole> RollHatch(Rng rng, PartyRole? include)
    {
        var pool = rng.Shuffle(Dps).Where(r => r != include).ToList();
        if (include is { } role && Dps.Contains(role)) pool.Insert(0, role);
        return pool.Take(HatchCount).OrderBy(r => (int)r).ToList();
    }
}
