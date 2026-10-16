using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Map;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Scenarios.Top.TopConstants;
using Actions = AnoMech.Scenarios.Top.TopActions;

namespace AnoMech.Scenarios.Top.P5Sigma;

public sealed class TopP5SigmaScenario : IMultiplayerReplayable
{
    public string Name => "Sigma";
    public IPhase Phase => TopZone.P5;
    public bool SupportsMultiplayer => true;

    public void DrawSettings() => settingsWindow.Draw();
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settingsWindow.DrawPerPlayer();
    public object SettingsOverrides => settingsWindow.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settingsWindow.Overrides.Validate().Problems;
    private readonly TopP5SigmaSettingsWindow settingsWindow = new();

    public IReadOnlyList<IScenarioAi> AiStrats => [new TopP5SigmaAi()];


    private TopP5SigmaState state = null!;
    private SimWorld world = null!;
    private SimParty party = null!;
    private HelloWorldHolderDeaths holderDeaths = null!;
    private SimEnemy?[] helpers = null!;

    // Exposed so MultiplayerManager can read the AI-relevant subset after a host Start and
    // broadcast it -- see UmadP3BlackHoleScenario.LastState for the pattern.
    public TopP5SigmaState? LastState { get; private set; }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = worldParam.Party;
        state = new TopP5SigmaState(world.Rng, party, settingsWindow.Overrides);
        LastState = state;
        holderDeaths = new HelloWorldHolderDeaths(world);
        helpers = new SimEnemy?[6];
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<TopP5SigmaState>)AiStrats[idx]).Run(state, world);

        Run_Omega_M_4000A63C();
        Run_Omega_4000A68F();
        Run_Omega_4000A690();
        Run_Right_Arm_Unit_4000A643();
        Run_Omega_4000A408();
        Run_SuperfluidHelper();
        Run_EventObj_1EB83C_4000A6E7();
        Run_EventObj_1EB83E_4000A6E8();
        Run_Rear_Power_Unit_4000A641();
        Run_Omega_F_4000A40B_2();
        Run_Omega_F_4000A40C_2();
        Run_PlayerTethers();
        Run_OtherDebuffs();
        Run_PlayerLockons();
    }
    
    public void Tick(float delta, float elapsed)
    {
    }
    
    private void Run_PlayerTethers()
    {
        world.Events.Add(11.82f, () =>
        {
            state.Order.ForEachPair((p1, p2) => world.Tether(
                p1, p2,
                TetherId.Glitch , duration: 32.000f,
                debuffStatusId: state.GlitchType.StatusId)
                     .SetConditionalStatus(StatusId.VulnerabilityUp, state.GlitchType.Condition)
            );
        });
    }

    private void Run_OtherDebuffs()
    {
        state.DynamisTargets.ForEach(p => p.AddStatus(StatusId.QuickeningDynamis, stacks: 1));
        world.Events.Add(0f, holderDeaths.SpawnHelpers);
        world.Events.Add(11.82f, () => state.HelloWorldTargets.Get(0)?.AddStatus(StatusId.HelloNearWorld, 56.000f));
        world.Events.Add(11.82f, () => state.HelloWorldTargets.Get(1)?.AddStatus(StatusId.HelloDistantWorld, 56.000f));
        world.Events.Add(28.39f, () => party.ForEachActive(member => member.AddStatus(StatusId.Looper, 18.000f)));
    }

    private void Run_PlayerLockons()
    {
        world.Events.Add(11.82f, () => state.Order.ForEachPair((i, p1, p2) =>
        {
            p1.AttachLockonVfx(LockonId.Playstation[i]); 
            p2.AttachLockonVfx(LockonId.Playstation[i]); 
        }));
        world.Events.Add(21.9f, () => state.WaveCannonTargets.ForEach(p => p.AttachLockonVfx(LockonId.WaveCannon)));
    }


    private void Run_Omega_M_4000A63C()
    {
        SimEnemy? omega_M_4000A63C = null;
        world.Events.Add(0f, () => omega_M_4000A63C = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaMDynamis, NameId: BNpcNameId.OmegaMDynamis, Level: 90, Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible, LayoutId: BNpcLayoutId.OmegaMDynamis, Placement: new Placement(new Vector3(0.000f, 0.000f, 5.000f), MathF.PI), InitialModeAttributeFlags: 0x32)));
        world.Events.Add(0.1f, () => omega_M_4000A63C?.AddStatus(StatusId.OmegaM));
        world.Events.Add(0.1f, () => omega_M_4000A63C?.SetTarget(party.Get(PartyRole.MainTank), follow: false));
        world.Events.Add(2.46f, () => omega_M_4000A63C?.Cast(ActionId.Teleport7b42, Vector3.Zero, animationLock: 1.1f));
        world.Events.Add(3.75f, () => omega_M_4000A63C?.Cast(Actions.RunMiSigmaVersion));
        world.Events.Add(11.82f, () => omega_M_4000A63C?.SetTargetable(false));
        world.Events.Add(11.87f, () => omega_M_4000A63C?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(13.88f, () => omega_M_4000A63C?.SetPosition(OmegaMSpot));
        world.Events.Add(13.96f, () => omega_M_4000A63C?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(26.16f, () => omega_M_4000A63C?.Cast(ActionId.SubjectSimulationFDynamis, animationLock: 2.1f));
        world.Events.Add(27.23f, () => omega_M_4000A63C?.SetModelState(0x06));
        world.Events.Add(27.23f, () => omega_M_4000A63C?.RemoveStatus(StatusId.OmegaM));
        world.Events.Add(27.23f, () => omega_M_4000A63C?.AddStatus(StatusId.Superfluid, stacks: 493, overrideStacks: true)?.ShownAsFrom(omega_M_4000A63C));
        world.Events.Add(28.25f, () => omega_M_4000A63C?.Cast(ActionId.SubjectSimulationFWarpDown, animationLock: 4.1f));
        world.Events.Add(28.79f, () => omega_M_4000A63C?.SetModelState(0x0B));
        world.Events.Add(32.36f, () => omega_M_4000A63C?.Cast(ActionId.Unknown7f30, animationLock: 4.1f));
        world.Events.Add(36.02f, () => omega_M_4000A63C?.SetModelState(0x05));
        world.Events.Add(36.02f, () => omega_M_4000A63C?.RemoveStatus(StatusId.Superfluid));
        world.Events.Add(36.02f, () => omega_M_4000A63C?.AddStatus(StatusId.OmegaF, stacks: 492, overrideStacks: true)?.ShownAsFrom(omega_M_4000A63C));
        world.Events.Add(36.47f, () => omega_M_4000A63C?.Cast(ActionId.Unknown7b20, animationLock: 2.1f));
        world.Events.Add(37.13f, () => omega_M_4000A63C?.SetModelState(0x0B));
        world.Events.Add(38.56f, () => omega_M_4000A63C?.Cast(ActionId.Teleport7b43, Vector3.Zero, animationLock: 1.1f));
        world.Events.Add(39.3f, () => omega_M_4000A63C?.SetPosition(Vector3.Zero));
        world.Events.Add(39.68f, () => omega_M_4000A63C?.Cast(Actions.Discharger));
        world.Events.Add(42.79f, () => omega_M_4000A63C?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(45.82f, () =>
        {
            if (state.OmegaFAttack != OmegaAttack.Legs) return;
            omega_M_4000A63C?.SetModeAttributeFlags(state.OmegaFAttack.AttributeFlags);
            omega_M_4000A63C?.SetModelState(0x04);
        });
        world.Events.Add(45.88f, () => omega_M_4000A63C?.SetPosition(state.NewNorthB.Apply(new Placement(new Vector3(0f, 0f, -10f), 0))));
        world.Events.Add(45.97f, () => omega_M_4000A63C?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(59.62f, () => omega_M_4000A63C?.Cast(state.OmegaFAttack.Action));
        world.Events.Add(64.21f, () => omega_M_4000A63C?.PlayActionTimeline(ActionTimelineId.WarpStart));
        // UNVERIFIED: no replay shows the legs form reverting.
        world.Events.Add(66.26f, () =>
        {
            if (state.OmegaFAttack != OmegaAttack.Legs) return;
            omega_M_4000A63C?.SetModeAttributeFlags(0x32);
            omega_M_4000A63C?.SetModelState(0x00);
        });
        world.Events.Add(69.29f, () => omega_M_4000A63C?.SetPosition(new Placement(Vector3.Zero, 3.142f)));
        world.Events.Add(69.38f, () => omega_M_4000A63C?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(72.46f, () => omega_M_4000A63C?.SetTargetable(true));
    }


    private void Run_Omega_4000A68F()
    {
        SimEnemy? omega_4000A68F = null;
        world.Events.Add(3.84f, () => omega_4000A68F = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.BeetleHelper, NameId: BNpcNameId.OmegaBeetle, Level: 90, Targetable: false, EnemyList: EnemyListMode.OnlyWhenVisible, Visibility: SpawnVisibility.HiddenUntilShown, LayoutId: BNpcLayoutId.BeetleHelper, Placement: state.NewNorthA.Apply(new Placement(new Vector3(0f, 0f, 20f), MathF.PI)))));
        world.Events.Add(19.93f, () => omega_4000A68F?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(27.63f, () => omega_4000A68F?.Cast(ActionId.ProgramLoop, animationLock: 3.1f));
        world.Events.Add(30.75f, () => omega_4000A68F?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(44.87f, () => omega_4000A68F?.Despawn());
    }

    private void Run_Omega_4000A690()
    {
        SimEnemy? omega_4000A690 = null;
        world.Events.Add(3.84f, () => omega_4000A690 = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.FinalHelper, NameId: BNpcNameId.OmegaFinal, Level: 90, Targetable: false, EnemyList: EnemyListMode.Manual, Visibility: SpawnVisibility.HiddenUntilShown, LayoutId: BNpcLayoutId.FinalHelper, Placement: state.NewNorthA.Apply(new Placement(new Vector3(0.000f, -0.000f, 0.000f), 0)))));
        world.Events.Add(12.27f, () => omega_4000A690?.SetVisibleInEnemyList(true));
        world.Events.Add(16.95f, () => omega_4000A690?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(22f, () => omega_4000A690?.Cast(ActionId.WaveCannon, animationLock: 3.1f));
        world.Events.Add(33.25f, () => omega_4000A690?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(35.65f, () => omega_4000A690?.Despawn());
    }

    private void Run_Right_Arm_Unit_4000A643()
    {
        for(int i = 0; i < 2; i++)
        {
            var offset = i * 2 - 1; // -1, 1
            SimEnemy? unit = null;
            SimTether? tether = null;
            world.Events.Add(1, () => unit = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.RightArmUnit, NameId: BNpcNameId.RightArmUnit, Level: 90, Targetable: false, EnemyList: EnemyListMode.OnlyWhenVisible, Visibility: SpawnVisibility.HiddenUntilShown, LayoutId: BNpcLayoutId.RightArmUnit,
                                                                  Placement: state.NewNorthA.Apply(new Placement(new Vector3(7.07f * offset, -0.000f, 7.07f), 0f)).Face(Vector3.Zero))));
            world.Events.Add(11.91f, () => unit?.PlayActionTimeline(ActionTimelineId.WarpEnd));
            world.Events.Add(12.53f, () => tether = world.TetherFarestPlayer(unit, TetherId.AutoTarget)
                                                         .SetAutoFaceTarget(true));
            world.Events.Add(30.84f, () => tether?.Despawn());
            world.Events.Add(30.93f, () => unit?.Cast(Actions.HyperPulseSigma));
            world.Events.Add(32.98f, () => unit?.PlayActionTimeline(ActionTimelineId.WarpStart));
            world.Events.Add(33.2f, () => unit?.SetTarget(null));
            world.Events.Add(45.88f, () => unit?.SetPosition(state.NewNorthB.Apply(new Placement(new Vector3(14.14f * offset, 0.000f, 14.14f), 0f)).Face(Vector3.Zero)));
            world.Events.Add(45.97f, () => unit?.PlayActionTimeline(ActionTimelineId.WarpEnd));
            world.Events.Add(46.59f, () => tether = world.TetherFarestPlayer(unit, TetherId.AutoTarget)
                  .SetAutoFaceTarget(true));
            world.Events.Add(67.85f, () => tether?.Despawn());
            world.Events.Add(67.97f, () => unit?.Cast(Actions.HyperPulseSigma));
            world.Events.Add(70.05f, () => unit?.PlayActionTimeline(ActionTimelineId.WarpStart));
            world.Events.Add(70.24f, () => unit?.SetTarget(null));
        }
    }

    private void Run_Omega_4000A408()
    {
        HelloWorld[] solvers = [
            new HelloWorld(world.Party, state.HelloWorldTargets[0], true),
            new HelloWorld(world.Party, state.HelloWorldTargets[1], false)];
        for (int index = 0; index < 6; index++)
        {
            var i = index;
            var target = state.WaveCannonTargets.Get(i);
            var tower = state.Towers[i];
            var towerLocation = state.Towers[i]?.Position;
            var helloWorldOffset = i / 2;
            var solverId = i % 2;
            
            world.Events.Add(1f, () => helpers[i] = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaHelper, NameId: BNpcNameId.OmegaBeetle, Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper, Placement: new Placement(new Vector3(0.000f, 0.000f, 0.000f), -0.000f))));
            world.Events.Add(31.02f, () => FireWaveCannon(helpers[i], target));
            world.Events.Add(43.60f, () => helpers[i]?.SetPosition(new Placement(towerLocation ?? default, 0f)));
            world.Events.Add(43.69f, () =>
            {
                if (towerLocation != null)
                {
                    var inTower = party.Find.InsideCircle(towerLocation.Value, Geometry.TowerRadius);
                    helpers[i]?.Cast(tower?.MinPlayers == 1 ? Actions.StorageViolationSolo : Actions.StorageViolationPair);
                    if (inTower.Count < tower?.MinPlayers)
                        helpers[i]?.Cast(Actions.StorageViolationObliteration);
                }
            });
        
            world.Events.Add(67.7f + helloWorldOffset, () => solvers[solverId].SetPosition(helpers[i]));
            world.Events.Add(67.8f + helloWorldOffset, () => solvers[solverId].CastSpell(helpers[i]));
        }
    }

    private static void FireWaveCannon(SimEnemy? helper, SimCharacter? target)
    {
        if (helper == null || target == null) return;
        helper.SetPosition(new Placement(Vector3.Zero, 0f).Face(target.Position));
        helper.Cast(Actions.WaveCannon);
    }

    private Placement OmegaMSpot => state.NewNorthA.Apply(new Placement(new Vector3(0f, 0f, -20f), 0f));

    private void Run_SuperfluidHelper()
    {
        world.Events.Add(26.97f, () => helpers[5]?.SetPosition(OmegaMSpot));
        world.Events.Add(27.06f, () => helpers[5]?.Cast(ActionId.SuperfluidAnimationM, animationLock: 2.1f));
        world.Events.Add(35.84f, () => helpers[5]?.SetPosition(OmegaMSpot));
        world.Events.Add(35.93f, () => helpers[5]?.Cast(ActionId.SuperfluidAnimationF, animationLock: 2.1f));
    }

    private void Run_EventObj_1EB83C_4000A6E7()
    {
        for (int index = 0; index < 6; index++)
        {
            var i = index;
            var tower = state.Towers[i];
            if (tower == null) continue;
            SimEventObject? eventObj_1EB83C_4000A6E7 = null;
            var config = new EventObjectSpawnConfig { EObjId = EObjId.TowerTimer, Placement = new Placement(tower.Position, -0.000f), TimelineState = 2 };
            world.Events.Add(33.64f, () => eventObj_1EB83C_4000A6E7 = world.SpawnEventObject(BindTowerLayout(config, 0x40)));
            world.Events.Add(43.68f, () => eventObj_1EB83C_4000A6E7?.Despawn());
        }
    }

    private void Run_EventObj_1EB83E_4000A6E8()
    {
        for(int index = 0; index < 6; index++)
        {
            var i = index;
            var tower = state.Towers[i];
            if (tower == null) continue;
            var eObjId = tower.MinPlayers == 1 ? EObjId.TowerSolo : EObjId.TowerPair;
            ushort[] stateIds = tower.MinPlayers == 1 ? [0x8, 0x10] : [0x8, 0x10, 0x20];
            SimEventObject? eventObj_1EB83E_4000A6E8 = null;
            var config = new EventObjectSpawnConfig { EObjId = eObjId, Placement = new Placement(tower.Position, -0.000f) };
            world.Events.Add(33.64f, () => eventObj_1EB83E_4000A6E8 = world.SpawnTower(BindTowerLayout(config, 0x48), stateIds, EObjId.TowerStateUnk, Geometry.TowerRadius));
            world.Events.Add(43.68f, () => eventObj_1EB83E_4000A6E8?.Despawn());
        }
    }

    // A tower on the 22.5°-offset ring is that spot's layout EObj, bound to the director. Its Arg2
    // numbers it from the rolled north (`arg2Base` + 0..7). Off the ring it stays unbound.
    private EventObjectSpawnConfig BindTowerLayout(EventObjectSpawnConfig config, uint arg2Base)
    {
        const float step = MathF.PI / 4f;
        var position = config.Placement.Position;
        var angle = MathF.Atan2(position.X, -position.Z) - step / 2f;
        var spot = MathF.Round(angle / step);
        if (MathF.Abs(angle - spot * step) > 0.01f) return config;
        var ring = ((int)spot % 8 + 8) % 8;
        var fromNorth = ((ring - (int)MathF.Round(state.AdjustedNorthA.RadiansFromNorth / step)) % 8 + 8) % 8;
        return config with
        {
            LayoutId = EObjId.TowerRingLayoutIds[ring],
            EventId = EObjId.TowerEventId,
            TargetableStatus = 5,
            Arg2 = (arg2Base + (uint)fromNorth) << 16 | 3,
        };
    }

    private void Run_Rear_Power_Unit_4000A641()
    {
        SimEnemy? rear_Power_Unit_4000A641 = null;
        world.Events.Add(1f, () => rear_Power_Unit_4000A641 = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.RearPowerUnit, NameId: BNpcNameId.RearPowerUnit, Level: 90, Targetable: false, EnemyList: EnemyListMode.OnlyWhenVisible, Visibility: SpawnVisibility.HiddenUntilShown, LayoutId: BNpcLayoutId.RearPowerUnit, Placement: state.NewNorthB.Apply(new Placement(new Vector3(0.000f, -0.000f, 0.000f), 0f)))));
        world.Events.Add(45.97f, () => rear_Power_Unit_4000A641?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(47.88f, () => rear_Power_Unit_4000A641?.AttachLockonVfx(state.SpinnerRotation.LockonId));
        world.Events.Add(54.97f, () => rear_Power_Unit_4000A641?.Cast(Actions.RearLasersCharging, state.NewNorthB.Apply(new Vector3(0f, 0f, -25f))));
        for (int i = 0; i < 13; i++)
        {
            var placement = state.NewNorthB.Apply(new Placement(new Vector3(0.000f, 0.000f, 0.000f), state.SpinnerRotation.Mul * (i + 1) * MathF.PI / 20));
            world.Events.Add(58.54f + i * 0.58f, () => rear_Power_Unit_4000A641?.SetPosition(placement));
            world.Events.Add(58.59f + i * 0.58f, () => rear_Power_Unit_4000A641?.Cast(Actions.RearLasersShoot, placement.MoveForward(-25f).Position));
        }
        world.Events.Add(66.13f, () => rear_Power_Unit_4000A641?.PlayActionTimeline(ActionTimelineId.WarpStart));
    }

    private void Run_Omega_F_4000A40B_2()
    {
        if (state.OmegaFAttack == OmegaAttack.Legs)
        {
            SimEnemy? omega_F_4000A40B_2 = null;
            world.Events.Add(59.62f, () => omega_F_4000A40B_2 = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaHelper, NameId: BNpcNameId.OmegaFDynamis, Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper, Placement: state.NewNorthB.Apply(Geometry.SuperliminalSteelOmenPlacement))));
            world.Events.Add(59.66f, () => omega_F_4000A40B_2?.Cast(Actions.SuperliminalSteelR, state.NewNorthB.Apply(Geometry.SuperliminalSteelOmenTargetR)));
        }
    }

    private void Run_Omega_F_4000A40C_2()
    {
        if (state.OmegaFAttack == OmegaAttack.Legs)
        {
            SimEnemy? omega_F_4000A40C_2 = null;
            world.Events.Add(59.62f, () => omega_F_4000A40C_2 = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.OmegaHelper, NameId: BNpcNameId.OmegaFDynamis, Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper, Placement: state.NewNorthB.Apply(Geometry.SuperliminalSteelOmenPlacement))));
            world.Events.Add(59.66f, () => omega_F_4000A40C_2?.Cast(Actions.SuperliminalSteelL, state.NewNorthB.Apply(Geometry.SuperliminalSteelOmenTargetL)));
        }
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s ? new TopP5SigmaAiReplayStateMessage(
            s.Order.List, s.DynamisTargets.List, s.HelloWorldTargets.List, s.HandBait.List, s.HelloWorldJumpOrder.List,
            s.NewNorthA.RadiansFromNorth, s.NewNorthB.RadiansFromNorth, s.TowerNorthFlipped,
            s.GlitchType == GlitchType.Far, s.SpinnerRotation == Rotation.Clockwise, s.OmegaFAttack == OmegaAttack.Staff,
            s.FirstMissing, s.SecondMissing)
        : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not TopP5SigmaAiReplayStateMessage msg) return null;
        var shadowState = TopP5SigmaState.FromNetworkReplay(
            replayWorld.Party, msg.Order, msg.DynamisTargets, msg.HelloWorldTargets, msg.HandBait, msg.HelloWorldJumpOrder,
            msg.NewNorthARadians, msg.NewNorthBRadians, msg.TowerNorthFlipped,
            msg.GlitchIsFar, msg.SpinnerIsClockwise, msg.OmegaFIsStaff, msg.FirstMissing, msg.SecondMissing);
        ((IScenarioAi<TopP5SigmaState>)AiStrats[aiIndex]).Run(shadowState, replayWorld);
        return shadowState;
    }
}
