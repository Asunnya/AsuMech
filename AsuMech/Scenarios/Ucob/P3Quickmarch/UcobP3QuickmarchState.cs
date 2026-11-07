using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Scenarios.Ucob.P3Quickmarch;

public sealed class UcobP3QuickmarchState
{
    public const int TwisterCount = 4;
    public const int PuddleCount = 4;
    public const int SpreadCount = 4;

    public static readonly PartyRole[] NonTanks = [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    public static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    public static readonly PartyRole[] TetherTargets = [MainTank, OffTank];

    public Direction DiveFrom { get; }
    public IReadOnlyList<PartyRole> TwisterTargets { get; }
    public IReadOnlyList<PartyRole> PuddleTargets { get; }
    // UNVERIFIED: tanks are never spread.
    public IReadOnlyList<PartyRole> SpreadTargets { get; }
    public PartyRole EarthShakerDps { get; }
    public IReadOnlyList<PartyRole> EarthShakerTargets { get; }
    public IReadOnlyList<PartyRole> StackTargets { get; }

    public UcobP3QuickmarchState(Rng rng, UcobP3QuickmarchStateOverrides? overrides = null)
    {
        overrides ??= new UcobP3QuickmarchStateOverrides();
        var everyone = Enum.GetValues<PartyRole>();
        // Every roll is drawn even when pinned, so a pin leaves the others as they were.
        var dive = rng.NextDirection();
        var twisters = rng.Shuffle(everyone).Take(TwisterCount).ToList();
        var puddles = Roll(rng, everyone, PuddleCount, overrides.PuddleIncludes);
        var spreads = Roll(rng, NonTanks, SpreadCount, overrides.SpreadIncludes);
        var earthShakerDps = rng.NextObj(Dps);
        DiveFrom = overrides.DiveFrom ?? dive;
        TwisterTargets = overrides.TwisterTargets ?? twisters;
        PuddleTargets = overrides.PuddleTargets ?? puddles;
        SpreadTargets = overrides.SpreadTargets ?? spreads;
        EarthShakerDps = overrides.EarthShakerDps is { } dps && Dps.Contains(dps) ? dps : earthShakerDps;
        EarthShakerTargets = [RegenHealer, ShieldHealer, EarthShakerDps];
        StackTargets = Dps.Where(r => r != EarthShakerDps).ToList();
    }

    private static IReadOnlyList<PartyRole> Roll(Rng rng, PartyRole[] pool, int count, PartyRole? include)
    {
        var picked = rng.Shuffle(pool).Where(r => r != include).ToList();
        if (include is { } role && pool.Contains(role)) picked.Insert(0, role);
        return picked.Take(count).OrderBy(r => (int)r).ToList();
    }
}
