using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top;

// The chain breaks, and Hello World Fail wipes the party, when its holder dies with the puddle still
// on them or a hit kills its target before the last jump. A hit on more than one player kills all of
// them. Casts on a helper spawned the same frame play no animation, so each member's fail helper is
// spawned up front. Forwarded from each scenario's hooks.
public sealed class HelloWorldFailures : IWorldEventListener
{
    private readonly SimWorld world;
    private readonly Dictionary<SimCharacter, SimEnemy?> helpers = [];
    private readonly List<HelloWorld> chains = [];

    public HelloWorldFailures(SimWorld world) => this.world = world;

    public HelloWorld Chain(PartyRole holder, bool near)
    {
        var chain = new HelloWorld(world.Party, holder, near);
        chains.Add(chain);
        return chain;
    }

    public void SpawnHelpers()
    {
        foreach (var member in world.Party.AllMembers())
            helpers[member] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.OmegaHelper, Visibility: SpawnVisibility.InvisibleHelper,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(Vector3.Zero, 0f)));
    }

    public void OnStatusLost(StatusLost lost)
    {
        if (lost is not { StatusId: StatusId.HelloNearWorld or StatusId.HelloDistantWorld, Reason: StatusLossReason.Died }) return;
        if (helpers.GetValueOrDefault(lost.Holder) is not { } helper) return;
        helper.SetPosition(lost.Holder.Position);
        helper.Cast(TopActions.HelloWorldFail);
    }

    public void OnActionResolved(ActionResolved resolved)
    {
        if (!IsChainHit(resolved.Action.ActionId) || resolved.Killed.Count == 0) return;
        if (resolved.Caster is not SimEnemy caster || !chains.Any(c => c.JumpsAfter(caster))) return;
        caster.Cast(TopActions.HelloWorldFail);
    }

    // UNVERIFIED: no pull overloads Dynamis, so what retail does then (Doom, as for Come Ruin?) is unknown.
    public void OnStatusAdded(StatusAdded added)
    {
        if (added is not { StatusId: StatusId.QuickeningDynamis, PreviousStacks: >= MaxDynamisStacks, Action: { } action, Caster: SimEnemy chain }
            || !IsChainHit(action.ActionId)) return;
        chain.Cast(TopActions.HelloWorldFail);
    }

    private const int MaxDynamisStacks = 3;

    private static bool IsChainHit(uint actionId)
        => actionId is ActionId.HelloNearWorld or ActionId.HelloNearWorldJump or ActionId.HelloDistantWorld or ActionId.HelloDistantWorldJump;
}
