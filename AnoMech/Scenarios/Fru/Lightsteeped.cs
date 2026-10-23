using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru;

// For OnStatusAdded in every scenario that stacks Lightsteeped.
public static class Lightsteeped
{
    // The action that lands a fifth stack has its caster follow up with Lightsteep.
    public static void ExplodeFifthStack(StatusAdded added)
    {
        if (added is { StatusId: StatusId.Lightsteeped, Stacks: >= 5, Caster: SimEnemy caster }) caster.Cast(FruActions.Lightsteep);
    }
}
