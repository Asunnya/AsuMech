using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static AsuMech.Scenarios.Ucob.UcobConstants;

namespace AsuMech.Scenarios.Ucob;

// Invisible casters for effects that don't come from a boss's body. A helper spawned in the frame
// it acts drops the effect or cast, so they wait in a pool.
public sealed class UcobHelperPool(SimWorld world, uint nameId, ICollection<SimEnemy> spawned)
{
    private const float EffectCarrierLinger = 3f;

    private readonly Queue<SimEnemy> pool = [];

    public void Fill(int count)
    {
        pool.Clear();
        for (var i = 0; i < count; i++)
            if (Spawn(new Placement(Vector3.Zero, 0f)) is { } helper) pool.Enqueue(helper);
    }

    public SimEnemy? Take(Placement placement)
    {
        var helper = pool.TryDequeue(out var pooled) ? pooled : Spawn(placement);
        helper?.SetPosition(placement);
        return helper;
    }

    // Back in the pool once its cast (busyFor) and its effect have played out.
    public void Release(SimEnemy helper, float busyFor = 0f) => world.Events.Add(busyFor + EffectCarrierLinger, () => pool.Enqueue(helper));

    public void Play(uint actionId, Vector3 at, GameObjectId? target = null)
    {
        if (Take(new Placement(at, 0f)) is not { } helper) return;
        UcobSim.PlayEffect(helper, actionId, 1.1f, target: target, at: at);
        Release(helper);
    }

    public void Clear() => pool.Clear();

    private SimEnemy? Spawn(Placement placement)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Dummy,
            NameId: nameId,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Visibility: SimEnemy.LegacyVisibility(true, true),
            Placement: placement));
        if (helper != null) spawned.Add(helper);
        return helper;
    }
}
