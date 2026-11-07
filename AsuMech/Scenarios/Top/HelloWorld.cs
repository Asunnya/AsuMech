using System.Linq;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Top.TopConstants;

namespace AsuMech.Scenarios.Top;

// One Hello World puddle chain: the first hit lands on the starting holder, each jump on the party
// member closest to (near) or farthest from (distant) the last one hit, cast by a helper standing on
// the last one hit.
public sealed class HelloWorld(SimParty party, PartyRole holder, bool near)
{
    private const int ChainHits = 3;

    private SimCharacter? currentTarget = party.Get(holder);
    private EnemyAction? lastAction;
    private int hits;

    // The resolve is this chain's latest hit and another one follows it.
    public bool JumpsAfter(ActionResolved resolved)
        => resolved.Action == lastAction && currentTarget is not null && resolved.Hits.Contains(currentTarget) && hits < ChainHits;

    public void CastSpell(SimEnemy? caster)
    {
        if (caster == null || currentTarget == null) return;
        var from = currentTarget.Placement();
        if (hits == 0)
        {
            Hit(caster, near ? TopActions.HelloNearWorld : TopActions.HelloDistantWorld, currentTarget, from);
            return;
        }
        var nextTarget = near
            ? party.Find.Closest(currentTarget.Position, currentTarget)
            : party.Find.Farest(currentTarget.Position, currentTarget);
        if (nextTarget == null)
        {
            caster.CastFromHelper(TopActions.HelloWorldFail, at: from);
            return;
        }
        Hit(caster, near ? TopActions.HelloNearWorldJump : TopActions.HelloDistantWorldJump, nextTarget, from);
    }

    private void Hit(SimEnemy caster, EnemyAction action, SimCharacter target, Placement from)
    {
        caster.CastFromHelper(action, target, at: from);
        currentTarget = target;
        lastAction = action;
        hits++;
    }

    // Hello World leaves everyone an Underflow and a Performance Debugger, and four of them a
    // Synchronization Debugger (its second and last stack holders), until Blue Screen.
    public static void ApplyDebuggers(SimParty party, Rng rng)
    {
        var synchronization = rng.Shuffle(PerRole.All.ToArray()).Take(4).ToHashSet();
        foreach (var role in PerRole.All)
        {
            if (party.Get(role) is not { } member) continue;
            member.AddStatus(StatusId.HWImmuneRedRot);
            member.AddStatus(StatusId.HWImmuneBlueRot);
            if (synchronization.Contains(role)) member.AddStatus(StatusId.HWImmuneStack);
        }
    }

    public static void RemoveDebuggers(SimParty party)
    {
        foreach (var member in party.AllMembers())
        {
            member.RemoveStatus(StatusId.HWImmuneRedRot);
            member.RemoveStatus(StatusId.HWImmuneBlueRot);
            member.RemoveStatus(StatusId.HWImmuneStack);
        }
    }
}
