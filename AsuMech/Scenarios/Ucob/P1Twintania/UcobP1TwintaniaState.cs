using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Scenarios.Ucob.P1Twintania;

public sealed class UcobP1TwintaniaState
{
    public const float FireballRadius = 4f;
    // UNVERIFIED: one or two took lethal hits, five or more lived.
    public const int FireballMinimumStack = 4;

    public static readonly int[] HatchCounts = [1, 1, 2, 2, 2];
    // The Generate whose markers are up while the targeted Liquid Hell starts.
    public const int HatchesDuringTargetedLiquidHell = 2;

    private static readonly PartyRole[] NonTanks = [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    public IReadOnlyList<IReadOnlyList<PartyRole>> TwisterTargets { get; }
    public IReadOnlyList<PartyRole> FireballTargets { get; }
    public IReadOnlyList<IReadOnlyList<PartyRole>> HatchTargets { get; }
    // Kept off the Hatch targets of that moment, who wait in a Neurolink where the puddles would land.
    public PartyRole LiquidHellTarget { get; }

    public UcobP1TwintaniaState(Rng rng, UcobP1TwintaniaStateOverrides? overrides = null)
    {
        overrides ??= new UcobP1TwintaniaStateOverrides();
        TwisterTargets = overrides.TwisterTargets
            ?? Enumerable.Range(0, 3).Select(_ => (IReadOnlyList<PartyRole>)rng.Shuffle(Enum.GetValues<PartyRole>()).Take(UcobTwisters.Count).ToList()).ToList();
        FireballTargets = overrides.FireballTargets.Select(pick => pick ?? rng.NextObj(NonTanks)).ToList();
        HatchTargets = overrides.HatchTargets
            ?? HatchCounts.Select((count, set) => RollHatch(rng, count, overrides.HatchIncludes[set])).ToList();
        LiquidHellTarget = overrides.LiquidHellTarget
            ?? rng.NextObj(Enum.GetValues<PartyRole>().Except(HatchTargets[HatchesDuringTargetedLiquidHell]).ToArray());
    }

    private static IReadOnlyList<PartyRole> RollHatch(Rng rng, int count, PartyRole? include)
    {
        var pool = rng.Shuffle(Dps).Where(r => r != include).ToList();
        if (include is { } role) pool.Insert(0, role);
        return pool.Take(count).OrderBy(r => (int)r).ToList();
    }
}
