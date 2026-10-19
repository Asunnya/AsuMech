using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.M10sConstants;
using Actions = AnoMech.Scenarios.M10s.M10sActions;

namespace AnoMech.Scenarios.M10s.SickestTakeoff;

// Deep Blue's first turn, from its arrival to its Divers' Dare: Sick Swell's wave carries the party
// across the arena while Deep Blue surfs down one lane (Sickest Take-off), then two light party
// stacks or eight spreads by the orbs it showed, the double proteans and Deep Impact.
public sealed class M10sSickestTakeoffScenario : IScenario
{
    public string Name => "Sickest Take-off to Deep Impact";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sSickestTakeoffAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sSickestTakeoffSettingsWindow settingsWindow = new();

    public const float ArriveAt = 5.4f;
    public const float OrbsShownAt = 17.92f;
    public const float TakeoffAt = 24.07f;
    public const float WaveHitsAt = 31.07f;
    public const float OrbsResolveAt = 33.66f;
    public const float AlleyOopCastAt = 40.29f;
    public const float FirstConesAt = 45.90f;
    public const float SecondConesAt = 48.48f;
    public const float DeepImpactCastAt = 51.42f;
    public const float DeepImpactHitAt = 56.59f;
    public const float DiversDareAt = 58.68f;

    // UNVERIFIED: measured off a single clear, where the party moved ~10y with the wave.
    public const float WaveCarries = 10f;
    private const float WaveCarryTime = 0.8f;
    private const float ReverseTurn = MathF.PI / 8f;
    private const ushort TwoOrbs = 0x3ED;
    private const ushort EightOrbs = 0x3EE;

