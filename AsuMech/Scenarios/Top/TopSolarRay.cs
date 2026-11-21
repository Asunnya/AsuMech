using System;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Top;

// Omega's tank buster pair between P5's trios: a cast on its target, then an instant second hit on
// whoever holds it by then. The first hit's vuln makes the second lethal on the same tank, so the
// other tank provokes in between.
public sealed class TopSolarRay(SimWorld world, Func<SimEnemy?> boss)
{
    public const float FirstHitAfterCast = 5.00f;
    public const float SecondHitAfterFirst = 3.17f;

    private PartyRole holder = PartyRole.MainTank;

    // `castAt` is a scenario time before the run's t=0, inside the lead-in a Delay opened.
    public void Schedule(float castAt, EnemyAction first, EnemyAction second)
    {
        LeadIn(castAt, () => CastFirst(first));
        LeadIn(castAt + FirstHitAfterCast + SecondHitAfterFirst, () => Cast(second));
    }

    private void LeadIn(float scenarioTime, Action action) => world.Events.Add(scenarioTime - world.Events.Elapsed, action);

    private void CastFirst(EnemyAction action)
    {
        holder = PartyRole.MainTank;
        if (boss() is not { } omega) return;
        omega.Provoked -= TakeEnmity;
        omega.Provoked += TakeEnmity;
        Cast(action);
    }

    private void Cast(EnemyAction action)
    {
        if (boss() is not { } omega || Holder() is not { } tank) return;
        omega.SetTarget(tank, follow: false);
        omega.Cast(action, tank);
    }

    private void TakeEnmity(SimCharacter by)
    {
        if (by is ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank } tank) holder = tank.Role;
    }

    private SimCharacter? Holder()
    {
        if (world.Party.Get(holder) is { } held && held.IsAlive()) return held;
        var other = holder == PartyRole.MainTank ? PartyRole.OffTank : PartyRole.MainTank;
        return world.Party.Get(other) is { } next && next.IsAlive() ? next : null;
    }
}
