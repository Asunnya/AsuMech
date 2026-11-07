using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Scenarios.Ucob.P3Blackfire;

public sealed class UcobP3BlackfireState
{
    public const int HypernovaCount = 4;

    public static readonly PartyRole[] Supports = [MainTank, OffTank, RegenHealer, ShieldHealer];
    public static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];

    public Direction NaelFrom { get; }
    public PartyRole LiquidHellTarget { get; }
    public PartyRole BeamTarget { get; }
    // Always two supports and two DPS.
    public IReadOnlyList<PartyRole> StackTargets { get; }
    public IReadOnlyList<PartyRole> HypernovaTargets { get; }

    // A role picked both to stack and to soak a tower stacks.
    public UcobP3BlackfireState(Rng rng, UcobP3BlackfireStateOverrides? overrides = null)
    {
        overrides ??= new UcobP3BlackfireStateOverrides();
        var everyone = Enum.GetValues<PartyRole>();
        // Every roll is drawn even when pinned, so a pin leaves the others as they were.
        var nael = rng.NextDirection();
        var liquidHell = rng.NextObj(everyone);
        var beam = rng.NextObj(everyone);
        var tower = overrides.TowerIncludes == overrides.StackIncludes ? null : overrides.TowerIncludes;
        var stack = RollTwo(rng, Supports, overrides.StackIncludes, tower).Concat(RollTwo(rng, Dps, overrides.StackIncludes, tower)).ToList();
        var hypernovas = Enumerable.Range(0, HypernovaCount).Select(_ => rng.NextObj(everyone)).ToList();
        NaelFrom = overrides.NaelFrom ?? nael;
        LiquidHellTarget = overrides.LiquidHellTarget ?? liquidHell;
        BeamTarget = overrides.BeamTarget ?? beam;
        StackTargets = overrides.StackTargets ?? stack;
        HypernovaTargets = overrides.HypernovaTargets ?? hypernovas;
    }

    private static IEnumerable<PartyRole> RollTwo(Rng rng, PartyRole[] pool, PartyRole? include, PartyRole? exclude)
    {
        var picked = rng.Shuffle(pool).Where(r => r != include && r != exclude).ToList();
        if (include is { } role && pool.Contains(role)) picked.Insert(0, role);
        return picked.Take(2).OrderBy(r => (int)r).ToList();
    }
}
