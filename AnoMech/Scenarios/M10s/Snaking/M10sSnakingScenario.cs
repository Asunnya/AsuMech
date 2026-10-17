using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.M10s.M10sConstants;
using Actions = AnoMech.Scenarios.M10s.M10sActions;

namespace AnoMech.Scenarios.M10s.Snaking;

// From Fire/Watersnaking to the second Hot Impact: the snakes split the party into fire and water,
// Deep Blue fires its proteans at the water players from a west corner while Red Hot's spreads
// land on the fire players, Sick Swell's wave rises, and the fire tank takes Hot Impact.
public sealed class M10sSnakingScenario : IScenario
{
    public string Name => "Fire/Watersnaking to Hot Impact";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sSnakingAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sSnakingSettingsWindow settingsWindow = new();

    public const float SnakingCastAt = 8.25f;
    public const float SnakingHitAt = 13.25f;
    public const float DeepBlueDashAt = 18.4f;
    public const float AlleyOopCastAt = 20.78f;
    public const float InfernoCastAt = 20.87f;
    public const float InfernoHitAt = 25.86f;
    public const float FirstConesAt = 26.39f;
    public const float SecondConesAt = 28.98f;
    public const float SickSwellCastAt = 28f;
    public const float WaveRisesAt = 30.99f;
    public const float HotImpactCastAt = 32.32f;
    public const float HotImpactHitAt = 37.32f;

    public static readonly Vector3 RedHotSpot = new(10f, 0f, 0f);

    private const float DashSpeed = 10f;
    private const float ReverseTurn = MathF.PI / 8f;
    private const float InfernoFireDelay = 0.45f;
    private const uint FireArg2Base = 0x50;

