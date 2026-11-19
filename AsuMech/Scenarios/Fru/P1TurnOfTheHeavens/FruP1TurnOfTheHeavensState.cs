using System;
using System.Collections.Generic;
using System.Linq;
using AsuMech.Core.Game.Party;

namespace AsuMech.Scenarios.Fru.P1TurnOfTheHeavens;

// The image's Cyclonic Break fire or lightning; Turn of the Heavens fire or lightning, the halos of
// flame on the west or the east half; Bound of Faith on two random players.
public sealed class FruP1TurnOfTheHeavensState
{
    public bool CyclonicFire { get; }
    public bool HeavensFire { get; }
    public bool FlameWest { get; }
    public IReadOnlyList<PartyRole> Tethered { get; }

    public FruP1TurnOfTheHeavensState(Rng rng, PartyRole playerRole, FruP1TurnOfTheHeavensStateOverrides overrides)
    {
        var cyclonicFire = rng.NextBool();
        var heavensFire = rng.NextBool();
        var flameWest = rng.NextBool();
        CyclonicFire = overrides.CyclonicFire ?? cyclonicFire;
        HeavensFire = overrides.HeavensFire ?? heavensFire;
        FlameWest = overrides.FlameWest ?? flameWest;
        var duties = overrides.BoundOfFaith.Resolve(playerRole);
        var forced = duties.Where(d => d.Value == BoundOfFaithDuty.Tethered).Select(d => d.Role);
        var refused = duties.Where(d => d.Value == BoundOfFaithDuty.Untethered).Select(d => d.Role).ToList();
        Tethered = forced.Concat(rng.Shuffle(Enum.GetValues<PartyRole>()).Where(r => !refused.Contains(r)))
            .Concat(refused).Distinct().Take(2).ToList();
    }

    // The halos that don't match the element only blast their own spot: the safe half.
    public bool SmallHalosWest => HeavensFire ^ FlameWest;

    public bool IsTethered(PartyRole role) => Tethered.Contains(role);

    public static bool IsSupport(PartyRole role) => role < PartyRole.MeleeDpsA;
}
