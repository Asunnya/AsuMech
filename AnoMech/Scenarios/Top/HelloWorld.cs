using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top;

// One Hello World puddle chain: the first hit lands on the starting holder, each jump on the party
// member closest to (near) or farthest from (distant) the last one hit.
public sealed class HelloWorld(SimParty party, PartyRole holder, bool near)
{
    private const int ChainHits = 3;

    private SimCharacter? currentTarget = party.Get(holder);
    private SimEnemy? lastCaster;
    private int hits;

    public Vector3? Position => currentTarget?.Position;

    public bool JumpsAfter(SimCharacter caster) => caster == lastCaster && hits < ChainHits;

    public void SetPosition(SimEnemy? helper)
    {
        if (currentTarget == null || helper == null) return;
        helper.SetPosition(currentTarget.Placement());
    }

    public void CastSpell(SimEnemy? helper)
    {
        if (helper == null || currentTarget == null) return;
        if (hits == 0)
        {
            Hit(helper, near ? TopActions.HelloNearWorld : TopActions.HelloDistantWorld, currentTarget);
            return;
        }
        var nextTarget = near
            ? party.Find.Closest(currentTarget.Position, currentTarget)
            : party.Find.Farest(currentTarget.Position, currentTarget);
        if (nextTarget == null)
        {
            helper.Cast(TopActions.HelloWorldFail);
            return;
        }
        Hit(helper, near ? TopActions.HelloNearWorldJump : TopActions.HelloDistantWorldJump, nextTarget);
    }

    private void Hit(SimEnemy helper, EnemyAction action, SimCharacter target)
    {
        helper.Cast(action, target);
        currentTarget = target;
        lastCaster = helper;
        hits++;
    }
}
