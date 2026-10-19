using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.M10s.InsaneAir;

namespace AnoMech.Scenarios.M10s.XtremeSnaking;

public sealed class M10sXtremeSnakingState
{
    public const int Rounds = 4;

    // Both boards make the same move at each stop: two Snaps, one Re-entry and one Vertical.
    // UNVERIFIED: the last move of the fourth; that pull ended before it.
    public static readonly SurfMove[][] Patterns =
    [
        [SurfMove.Snap, SurfMove.Vertical, SurfMove.Reentry, SurfMove.Snap],
        [SurfMove.Snap, SurfMove.Reentry, SurfMove.Snap, SurfMove.Vertical],
        [SurfMove.Vertical, SurfMove.Snap, SurfMove.Reentry, SurfMove.Snap],
        [SurfMove.Snap, SurfMove.Reentry, SurfMove.Vertical, SurfMove.Snap],
    ];

    // Xtreme Firesnaking takes the four players nearest Red Hot, or the four furthest.
    public bool FireOnTheNearest { get; }
    public IReadOnlyList<SurfMove> Moves { get; }
    // UNVERIFIED: whether the ending is rolled apart from the moves; the logs show both with Snap first.
    public bool RedHotEndsInTheMiddle { get; }

    // Filled when the snakes land, since who is fire depends on where everyone stood.
    public HashSet<PartyRole> Fire { get; } = [];

    public M10sXtremeSnakingState(Rng rng, M10sXtremeSnakingStateOverrides? overrides = null, PartyRole? player = null)
    {
        overrides ??= new M10sXtremeSnakingStateOverrides();
        var nearest = rng.NextBool();
        if (player is { } me && overrides.PlayerFire is { } fire) nearest = fire == StartsByRedHot(me);
        FireOnTheNearest = overrides.FireOnTheNearest ?? nearest;
        var pattern = rng.Next(Patterns.Length);
        Moves = Patterns[overrides.Pattern is { } p && p >= 0 && p < Patterns.Length ? p : pattern];
        var middle = rng.NextBool();
        RedHotEndsInTheMiddle = overrides.RedHotEndsInTheMiddle ?? middle;
    }

    // Light party two waits by Red Hot, light party one by Deep Blue.
    public static bool StartsByRedHot(PartyRole role) =>
        role is PartyRole.OffTank or PartyRole.ShieldHealer or PartyRole.MeleeDpsB or PartyRole.CasterDps;

    public int VerticalRound => Moves.ToList().IndexOf(SurfMove.Vertical);

    // The board stops are Insane Air's; only the moves differ.
    public InsaneAirVariant Boards => new("Xtreme", Moves, Moves, RedHotEndsInTheMiddle, []);
}
