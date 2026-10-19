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

namespace AnoMech.Scenarios.M10s.ArenaSplit;

// From Deep Aerial's Divers' Dare to the next one: Red Hot charges down the middle and leaves the
// arena split by fire, then spreads, Deep Blue's proteans from one half, Freaky Pyrotation's pair
// stacks and Sickest Take-off with the wave and two light party stacks. Wherever water crosses the
// fire, and at every fire puddle, Xtreme Aether bursts.
public sealed class M10sArenaSplitScenario : IScenario
{
    public string Name => "Arena Split";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sArenaSplitAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sArenaSplitSettingsWindow settingsWindow = new();

    public const float LeapAt = 3.96f;
    public const float FloaterCastAt = 6.26f;
    public const float SplitLineAt = 11.46f;
    public const float SplitFireAt = 12.71f;
    public const float InfernoCastAt = 15.54f;
    public const float InfernoHitAt = 20.51f;
    public const float DeepBlueLeapAt = 18.16f;
    public const float AlleyOopCastAt = 20.42f;
    public const float FirstConesAt = 26.05f;
    public const float FirstSteamAt = 27.65f;
    public const float PyrotationMarkAt = 28.40f;
    public const float SecondConesAt = 28.63f;
    public const float SecondSteamAt = 30.23f;
    public const float PyrotationHitAt = 33.64f;
    public const float TakeoffAt = 34.80f;
    public const float WaveHitsAt = 41.77f;
    public const float ThirdSteamAt = 43.06f;
    public const float DiversDareAt = 43.99f;
    public const float SlabAt = 44.30f;
    public const float FireFadesAt = 49.7f;
    public const float SteamResolves = 2.75f;

    public static readonly Vector3 FloaterStart = new(0f, 0f, -Geometry.ArenaHalfWidth);
    public static readonly Vector3 DeepBlueLands = new(-4.33f, 0f, -3.38f);
    public const float SplitHalfWidth = 4f;
    // UNVERIFIED: the puddles' size, as in the opener.
    public const float PuddleRadius = 4f;
    public const float ConeLength = 60f;
    public const float ReverseTurn = MathF.PI / 8f;

    // UNVERIFIED: how far the wave carries the party.
    public const float WaveCarries = 10f;
    private const float WaveCarryTime = 0.8f;
    // UNVERIFIED: how long fire takes to start burning after it shows, and how long standing in it kills.
    private const float FireArmDelay = 1.5f;
    private const float BurnToDeath = 1f;
    private const ushort TwoOrbs = 0x3ED;
    private const byte SurfingModelState = 0x2F;
    private const float FloaterDashSpeed = 40f;
    private const float DeepBlueLeapSpeed = 10f;
    private const float InfernoFireDelay = 0.81f;
    private const float PyrotationFireDelay = 1.04f;
    private const uint FireArg2Base = 0x50;

