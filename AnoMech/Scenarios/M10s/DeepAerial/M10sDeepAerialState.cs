using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.M10s.DeepAerial;

public sealed class M10sDeepAerialState
{
    public const int Rounds = 6;

    // Both brothers start on the north intercardinals, else on the south ones.
    public bool BossesNorth { get; }
    // Per round, who takes Red Hot's fire tether and who takes Deep Blue's water tether.
    public IReadOnlyList<(PartyRole Fire, PartyRole Water)> Tethers { get; }

    public int FireWavesThroughThePrison { get; set; }
    // A water wave through the prison shields it; a fire wave before this time does nothing.
    public float PrisonShieldedUntil { get; set; }

    public static readonly PartyRole[] Tetherable =
        [PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    public M10sDeepAerialState(Rng rng, M10sDeepAerialStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M10sDeepAerialStateOverrides();
        var north = rng.NextBool();
        BossesNorth = overrides.BossesNorth ?? north;
        var first = Half(rng, player, overrides.PlayerFirstTether);
        var second = Half(rng, player, overrides.PlayerSecondTether);
        Tethers = overrides.Tethers ?? [.. first, .. second];
    }

    private static List<(PartyRole, PartyRole)> Half(Rng rng, PartyRole? player, int? pick)
    {
        var order = rng.Shuffle(Tetherable).ToList();
        if (player is { } me && Tetherable.Contains(me) && pick is { } slot and >= 0 and < 6)
        {
            order.Remove(me);
            order.Insert(slot, me);
        }
        return [.. Enumerable.Range(0, 3).Select(r => (order[2 * r], order[2 * r + 1]))];
    }

    // -1 when the brothers start north, +1 south.
    public float Side => BossesNorth ? -1f : 1f;
}
