using System.Collections.Generic;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.Fru.FruConstants;
using static AsuMech.Scenarios.Fru.P2QuadrupleSlap.FruP2QuadrupleSlapState;
using Actions = AsuMech.Scenarios.Fru.FruActions;

namespace AsuMech.Scenarios.Fru.P2QuadrupleSlap;

// t=0 is the Usurper spawning as Fatebreaker falls.
public sealed class FruP2QuadrupleSlapScenario : IScenario
{
    public string Name => "Quadruple Slap";
    public IPhase Phase => FruZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP2QuadrupleSlapAi()];

    private SimWorld world = null!;
    private SimEnemy? usurper;
    private PartyRole enmity;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        enmity = PartyRole.MainTank;
        var state = new FruP2QuadrupleSlapState();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<FruP2QuadrupleSlapState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, SpawnUsurper);
        world.Events.Add(TargetableAt, () => world.Map.DirectorUpdate(0x80000027U, 0x3, 0x2U, UsurperBattleTalk, usurper?.EntityId ?? 0));
        world.Events.Add(TargetableAt, Appear);
        world.Events.Add(FirstSlapAt, () => usurper?.SetModeAttributeFlags(0xF3));
        world.Events.Add(FirstSlapAt, () => Slap(Actions.QuadrupleSlap, marked: true));
        world.Events.Add(16.2f, () => usurper?.SetModelState(5));
        world.Events.Add(SecondSlapAt, () => Slap(Actions.QuadrupleSlapSecond, marked: false));
        world.Events.Add(20.3f, () => usurper?.SetModelState(0));
        world.Events.Add(22.55f, () => usurper?.SetModeAttributeFlags(0xF0));
        world.Events.Add(22.55f, () => usurper?.SetModelState(4));
    }

    private void SpawnUsurper()
    {
        usurper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.UsurperP2,
            NameId: BNpcNameId.Usurper,
            Level: Level,
            Visibility: SpawnVisibility.HiddenUntilShown,
            Placement: new Placement(Vector3.Zero, 0f),
            InitialModeAttributeFlags: 0xF0,
            LayoutId: LayoutId.UsurperP2));
        if (usurper is not null) usurper.Provoked += TakeEnmity;
    }

    private void Appear()
    {
        if (usurper is not { } boss) return;
        boss.SetTargetable(true);
        boss.PlayActionTimeline(ActionTimelineId.WarpEnd);
        boss.SetTarget(Tank(), follow: false);
    }

    private void TakeEnmity(SimCharacter by)
    {
        if (by is not ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank } tank) return;
        enmity = tank.Role;
        usurper?.SetTarget(by, follow: false);
    }

    private SimCharacter? Tank()
    {
        if (world.Party.Get(enmity) is { } holder && holder.IsAlive()) return holder;
        var other = enmity == PartyRole.MainTank ? PartyRole.OffTank : PartyRole.MainTank;
        return world.Party.Get(other) is { } next && next.IsAlive() ? next : null;
    }

    // UNVERIFIED that a Provoke between the slaps moves the second: every pull leaves both on one tank.
    private void Slap(EnemyAction action, bool marked)
    {
        if (usurper is not { } boss || Tank() is not { } tank) return;
        if (marked) tank.AttachLockonVfx(LockonId.QuadrupleSlap);
        boss.Cast(action, tank);
    }
}
