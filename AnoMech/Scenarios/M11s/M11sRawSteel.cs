using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using System.Numerics;
using static AnoMech.Scenarios.M11s.M11sConstants;
using Actions = AnoMech.Scenarios.M11s.M11sActions;

namespace AnoMech.Scenarios.M11s;

// Raw Steel Trophy. Axe: the main tank's shared buster and six spreads. Scythe: a cone on each tank
// and one party cone on a healer, all from the boss.
public sealed class M11sRawSteel(SimWorld world, Func<SimEnemy?> tyrant)
{
    private readonly SimEnemy?[] coneHelpers = new SimEnemy?[3];
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[6];

    public void Spawn()
    {
        for (var i = 0; i < coneHelpers.Length; i++) coneHelpers[i] = SpawnHelper();
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper();
    }

    public void Trophy(bool axe) => tyrant()?.Cast(axe ? ActionId.RawSteelTrophyAxe : ActionId.RawSteelTrophyScythe, animationLock: 0.2f);

    public void Swing(bool axe) => tyrant()?.Cast(axe ? ActionId.RawSteelAxe : ActionId.RawSteelScythe, animationLock: 0.2f);

    public void Follow(bool axe)
    {
        if (axe)
            tyrant()?.Cast(Actions.RawSteelAxeStack, Alive(PartyRole.MainTank) ?? Alive(PartyRole.OffTank));
        else
            tyrant()?.Cast(ActionId.RawSteelScytheFollow, animationLock: 0.2f);
    }

    public void Hit(bool axe, PartyRole heavyHitterTarget)
    {
        if (tyrant() is not { } boss) return;
        if (axe)
        {
            var i = 0;
            foreach (var member in AliveNonTanks())
            {
                if (i >= spreadHelpers.Length) break;
                spreadHelpers[i]?.SetPosition(boss.Placement());
                spreadHelpers[i++]?.Cast(Actions.Impact, member);
            }
            return;
        }
        PartyRole[] tanks = [PartyRole.MainTank, PartyRole.OffTank];
        for (var t = 0; t < tanks.Length; t++)
        {
            if (Alive(tanks[t]) is not { } tank) continue;
            coneHelpers[t]?.SetPosition(boss.Placement());
            coneHelpers[t]?.Cast(Actions.RawSteelScytheCone, tank);
        }
        var hitter = Alive(heavyHitterTarget) ?? AliveNonTanks().FirstOrDefault();
        coneHelpers[2]?.SetPosition(boss.Placement());
        if (hitter != null) coneHelpers[2]?.Cast(Actions.HeavyHitter, hitter);
    }

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private List<SimCharacter> AliveNonTanks() => Enum.GetValues<PartyRole>().Where(r => !r.IsTank()).Select(Alive).OfType<SimCharacter>().ToList();

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.Tyrant,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));
}