    private SimWorld world = null!;
    private M10sArenaSplitState state = null!;
    private BurningGround ground = null!;
    private SimEnemy? redHot;
    private SimEnemy? deepBlue;
    private SimEnemy? wave;
    private SimEnemy? waveWall;
    private SimEnemy? laneHelper;
    private SimEnemy? waveHelper;
    private SimEnemy? floaterStart;
    private SimEnemy? floaterEnd;
    private SimTether? waveTether;
    private SimTether? floaterTether;
    private readonly SimEnemy?[] redHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] coneHelpers = new SimEnemy?[16];
    private readonly SimEnemy?[] orbHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] aethers = new SimEnemy?[24];
    private readonly List<Vector3> pyrotationPuddles = [];
    private readonly List<Vector3> firstCrossings = [];
    private readonly List<Vector3> secondCrossings = [];
    private readonly List<SimEventObject> fireVisuals = [];
    private uint nextFireArg;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sArenaSplitState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        ground = new BurningGround(StatusId.Burns, BurnToDeath, "stood in the fire");
        pyrotationPuddles.Clear();
        firstCrossings.Clear();
        secondCrossings.Clear();
        fireVisuals.Clear();
        nextFireArg = 0;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sArenaSplitState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(3.87f, () => redHot?.AddStatus(StatusId.DirectionalDisregard));
        world.Events.Add(LeapAt, () =>
        {
            redHot?.Cast(ActionId.Leap, animationLock: 0.8f);
            MarkTheWaveWall();
        });
        world.Events.Add(4f, () => deepBlue?.Cast(ActionId.SickSwell, animationLock: 0.2f));
        world.Events.Add(4.76f, () => redHot?.SetPosition(new Placement(FloaterStart, 0f)));
        world.Events.Add(6.22f, () => floaterTether = world.Tether(floaterStart, floaterEnd, TetherId.FloaterTaut));
        world.Events.Add(FloaterCastAt, () =>
        {
            redHot?.SetModelState(SurfingModelState);
            redHot?.Cast(ActionId.FlameFloaterSplit, animationLock: 0.2f);
        });
        world.Events.Add(7.07f, RaiseTheWave);
        world.Events.Add(SplitLineAt, SplitTheArena);
        world.Events.Add(SplitFireAt, () => ShowFire(EObjId.FloaterFire, new Placement(FloaterStart, 0f)));
        world.Events.Add(12.84f, () => redHot?.MoveTo(Vector3.Zero, FloaterDashSpeed));
        world.Events.Add(14.43f, () => redHot?.Cast(ActionId.Landing, animationLock: 0.5f));
        world.Events.Add(14.92f, () =>
        {
            redHot?.RemoveStatus(StatusId.DirectionalDisregard);
            redHot?.SetModelState(0);
        });
        world.Events.Add(InfernoCastAt, AlleyOopInferno);
        world.Events.Add(DeepBlueLeapAt, () =>
        {
            deepBlue?.Cast(ActionId.DeepBlueLeap, animationLock: 0.2f);
            deepBlue?.MoveTo(state.DeepBlueSpot, DeepBlueLeapSpeed, 0f);
        });
        world.Events.Add(AlleyOopCastAt, () =>
        {
            deepBlue?.Face(Vector3.Zero);
            deepBlue?.Cast(state.DoubleDip ? ActionId.DoubleDip : ActionId.ReverseAlleyOop, animationLock: 0.2f);
        });
        world.Events.Add(InfernoHitAt, DropInfernoFire);
        world.Events.Add(FirstConesAt - 0.04f, AimFirstCones);
        world.Events.Add(FirstConesAt, () => CastCones(state.DoubleDip ? Actions.DoubleDipFirst : Actions.ReverseFirst, state.FirstCones.Count));
        world.Events.Add(26.5f, () => RaiseAethers(0, [.. state.InfernoPuddles, .. firstCrossings]));
        world.Events.Add(FirstSteamAt, () => BurstAethers(0, 16));
        world.Events.Add(PyrotationMarkAt, MarkPyrotation);
        world.Events.Add(28.49f, () => redHot?.Cast(ActionId.FreakyPyrotation, animationLock: 0.2f));
        world.Events.Add(28.54f, () => deepBlue?.AddStatus(StatusId.Orbs, stacks: TwoOrbs));
        world.Events.Add(SecondConesAt - 0.04f, AimSecondCones);
        world.Events.Add(SecondConesAt, () =>
        {
            CastCones(state.DoubleDip ? Actions.DoubleDipSecond : Actions.ReverseSecond, state.SecondCones.Count);
            deepBlue?.SetModelState(SurfingModelState);
            deepBlue?.Cast(ActionId.SickestTakeoff, animationLock: 0.2f);
        });
        world.Events.Add(29.07f, () => RaiseAethers(16, secondCrossings));
        world.Events.Add(SecondSteamAt, () => BurstAethers(16, 8));
        world.Events.Add(FirstSteamAt + SteamResolves + 0.5f, () => HideAethers(0, 16));
        world.Events.Add(PyrotationHitAt, FreakyPyrotation);
        world.Events.Add(SecondSteamAt + SteamResolves + 0.5f, () => HideAethers(16, 8));
        world.Events.Add(TakeoffAt - 0.35f, () =>
        {
            waveTether?.Despawn();
            deepBlue?.PlayActionTimeline(TimelineId.Leave);
            deepBlue?.SetVisible(false);
        });
        world.Events.Add(TakeoffAt - 0.04f, SurfIntoTheLane);
        world.Events.Add(TakeoffAt, () =>
        {
            laneHelper?.Cast(Actions.SickestTakeoffLane);
            waveHelper?.Cast(Actions.SickSwellWave);
        });
        world.Events.Add(TakeoffAt + 1.17f, () => deepBlue?.SetVisible(true));
        world.Events.Add(WaveHitsAt + 0.05f, CarryWithTheWave);
        world.Events.Add(41.9f, () => RaiseAethers(0, [.. state.InfernoPuddles, .. pyrotationPuddles, Vector3.Zero, LaneCrossing()]));
        world.Events.Add(42.09f, () => deepBlue?.SetVisible(false));
        world.Events.Add(42.5f, () => wave?.SetVisible(false));
        world.Events.Add(42.7f, () =>
        {
            deepBlue?.SetModelState(0);
            deepBlue?.SetPosition(new Placement(DeepBlueLands, 0f).Face(Vector3.Zero));
            deepBlue?.SetVisible(true);
            deepBlue?.PlayActionTimeline(TimelineId.Arrive);
        });
        world.Events.Add(ThirdSteamAt, () => BurstAethers(0, 16));
        world.Events.Add(DiversDareAt, () =>
        {
            redHot?.Cast(Actions.DiversDare);
            deepBlue?.Cast(Actions.DiversDareBlue);
        });
        world.Events.Add(SlabAt, Slabs);
        world.Events.Add(45f, () => deepBlue?.RemoveStatus(StatusId.Orbs));
        world.Events.Add(ThirdSteamAt + SteamResolves + 0.5f, () => HideAethers(0, 16));
        world.Events.Add(FireFadesAt, FadeFire);
        world.Events.Add(53f, DespawnAll);
    }

    public void Tick(float delta, float elapsed) => ground.Tick(world.Party, delta, elapsed);

    private void SpawnActors()
    {
        redHot = SpawnBrother(BNpcBaseId.RedHot, BNpcNameId.RedHot, new Vector3(-2.9f, 0f, 7.7f));
        deepBlue = SpawnBrother(BNpcBaseId.DeepBlue, BNpcNameId.DeepBlue, new Vector3(-2.4f, 0f, 5.7f));
        wave = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.SickSwell,
            NameId: BNpcNameId.SickSwell,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(Vector3.Zero, state.PushHeading)));
        wave?.SetVisible(false);
        waveWall = SpawnHelper(BNpcNameId.DeepBlue);
        laneHelper = SpawnHelper(BNpcNameId.DeepBlue);
        waveHelper = SpawnHelper(BNpcNameId.DeepBlue);
        floaterStart = SpawnHelper(BNpcNameId.RedHot);
        floaterEnd = SpawnHelper(BNpcNameId.RedHot);
        floaterStart?.SetPosition(new Placement(FloaterStart, 0f));
        for (var i = 0; i < redHelpers.Length; i++) redHelpers[i] = SpawnHelper(BNpcNameId.RedHot);
        for (var i = 0; i < coneHelpers.Length; i++) coneHelpers[i] = SpawnHelper(BNpcNameId.DeepBlue);
        for (var i = 0; i < orbHelpers.Length; i++) orbHelpers[i] = SpawnHelper(BNpcNameId.DeepBlue);
        for (var i = 0; i < aethers.Length; i++)
        {
            aethers[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.XtremeAether,
                NameId: BNpcNameId.XtremeAether,
                Level: Level,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(Vector3.Zero, 0f)));
            aethers[i]?.SetVisible(false);
        }
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

    private void SplitTheArena()
    {
        floaterTether?.Despawn();
        floaterStart?.Cast(Actions.FlameFloaterSplitLine);
        ground.AddStrip(ActionId.FlameFloaterSplit, SplitFireAt + FireArmDelay, FloaterStart, new Vector2(0f, 1f), 2f * Geometry.ArenaHalfWidth, SplitHalfWidth);
    }

    private void AlleyOopInferno()
    {
        redHot?.Cast(ActionId.AlleyOopInferno, animationLock: 0.2f);
        var members = Alive();
        for (var i = 0; i < members.Count && i < redHelpers.Length; i++)
        {
            if (redHot is { } boss) redHelpers[i]?.SetPosition(boss.Placement());
            redHelpers[i]?.Cast(Actions.AlleyOopInfernoSpread, members[i]);
            members[i].AttachLockonVfx(LockonId.Spread);
        }
    }

    private void DropInfernoFire()
    {
        foreach (var member in Alive())
        {
            var at = member.Position;
            state.InfernoPuddles.Add(at);
            ground.AddCircle(ActionId.AlleyOopInferno, InfernoHitAt + InfernoFireDelay + FireArmDelay, at, PuddleRadius);
            world.Events.Add(InfernoFireDelay, () => ShowFire(EObjId.InfernoFire, new Placement(at, 0f)));
        }
    }

    // One cone per player, aimed from Deep Blue at where each stands.
    private void AimFirstCones()
    {
        state.FirstCones.Clear();
        firstCrossings.Clear();
        if (deepBlue is not { } boss) return;
        foreach (var member in Alive().Take(8))
        {
            var aim = boss.Placement().Face(member.Position).Rotation;
            coneHelpers[state.FirstCones.Count]?.SetPosition(new Placement(boss.Position, aim));
            state.FirstCones.Add(aim);
            if (StripCrossing(boss.Position, aim) is { } crossing) firstCrossings.Add(crossing);
        }
    }

    // Double-dip fires the same cones again; Reverse fires two, half a gap either side of each.
    private void AimSecondCones()
    {
        state.SecondCones.Clear();
        secondCrossings.Clear();
        if (deepBlue is not { } boss) return;
        var aims = state.DoubleDip ? state.FirstCones : state.FirstCones.SelectMany(a => new[] { a - ReverseTurn, a + ReverseTurn });
        foreach (var aim in aims.Take(coneHelpers.Length))
        {
            coneHelpers[state.SecondCones.Count]?.SetPosition(new Placement(boss.Position, aim));
            state.SecondCones.Add(aim);
            if (StripCrossing(boss.Position, aim) is { } crossing && secondCrossings.Count < 8) secondCrossings.Add(crossing);
        }
    }

    // Where a cone's centre line crosses the fire down the middle.
    public static Vector3? StripCrossing(Vector3 origin, float heading)
    {
        var dx = MathF.Sin(heading);
        if (MathF.Abs(dx) < 1e-3f) return null;
        var along = -origin.X / dx;
        if (along <= 0f || along > ConeLength) return null;
        var z = origin.Z + along * MathF.Cos(heading);
        return MathF.Abs(z) <= Geometry.ArenaHalfWidth ? new Vector3(0f, 0f, z) : null;
    }

    private void CastCones(EnemyAction cone, int count)
    {
        for (var i = 0; i < count && i < coneHelpers.Length; i++)
            coneHelpers[i]?.Cast(cone);
    }

    private void RaiseAethers(int first, IReadOnlyList<Vector3> spots)
    {
        for (var i = 0; i < spots.Count && first + i < aethers.Length; i++)
        {
            var aether = aethers[first + i];
            aether?.SetPosition(new Placement(spots[i], 0f));
            aether?.SetVisible(true);
            aether?.PlayActionTimeline(TimelineId.AetherRises);
        }
    }

    private void BurstAethers(int first, int count)
    {
        for (var i = first; i < first + count && i < aethers.Length; i++)
            if (aethers[i] is { Visible: true } aether) aether.Cast(Actions.SteamBurst);
    }

    private void HideAethers(int first, int count)
    {
        for (var i = first; i < first + count && i < aethers.Length; i++)
            aethers[i]?.SetVisible(false);
    }

    private void MarkPyrotation()
    {
        foreach (var role in state.PyrotationTargets)
            if (world.Party.Get(role) is { } member && member.IsAlive()) member.AttachLockonVfx(LockonId.FreakyPyrotation);
    }

    private void FreakyPyrotation()
    {
        var i = 0;
        foreach (var role in state.PyrotationTargets)
        {
            if (world.Party.Get(role) is not { } target || !target.IsAlive()) continue;
            var at = target.Position;
            pyrotationPuddles.Add(at);
            redHelpers[i]?.SetPosition(new Placement(at, 0f));
            redHelpers[i++]?.Cast(Actions.FreakyPyrotationStack, target);
            ground.AddCircle(ActionId.FreakyPyrotation, PyrotationHitAt + PyrotationFireDelay + FireArmDelay, at, PuddleRadius);
            world.Events.Add(PyrotationFireDelay, () => ShowFire(EObjId.PyrotationFire, new Placement(at, 0f)));
        }
    }

    private void SurfIntoTheLane()
    {
        deepBlue?.SetPosition(new Placement(state.LaneCentre, state.PushHeading));
        laneHelper?.SetPosition(new Placement(state.LaneStart, state.PushHeading));
        waveHelper?.SetPosition(new Placement(state.FromWaveFrame(-25f, 0f), state.PushHeading));
    }

    private Vector3 LaneCrossing() => state.LaneCentre with { X = 0f };

    private void CarryWithTheWave()
    {
        foreach (var member in Alive())
            member.Push(state.PushHeading, WaveCarries, WaveCarryTime);
    }

    // UNVERIFIED: the stacks land on the healers.
    private void Slabs()
    {
        var stack = 0;
        foreach (var healer in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer })
        {
            if (world.Party.Get(healer) is not { } target || !target.IsAlive()) continue;
            orbHelpers[stack]?.SetPosition(new Placement(target.Position, 0f));
            orbHelpers[stack++]?.Cast(Actions.AwesomeSlab, target);
        }
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

    private void FadeFire()
    {
        ground.PutOut(world.Party);
        foreach (var fire in fireVisuals) fire.SetState(0);
    }

    private void DespawnAll()
    {
        foreach (var fire in fireVisuals) fire.Despawn();
        fireVisuals.Clear();
        waveTether?.Despawn();
        floaterTether?.Despawn();
    }
}
