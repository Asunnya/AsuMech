using AsuMech.Core.SimObjects;

namespace AsuMech.Scenarios.Fru;

// A battle talk line at a time from scenario start. Ryne's and Gaia's lines name no actor; a boss's
// own lines name the boss.
public readonly record struct FruBattleTalk(float At, uint Line, uint Speaker)
{
    private const uint NoActor = 0xE0000000;

    public void Say(SimWorld world, SimEnemy? boss)
        => world.Map.DirectorUpdate(0x80000027U, Line, 0x2U, Speaker, boss is { } b && Speaker != FruConstants.RyneBattleTalk && Speaker != FruConstants.GaiaBattleTalk ? b.EntityId : NoActor);
}