    private SimWorld world = null!;
    private M10sSickestTakeoffState state = null!;
    private SimEnemy? deepBlue;
    private SimEnemy? wave;
    private SimEnemy? waveWall;
    private SimEnemy? laneHelper;
    private SimEnemy? waveHelper;
    private readonly SimEnemy?[] helpers = new SimEnemy?[16];
    private SimTether? waveTether;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sSickestTakeoffState(world.Rng, settingsWindow.Overrides);
        state.FirstCones.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sSickestTakeoffState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(ArriveAt, () => { deepBlue?.SetVisible(true); deepBlue?.PlayActionTimeline(TimelineId.Arrive); });
        foreach (var at in new[] { 5.49f, 9.55f, 15.6f })
            world.Events.Add(at, AutoAttack);
        world.Events.Add(8.25f, () => deepBlue?.MoveTo(Vector3.Zero, 6f, 0f));
        world.Events.Add(9.77f, MarkTheWaveWall);
        world.Events.Add(9.82f, () => deepBlue?.Cast(ActionId.SickSwell, animationLock: 0.2f));
        world.Events.Add(12.66f, RaiseTheWave);
        world.Events.Add(OrbsShownAt, () => deepBlue?.AddStatus(StatusId.Orbs, stacks: state.EightOrbs ? EightOrbs : TwoOrbs));
        world.Events.Add(18.01f, () => deepBlue?.Cast(ActionId.SickestTakeoff, animationLock: 0.2f));
        world.Events.Add(22f, () => waveTether?.Despawn());
        world.Events.Add(23.72f, () => { deepBlue?.PlayActionTimeline(TimelineId.Leave); deepBlue?.SetVisible(false); });
        world.Events.Add(TakeoffAt - 0.04f, SurfIntoTheLane);
        world.Events.Add(TakeoffAt, Takeoff);
        world.Events.Add(25.24f, () => deepBlue?.SetVisible(true));
        world.Events.Add(WaveHitsAt + 0.05f, CarryWithTheWave);
        world.Events.Add(31.39f, () => deepBlue?.SetVisible(false));
        world.Events.Add(32.01f, () => deepBlue?.SetPosition(new Placement(Vector3.Zero, MathF.PI)));
        world.Events.Add(32.71f, () => { deepBlue?.SetVisible(true); deepBlue?.PlayActionTimeline(TimelineId.Arrive); });
        world.Events.Add(OrbsResolveAt, ResolveOrbs);
        world.Events.Add(34.4f, () => { wave?.SetVisible(false); deepBlue?.RemoveStatus(StatusId.Orbs); });
        world.Events.Add(AlleyOopCastAt, () => deepBlue?.Cast(state.DoubleDip ? ActionId.DoubleDip : ActionId.ReverseAlleyOop, animationLock: 0.2f));
        world.Events.Add(FirstConesAt - 0.04f, AimFirstCones);
        world.Events.Add(FirstConesAt, () => CastCones(state.DoubleDip ? Actions.DoubleDipFirst : Actions.ReverseFirst, state.FirstCones.Count));
        world.Events.Add(SecondConesAt - 0.04f, AimSecondCones);
        world.Events.Add(SecondConesAt, () => CastCones(state.DoubleDip ? Actions.DoubleDipSecond : Actions.ReverseSecond, SecondCones().Count));
        world.Events.Add(DeepImpactCastAt, () => deepBlue?.Cast(ActionId.DeepImpact, animationLock: 0.2f));
        world.Events.Add(DeepImpactHitAt, DeepImpact);
        world.Events.Add(DiversDareAt, () => deepBlue?.Cast(Actions.DiversDareBlue));
        world.Events.Add(66f, DespawnAll);
    }

    private void SpawnActors()
    {
        deepBlue = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.DeepBlue,
            NameId: BNpcNameId.DeepBlue,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(new Vector3(0f, 0f, -7.5f), 0f)));
        deepBlue?.SetVisible(false);
        wave = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.SickSwell,
            NameId: BNpcNameId.SickSwell,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(Vector3.Zero, state.PushHeading)));
        wave?.SetVisible(false);
        waveWall = SpawnHelper();
        laneHelper = SpawnHelper();
        waveHelper = SpawnHelper();
        for (var i = 0; i < helpers.Length; i++) helpers[i] = SpawnHelper();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.DeepBlue,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private void AutoAttack()
    {
        if (world.Party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive() && deepBlue is { Visible: true } boss)
            boss.Cast(ActionId.DeepBlueAttack, tank, animationLock: 0.2f);
    }

    // Deep Blue tethers the wall the wave will rise at.
    private void MarkTheWaveWall()
    {
        waveWall?.SetPosition(new Placement(state.FromWaveFrame(-40f, 0f), state.PushHeading));
        waveTether = world.Tether(deepBlue, waveWall, TetherId.WaveWall);
    }

    private void RaiseTheWave()
    {
        wave?.SetPosition(new Placement(Vector3.Zero, state.PushHeading));
        wave?.SetVisible(true);
        wave?.PlayActionTimeline(TimelineId.WaveRises);
    }

    private void SurfIntoTheLane()
    {
        deepBlue?.SetPosition(new Placement(state.FromWaveFrame(0f, state.Lane * M10sSickestTakeoffState.LaneOffset), state.PushHeading));
        laneHelper?.SetPosition(new Placement(state.LaneStart, state.PushHeading));
        waveHelper?.SetPosition(new Placement(state.FromWaveFrame(-25f, 0f), state.PushHeading));
    }

    private void Takeoff()
    {
        laneHelper?.Cast(Actions.SickestTakeoffLane);
        waveHelper?.Cast(Actions.SickSwellWave);
    }

    private void CarryWithTheWave()
    {
        foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
            member.Push(state.PushHeading, WaveCarries, WaveCarryTime);
    }

    private void ResolveOrbs()
    {
        if (state.EightOrbs)
        {
            var i = 0;
            foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
            {
                if (i >= helpers.Length) break;
                helpers[i]?.SetPosition(new Placement(member.Position, 0f));
                helpers[i++]?.Cast(Actions.AwesomeSplash, member);
            }
            return;
        }
        var stack = 0;
        foreach (var healer in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer })
        {
            if (world.Party.Get(healer) is not { } target || !target.IsAlive()) continue;
            helpers[stack]?.SetPosition(new Placement(target.Position, 0f));
            helpers[stack++]?.Cast(Actions.AwesomeSlab, target);
        }
    }

    // One cone per player, aimed from Deep Blue at where each stands.
    private void AimFirstCones()
    {
        state.FirstCones.Clear();
        if (deepBlue is not { } boss) return;
        var i = 0;
        foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
        {
            if (i >= helpers.Length) break;
            var aim = boss.Placement().Face(member.Position).Rotation;
            state.FirstCones.Add(aim);
            helpers[i++]?.SetPosition(new Placement(boss.Position, aim));
        }
    }

    // Double-dip fires the same cones again; Reverse fires two, half a gap either side of each.
    private List<float> SecondCones() => state.DoubleDip
        ? [.. state.FirstCones]
        : [.. state.FirstCones.SelectMany(aim => new[] { aim + ReverseTurn, aim - ReverseTurn })];

    private void AimSecondCones()
    {
        if (deepBlue is not { } boss) return;
        var aims = SecondCones();
        for (var i = 0; i < aims.Count && i < helpers.Length; i++)
            helpers[i]?.SetPosition(new Placement(boss.Position, aims[i]));
    }

    private void CastCones(Core.EnemyActions.EnemyAction cone, int count)
    {
        for (var i = 0; i < count && i < helpers.Length; i++)
            helpers[i]?.Cast(cone);
    }

    private void DeepImpact()
    {
        if (deepBlue is not { } boss) return;
        var furthest = world.Party.ActiveMembers().Where(m => m.IsAlive())
            .OrderByDescending(m => Vector3.DistanceSquared(m.Position, boss.Position)).FirstOrDefault();
        if (furthest != null) boss.Cast(Actions.DeepImpactHit, furthest);
    }

    private void DespawnAll()
    {
        waveTether?.Despawn();
        wave?.SetVisible(false);
    }
}