    private SimWorld world = null!;
    private M10sSnakingState state = null!;
    private SimEnemy? redHot;
    private SimEnemy? deepBlue;
    private SimEnemy? wave;
    private SimEnemy? waveWall;
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] coneHelpers = new SimEnemy?[8];
    private SimTether? waveTether;
    private readonly List<SimEventObject> fireVisuals = [];
    private uint nextFireArg;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sSnakingState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        state.FirstCones.Clear();
        fireVisuals.Clear();
        nextFireArg = 0;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sSnakingState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(6.07f, () =>
        {
            redHot?.Cast(ActionId.EpicBrotherhood, deepBlue, animationLock: 0.2f);
            deepBlue?.Cast(ActionId.EpicBrotherhoodBlue, redHot, animationLock: 0.2f);
        });
        world.Events.Add(SnakingCastAt, () =>
        {
            redHot?.SetRotation(-MathF.PI / 2f);
            deepBlue?.SetRotation(MathF.PI / 2f);
            redHot?.Cast(Actions.Firesnaking);
            deepBlue?.Cast(Actions.Watersnaking);
        });
        world.Events.Add(SnakingHitAt - 0.05f, PickTheSnakes);
        world.Events.Add(DeepBlueDashAt, () =>
        {
            deepBlue?.AddStatus(StatusId.DirectionalDisregard);
            deepBlue?.MoveTo(state.DeepBlueCorner, DashSpeed, 0f);
        });
        world.Events.Add(AlleyOopCastAt, () => deepBlue?.Cast(state.DoubleDip ? ActionId.DoubleDip : ActionId.ReverseAlleyOop, animationLock: 0.2f));
        world.Events.Add(InfernoCastAt, AlleyOopInferno);
        world.Events.Add(InfernoHitAt, DropInfernoFire);
        world.Events.Add(FirstConesAt - 0.04f, AimFirstCones);
        world.Events.Add(FirstConesAt, () => CastCones(state.DoubleDip ? Actions.DoubleDipFirst : Actions.ReverseFirst, state.FirstCones.Count));
        world.Events.Add(27f, () => redHot?.SetTarget(FireTank()));
        world.Events.Add(27.95f, MarkTheWaveWall);
        world.Events.Add(SickSwellCastAt, () => deepBlue?.Cast(ActionId.SickSwell, animationLock: 0.2f));
        world.Events.Add(SecondConesAt - 0.04f, AimSecondCones);
        world.Events.Add(SecondConesAt, () => CastCones(state.DoubleDip ? Actions.DoubleDipSecond : Actions.ReverseSecond, SecondCones().Count));
        world.Events.Add(WaveRisesAt, RaiseTheWave);
        world.Events.Add(HotImpactCastAt, HotImpact);
        world.Events.Add(33.04f, () =>
        {
            deepBlue?.RemoveStatus(StatusId.DirectionalDisregard);
            deepBlue?.MoveTo(Vector3.Zero, 6f);
        });
        world.Events.Add(41f, () => { });
    }

    private void SpawnActors()
    {
        redHot = SpawnBrother(BNpcBaseId.RedHot, BNpcNameId.RedHot, new Vector3(5f, 0f, 0f));
        deepBlue = SpawnBrother(BNpcBaseId.DeepBlue, BNpcNameId.DeepBlue, new Vector3(-5f, 0f, 0f));
        wave = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.SickSwell,
            NameId: BNpcNameId.SickSwell,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(Vector3.Zero, state.PushHeading)));
        wave?.SetVisible(false);
        waveWall = SpawnHelper(BNpcNameId.DeepBlue);
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper(BNpcNameId.RedHot);
        for (var i = 0; i < coneHelpers.Length; i++) coneHelpers[i] = SpawnHelper(BNpcNameId.DeepBlue);
    }

    private SimEnemy? SpawnBrother(uint baseId, uint nameId, Vector3 at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: baseId,
        NameId: nameId,
        Level: Level,
        Targetable: true,
        EnemyList: EnemyListMode.Always,
        Placement: new Placement(at, 0f).Face(Vector3.Zero)));

    private SimEnemy? SpawnHelper(uint nameId) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: nameId,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private List<SimCharacter> Alive() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList();

    private void PickTheSnakes()
    {
        if (redHot is not { } boss) return;
        var byDistance = Alive().OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).ToList();
        if (!state.FireOnTheNearest) byDistance.Reverse();
        for (var i = 0; i < byDistance.Count; i++)
            byDistance[i].AddStatus(i < 4 ? StatusId.Firesnaking : StatusId.Watersnaking);
    }

    private List<SimCharacter> FirePlayers() => Alive().Where(m => m.HasStatus(StatusId.Firesnaking)).ToList();

    private void AlleyOopInferno()
    {
        redHot?.SetPosition(new Placement(RedHotSpot, -MathF.PI / 2f));
        redHot?.Cast(ActionId.AlleyOopInferno, animationLock: 0.2f);
        var fire = FirePlayers();
        for (var i = 0; i < fire.Count && i < spreadHelpers.Length; i++)
        {
            spreadHelpers[i]?.SetPosition(new Placement(RedHotSpot, 0f));
            spreadHelpers[i]?.Cast(Actions.AlleyOopInfernoSpread, fire[i]);
            fire[i].AttachLockonVfx(LockonId.Spread);
        }
    }

    // Each spread leaves fire where it lands; Steam Burst sets it off later in the fight.
    private void DropInfernoFire()
    {
        foreach (var member in FirePlayers())
        {
            var at = member.Position;
            world.Events.Add(InfernoFireDelay, () => ShowFire(EObjId.InfernoFire, new Placement(at, 0f)));
        }
    }

    // UNVERIFIED: the cones take the four players nearest Deep Blue; only the water players stood there.
    private void AimFirstCones()
    {
        state.FirstCones.Clear();
        if (deepBlue is not { } boss) return;
        var nearest = Alive().OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).Take(4).ToList();
        for (var i = 0; i < nearest.Count; i++)
        {
            var aim = boss.Placement().Face(nearest[i].Position).Rotation;
            state.FirstCones.Add(aim);
            coneHelpers[i]?.SetPosition(new Placement(boss.Position, aim));
        }
    }

    // Double-dip fires the same cones again; Reverse fires a cone half a gap to each side of every first one.
    private List<float> SecondCones() => state.DoubleDip
        ? [.. state.FirstCones]
        : [.. state.FirstCones.SelectMany(aim => new[] { aim + ReverseTurn, aim - ReverseTurn })];

    private void AimSecondCones()
    {
        if (deepBlue is not { } boss) return;
        var aims = SecondCones();
        for (var i = 0; i < aims.Count && i < coneHelpers.Length; i++)
            coneHelpers[i]?.SetPosition(new Placement(boss.Position, aims[i]));
    }

    private void CastCones(EnemyAction cone, int count)
    {
        for (var i = 0; i < count && i < coneHelpers.Length; i++) coneHelpers[i]?.Cast(cone);
    }

    private void MarkTheWaveWall()
    {
        waveWall?.SetPosition(new Placement(state.WaveWall, state.PushHeading));
        waveTether = world.Tether(deepBlue, waveWall, TetherId.WaveWall);
    }

    private void RaiseTheWave()
    {
        waveTether?.Despawn();
        wave?.SetPosition(new Placement(Vector3.Zero, state.PushHeading));
        wave?.SetVisible(true);
        wave?.PlayActionTimeline(TimelineId.WaveRises);
    }

    private SimCharacter? FireTank()
    {
        var fire = FirePlayers();
        var tanks = new[] { PartyRole.MainTank, PartyRole.OffTank }.Select(r => world.Party.Get(r)).Where(t => t != null).ToList();
        return fire.FirstOrDefault(tanks.Contains) ?? fire.FirstOrDefault() ?? world.Party.Get(PartyRole.MainTank);
    }

    private void HotImpact()
    {
        if (FireTank() is not { } target || !target.IsAlive()) return;
        target.AttachLockonVfx(LockonId.SharedBuster);
        redHot?.Cast(Actions.HotImpactOnFire, target);
    }

    private void ShowFire(uint eobjId, Placement placement)
    {
        var fire = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = eobjId,
            Placement = placement,
            EventId = DirectorEventId,
            TargetableStatus = 5,
            Arg2 = (FireArg2Base + nextFireArg++) << 16 | 3,
        });
        if (fire == null) return;
        fireVisuals.Add(fire);
        world.Events.Add(0.15f, () => fire.SetState(1));
    }
}
