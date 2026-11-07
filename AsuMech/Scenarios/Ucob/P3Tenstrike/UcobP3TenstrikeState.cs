using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;
using static AsuMech.Core.Game.Party.PartyRole;

namespace AsuMech.Scenarios.Ucob.P3Tenstrike;

public sealed class UcobP3TenstrikeState
{
    public const int HatchCount = 3;
    public const int EarthShakersPerSet = 4;

    // UNVERIFIED: no tank was marked in the clear or in P1.
    public static readonly PartyRole[] HatchPool = [RegenHealer, ShieldHealer, MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Dps = [MeleeDpsA, MeleeDpsB, PhysRangedDps, CasterDps];
    private static readonly PartyRole[] Healers = [RegenHealer, ShieldHealer];
    private static readonly PartyRole[] Tanks = [MainTank, OffTank];

    // The same three are marked by both Generates.
    public IReadOnlyList<PartyRole> HatchTargets { get; }
    // DPS first, then healers, then tanks; UNVERIFIED: the order inside each role.
    public IReadOnlyList<PartyRole> MeteorOrder { get; }
    public IReadOnlyList<PartyRole> FirstEarthShakers { get; }
    public IReadOnlyList<PartyRole> SecondEarthShakers { get; }

    public UcobP3TenstrikeState(Rng rng, UcobP3TenstrikeStateOverrides? overrides = null)
    {
        overrides ??= new UcobP3TenstrikeStateOverrides();
        var hatches = rng.Shuffle(HatchPool).ToList();
        if (overrides.HatchInclude is { } hatch) MoveInto(hatches, hatch, 0, HatchCount);
        HatchTargets = (overrides.HatchTargets ?? hatches.Take(HatchCount).ToList()).OrderBy(r => r).ToList();
        var meteors = new[] { rng.Shuffle(Dps), rng.Shuffle(Healers), rng.Shuffle(Tanks) }.SelectMany(group => group).ToList();
        MeteorOrder = overrides.MeteorOrder ?? meteors;
        var everyone = Enum.GetValues<PartyRole>();
        var shakers = rng.Shuffle(everyone).ToList();
        if (overrides.SecondEarthShakerInclude is { } second) MoveInto(shakers, second, EarthShakersPerSet, everyone.Length);
        if (overrides.FirstEarthShakerInclude is { } firstPick) MoveInto(shakers, firstPick, 0, EarthShakersPerSet);
        FirstEarthShakers = (overrides.FirstEarthShakers ?? shakers.Take(EarthShakersPerSet).ToList()).OrderBy(r => r).ToList();
        SecondEarthShakers = everyone.Except(FirstEarthShakers).ToList();
    }

    // Swaps `role` into [from, to) if it isn't there, so a pin never changes how much is rolled.
    private static void MoveInto(List<PartyRole> rolled, PartyRole role, int from, int to)
    {
        var at = rolled.IndexOf(role);
        if (at < 0 || (at >= from && at < to)) return;
        var into = at < from ? from : to - 1;
        (rolled[at], rolled[into]) = (rolled[into], rolled[at]);
    }
}
