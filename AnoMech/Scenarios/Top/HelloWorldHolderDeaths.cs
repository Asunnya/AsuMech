using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Top.TopConstants;

namespace AnoMech.Scenarios.Top;

// A Hello World holder dying with the puddle still on them fails it. Casts on a helper spawned the
// same frame play no animation, so each member's fail helper is spawned up front.
public sealed class HelloWorldHolderDeaths(SimWorld world)
{
    private readonly Dictionary<SimCharacter, SimEnemy?> helpers = [];

    public void SpawnHelpers()
    {
        foreach (var member in world.Party.AllMembers())
            helpers[member] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.OmegaHelper, Visibility: SpawnVisibility.InvisibleHelper,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(Vector3.Zero, 0f)));
    }

    public void Check()
    {
        foreach (var member in world.Party.AllMembers())
        {
            if (member.IsAlive()) continue;
            var status = member.HasStatus(StatusId.HelloNearWorld) ? StatusId.HelloNearWorld
                : member.HasStatus(StatusId.HelloDistantWorld) ? StatusId.HelloDistantWorld
                : (ushort)0;
            if (status == 0) continue;
            member.RemoveStatus(status);
            if (helpers.GetValueOrDefault(member) is not { } helper) continue;
            helper.SetPosition(member.Position);
            helper.Cast(TopActions.HelloWorldFail);
        }
    }
}
