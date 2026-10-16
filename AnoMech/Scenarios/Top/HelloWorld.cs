using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top;

// One Hello World puddle chain: the first hit lands on the starting holder, each jump on the party
// member closest to (near) or farthest from (distant) the last one hit.
public sealed class HelloWorld(SimParty party, PartyRole holder, bool near)
{
    private SimCharacter? currentTarget = party.Get(holder);
    private bool first = true;

    public Vector3? Position => currentTarget?.Position;

    public void SetPosition(SimEnemy? helper)
    {
        if (currentTarget == null || helper == null) return;
        helper.SetPosition(currentTarget.Placement());
    }

    public void CastSpell(SimEnemy? helper)
    {
        if (helper == null || currentTarget == null) return;
        if (first)
        {
            first = false;
            helper.Cast(near ? TopActions.HelloNearWorld : TopActions.HelloDistantWorld, currentTarget);
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
        helper.Cast(near ? TopActions.HelloNearWorldJump : TopActions.HelloDistantWorldJump, nextTarget);
        currentTarget = nextTarget;
    }
}
