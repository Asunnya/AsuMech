using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top;

// The chain breaks, and Hello World Fail wipes the party, when its holder dies with the puddle still
// on them or one of its hits catches anyone besides its target or kills. Casts on a helper spawned
// the same frame play no animation, so each member's fail helper is spawned up front.
// Forwarded from each scenario's hooks.
public sealed class HelloWorldFailures : IWorldEventListener
{
    private readonly SimWorld world;
    private readonly Dictionary<SimCharacter, SimEnemy?> helpers = [];

    public HelloWorldFailures(SimWorld world) => this.world = world;

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

    // UNVERIFIED who casts it and when: every fail in the replays follows a holder's death.
    public void OnActionResolved(ActionResolved resolved)
    {
        if (!IsChainHit(resolved.Action.ActionId)) return;
        if (resolved.Hits.Count == 1 && resolved.Killed.Count == 0) return;
        if (resolved.Caster is SimEnemy chain) chain.Cast(TopActions.HelloWorldFail);
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
