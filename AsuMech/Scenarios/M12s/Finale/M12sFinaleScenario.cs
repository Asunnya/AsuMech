using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.M12s.IdyllicDream;
using static AsuMech.Scenarios.M12s.M12sConstants;
using Actions = AsuMech.Scenarios.M12s.M12sActions;

namespace AsuMech.Scenarios.M12s.Finale;

// t=0 = P2 7:44, three seconds before the second Idyllic Dream. Double Sobat on the main tank and its
// second kick, Esoteric Finisher on both tanks, then Replication's clones join two Arcadian Hells.
// Retail's kill is a DPS check after the second Hell, so the run ends there.
public sealed class M12sFinaleScenario : IScenario
{
    public string Name => "Finale";
    public IPhase Phase => M12sZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sFinaleAi()];

    public const float IdyllicDreamCastAt = 3f;
    public const float SobatMarkedAt = 11.06f;
    public const float SobatCastAt = 11.15f;
    public const float FirstKickAt = 16.71f;
    public const float SecondKickLandsAt = 21.45f;
    public const float FinisherAt = 23.75f;
    public const float ReplicationCastAt = 32.58f;
    public static readonly float[] ClonesShowAt = [40.74f, 54.21f];
    public static readonly float[] ArcadianHellCastAt = [46.09f, 62.36f];

    private SimWorld world = null!;
    private SimEnemy? lindwurm;
    private readonly List<SimEnemy?> fourClones = [];
    private readonly List<SimEnemy?> eightClones = [];
    private readonly List<SimEnemy?> helpers = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        fourClones.Clear();
        eightClones.Clear();
        helpers.Clear();
        var state = new M12sFinaleState();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sFinaleState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new CircleArena(M12sIdyllicDreamState.ArenaRadius), "Fell off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(DnP2Waymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(0.5f, TankTheBoss);
        world.Events.Add(IdyllicDreamCastAt, () => lindwurm?.Cast(Actions.IdyllicDream));
        world.Events.Add(SobatMarkedAt, () => Alive(PartyRole.MainTank)?.AttachLockonVfx(LockonId.DoubleSobat));
        world.Events.Add(SobatCastAt, () => lindwurm?.Cast(ActionId.DoubleSobat, animationLock: 0.2f));
        world.Events.Add(FirstKickAt, DoubleSobat);
        world.Events.Add(FinisherAt, EsotericFinisher);
        world.Events.Add(ReplicationCastAt, () => lindwurm?.Cast(ActionId.ReplicationFinale, animationLock: 2.1f));
        world.Events.Add(ClonesShowAt[0], () => Show(fourClones));
        world.Events.Add(ArcadianHellCastAt[0], () => ArcadianHell(fourClones, Actions.ArcadianHellFour));
        world.Events.Add(ClonesShowAt[1], () =>
        {
            Hide(fourClones);
            Show(eightClones);
        });
        world.Events.Add(ArcadianHellCastAt[1], () => ArcadianHell(eightClones, Actions.ArcadianHellEight));
    }

    private void SpawnActors()
    {
        lindwurm = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP2,
            NameId: BNpcNameId.LindwurmP2,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        foreach (var spot in M12sFinaleState.FourClones) fourClones.Add(SpawnClone(spot));
        foreach (var spot in M12sFinaleState.EightClones) eightClones.Add(SpawnClone(spot));
        for (var i = 0; i < 4; i++)
            helpers.Add(world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Helper,
                NameId: BNpcNameId.LindwurmP2,
                Level: 1,
                EnemyList: EnemyListMode.Never,
                Visibility: SpawnVisibility.InvisibleHelper)));
    }

    private SimEnemy? SpawnClone(Vector3 spot)
    {
        var heading = MathF.Abs(spot.X) > MathF.Abs(spot.Z) ? -MathF.Sign(spot.X) * MathF.PI / 2f : (spot.Z < 0f ? 0f : MathF.PI);
        var clone = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Lindschrat,
            NameId: BNpcNameId.Lindschrat,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(spot, heading)));
        clone?.SetVisible(false);
        return clone;
    }

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private void TankTheBoss() => lindwurm?.SetTarget(Alive(PartyRole.MainTank), follow: false);

    private static void Show(List<SimEnemy?> clones) => clones.ForEach(c => c?.SetVisible(true));

    private static void Hide(List<SimEnemy?> clones) => clones.ForEach(c => c?.SetVisible(false));

    // The first kick lands on the main tank; the second, cast from the centre, kicks the same way again.
    private void DoubleSobat()
    {
        if (lindwurm is not { } boss || Alive(PartyRole.MainTank) is not { } tank) return;
        boss.Cast(Actions.DoubleSobatFirst, tank);
        helpers[0]?.SetPosition(new Placement(Vector3.Zero, MathF.Atan2(tank.Position.X, tank.Position.Z)));
        helpers[0]?.Cast(Actions.DoubleSobatSecond);
    }

    private void EsotericFinisher()
    {
        PartyRole[] tanks = [PartyRole.MainTank, PartyRole.OffTank];
        for (var i = 0; i < tanks.Length; i++)
        {
            if (Alive(tanks[i]) is not { } tank) continue;
            helpers[1 + i]?.SetPosition(new Placement(Vector3.Zero, 0f));
            helpers[1 + i]?.Cast(Actions.EsotericFinisher, tank);
        }
    }

    private void ArcadianHell(List<SimEnemy?> clones, EnemyAction cloneHit)
    {
        lindwurm?.Cast(Actions.ArcadianHell);
        foreach (var clone in clones) clone?.Cast(cloneHit);
    }
}
