using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top;

// A Hello World holder dying with the puddle still on them fails it. Casts on a helper spawned the
// same frame play no animation, so each member's fail helper is spawned up front.
public sealed class HelloWorldHolderDeaths
{
    private readonly SimWorld world;
    private readonly Dictionary<SimCharacter, SimEnemy?> helpers = [];

    public HelloWorldHolderDeaths(SimWorld world)
    {
        this.world = world;
        world.OnDeathWithStatus(StatusId.HelloNearWorld, Fail);
        world.OnDeathWithStatus(StatusId.HelloDistantWorld, Fail);
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

    private void Fail(SimCharacter member)
    {
        if (helpers.GetValueOrDefault(member) is not { } helper) return;
        helper.SetPosition(member.Position);
        helper.Cast(TopActions.HelloWorldFail);
    }
}
