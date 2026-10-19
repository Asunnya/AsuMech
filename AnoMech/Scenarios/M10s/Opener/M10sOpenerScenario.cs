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

namespace AnoMech.Scenarios.M10s.Opener;

// Red Hot's opener, from the pull to the first Divers' Dare: Hot Impact, Flame Floater's four
// charges along the tethers, Alley-oop Inferno's spreads, Cutback Blaze and Pyrotation. Each
// charge, spread, cleave and stack leaves fire on the floor until Divers' Dare.
public sealed class M10sOpenerScenario : IScenario
{
    public string Name => "Flame Floater to Pyrotation";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sOpenerAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sOpenerSettingsWindow settingsWindow = new();

    public static readonly float[] FloaterStatusAt = [22.77f, 24.77f, 26.78f, 28.79f];
    public static readonly float[] FloaterTetherAt = [22.91f, 24.92f, 26.92f, 28.88f];
    public static readonly float[] FloaterHitAt = [30.17f, 33.38f, 36.72f, 39.88f];
    public const float LeapAt = 20.5f;
    public const float LandAt = 21.3f;
    public const float FloaterCastAt = 22.82f;
    public const float InfernoCastAt = 48.08f;
    public const float InfernoHitAt = 53.08f;
    public const float CutbackCastAt = 57.54f;
    public const float CutbackHitAt = 62.71f;
    public static readonly float[] PyrotationHitAt = [69.60f, 71.59f, 73.63f];
    public const float DiversDareAt = 77.41f;
    public const float FireFadesAt = 82.66f;

    public static readonly Vector2 InfernoBossSpot = new(-7.5f, 6f);
    public static readonly Vector2 CutbackBossSpot = new(-2f, 2f);

    private static readonly ushort[] LineStatuses = [StatusId.FirstInLine, StatusId.SecondInLine, StatusId.ThirdInLine, StatusId.FourthInLine];

    private const float FloaterDashDelay = 1.38f;
    private const float FloaterDashSpeed = 40f;
    private const float FireSpawnDelay = 0.54f;
    private const float InfernoFireDelay = 0.45f;
    // UNVERIFIED: how long fire takes to start burning after it shows, and how long standing in it kills.
    private const float FireArmDelay = 1.5f;
    private const float BurnToDeath = 1f;
    private const float StripLength = 60f;
    private const float StripHalfWidth = 4f;
    // UNVERIFIED: the spread and stack puddles' size; players stood 4.6y from the last one unhurt.
    public const float PuddleRadius = 4f;
    private const uint FireArg2Base = 0x50;

    private SimWorld world = null!;
    private M10sOpenerState state = null!;
    private SimEnemy? redHot;
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[8];
    private SimEnemy? stackHelper;
    private readonly SimEnemy?[] tetherAnchors = new SimEnemy?[M10sOpenerState.Floaters];
    private readonly SimTether?[] tethers = new SimTether?[M10sOpenerState.Floaters];
    private readonly SimCharacter?[] tetherSources = new SimCharacter?[M10sOpenerState.Floaters];
    private BurningGround ground = null!;
    private readonly List<SimEventObject> fireVisuals = [];
    private uint nextFireArg;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sOpenerState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        state.PyrotationSpots.Clear();
        state.InfernoPuddles.Clear();
        ground = new BurningGround(StatusId.Burns, BurnToDeath, "stood in its lingering fire");
        fireVisuals.Clear();
        Array.Clear(tethers);
        Array.Clear(tetherSources);
        nextFireArg = 0;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sOpenerState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(0.3f, () => redHot?.MoveTo(Vector3.Zero, 6f, 0f));
        foreach (var at in new[] { 3.26f, 6.29f, 17.39f })
            world.Events.Add(at, AutoAttack);
        world.Events.Add(9.36f, () => redHot?.Cast(Actions.HotImpact, world.Party.Get(PartyRole.MainTank)));
        world.Events.Add(20.41f, () => redHot?.AddStatus(StatusId.DirectionalDisregard));
        world.Events.Add(LeapAt, () => redHot?.Cast(ActionId.Leap, animationLock: 0.8f));
        world.Events.Add(LandAt, LandOnTheWall);
        for (var i = 0; i < M10sOpenerState.Floaters; i++)
        {
            var n = i;
            world.Events.Add(FloaterStatusAt[n], () => MarkFloater(n));
            world.Events.Add(FloaterTetherAt[n], () => TetherFloater(n));
            world.Events.Add(FloaterHitAt[n], () => Charge(n));
        }
        world.Events.Add(FloaterCastAt, () => redHot?.Cast(ActionId.FlameFloater, animationLock: 0.2f));
        world.Events.Add(42.96f, () => redHot?.Cast(ActionId.Landing, animationLock: 0.5f));
        world.Events.Add(43.99f, () => redHot?.RemoveStatus(StatusId.DirectionalDisregard));
        world.Events.Add(43.99f, () => redHot?.MoveTo(state.FromWestFrame(InfernoBossSpot)));
        world.Events.Add(InfernoCastAt, AlleyOopInferno);
        world.Events.Add(InfernoHitAt, DropInfernoFire);
        world.Events.Add(54.3f, () => redHot?.MoveTo(state.FromWestFrame(CutbackBossSpot)));
        world.Events.Add(CutbackCastAt, () => redHot?.Cast(ActionId.CutbackBlaze, animationLock: 0.2f));
        world.Events.Add(CutbackCastAt, AimCutback);
        world.Events.Add(CutbackHitAt, () => redHot?.Cast(Actions.CutbackBlazeCleave));
        world.Events.Add(CutbackHitAt + 0.6f, DropCutbackFire);
        world.Events.Add(63.96f, () => redHot?.Cast(ActionId.Pyrotation, animationLock: 0.2f));
        foreach (var at in PyrotationHitAt)
            world.Events.Add(at, PyrotationHit);
        world.Events.Add(DiversDareAt, () => redHot?.Cast(Actions.DiversDare));
        world.Events.Add(FireFadesAt, FadeFire);
        world.Events.Add(86f, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        UpdateTethers();
        ground.Tick(world.Party, delta, elapsed);
    }

    private void SpawnActors()
    {
        redHot = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.RedHot,
            NameId: BNpcNameId.RedHot,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(new Vector3(0f, 0f, -10f), 0f)));
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper();
        stackHelper = SpawnHelper();
        for (var i = 0; i < tetherAnchors.Length; i++) tetherAnchors[i] = SpawnHelper();
    }

    private SimEnemy? SpawnHelper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: BNpcNameId.RedHot,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private void AutoAttack()
    {
        if (world.Party.Get(PartyRole.MainTank) is { } tank && tank.IsAlive())
            redHot?.Cast(ActionId.RedHotAttack, tank, animationLock: 0.2f);
    }

    private void LandOnTheWall()
    {
        var wall = state.WallDirection * Geometry.ArenaHalfWidth;
        redHot?.SetPosition(new Placement(new Vector3(wall.X, 0f, wall.Y), 0f).Face(Vector3.Zero));
    }

    private SimCharacter? Floater(int n) => world.Party.Get(state.FloaterOrder[n]);

    private void MarkFloater(int n) => Floater(n)?.AddStatus(LineStatuses[n]);

    private void TetherFloater(int n)
    {
        tetherSources[n] = n == 0 ? redHot : Floater(n - 1);
        tethers[n] = world.Tether(tetherSources[n], Floater(n), TetherId.FloaterSlack);
    }

    private void UpdateTethers()
    {
        for (var n = 0; n < tethers.Length; n++)
        {
            if (tethers[n] is not { IsActive: true } tether || tetherSources[n] is not { } source || Floater(n) is not { } target) continue;
            var taut = Vector3.Distance(source.Position, target.Position) >= Actions.FlameFloaterLethalWithin;
            var wanted = taut ? TetherId.FloaterTaut : TetherId.FloaterSlack;
            if (tether.TetherId == wanted) continue;
            tether.Despawn();
            tethers[n] = world.Tether(source, target, wanted);
        }
    }

    private void Charge(int n)
    {
        if (redHot is not { } boss || Floater(n) is not { } target) return;
        target.RemoveStatus(LineStatuses[n]);
        tethers[n]?.Despawn();
        tethers[n] = null;
        var start = boss.Position;
        var landing = target.Position;
        boss.Cast(Actions.FlameFloaterLines[n], target);
        var direction = new Vector2(landing.X - start.X, landing.Z - start.Z);
        direction = direction.LengthSquared() > 0.01f ? Vector2.Normalize(direction) : new Vector2(MathF.Sin(boss.Rotation), MathF.Cos(boss.Rotation));
        var hitAt = FloaterHitAt[n];
        ground.AddStrip(ActionId.FlameFloater, hitAt + FireSpawnDelay + FireArmDelay, start, direction, StripLength, StripHalfWidth);
        world.Events.Add(FireSpawnDelay, () => ShowFire(EObjId.FloaterFire, new Placement(start, MathF.Atan2(direction.X, direction.Y))));
        world.Events.Add(FloaterDashDelay, () => redHot?.MoveTo(landing, FloaterDashSpeed));
        if (n + 1 < tethers.Length && tethers[n + 1] is { } next)
        {
            next.Despawn();
            tetherAnchors[n + 1]?.SetPosition(new Placement(landing, 0f));
            tetherSources[n + 1] = tetherAnchors[n + 1];
            tethers[n + 1] = world.Tether(tetherSources[n + 1], Floater(n + 1), TetherId.FloaterSlack);
        }
    }

    private void AlleyOopInferno()
    {
        redHot?.Cast(ActionId.AlleyOopInferno, animationLock: 0.2f);
        var i = 0;
        foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
        {
            if (i >= spreadHelpers.Length) break;
            if (redHot is { } boss) spreadHelpers[i]?.SetPosition(boss.Placement());
            spreadHelpers[i++]?.Cast(Actions.AlleyOopInfernoSpread, member);
        }
    }

    private void DropInfernoFire()
    {
        foreach (var member in world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList())
        {
            var at = member.Position;
            state.InfernoPuddles.Add(at);
            ground.AddCircle(ActionId.AlleyOopInferno, InfernoHitAt + InfernoFireDelay + FireArmDelay, at, PuddleRadius);
            world.Events.Add(InfernoFireDelay, () => ShowFire(EObjId.InfernoFire, new Placement(at, 0f)));
        }
    }

    // As the bar starts Red Hot turns to the nearest player, the bait. The swing spares the slice
    // it faces and burns the rest; once it lands that slice catches fire and the burnt side is clear.
    private void AimCutback()
    {
        if (redHot is not { } boss) return;
        var nearest = world.Party.ActiveMembers().Where(m => m.IsAlive())
            .OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).FirstOrDefault();
        if (nearest != null) boss.Face(nearest);
        state.CutbackOrigin = boss.Position;
        state.SafeSliceRotation = boss.Rotation;
    }

    private void DropCutbackFire()
    {
        if (state.SafeSliceRotation is not { } bait) return;
        var direction = new Vector2(MathF.Sin(bait), MathF.Cos(bait));
        ground.AddSlice(ActionId.CutbackBlaze, CutbackHitAt + 0.6f + FireArmDelay, state.CutbackOrigin, direction, MathF.PI - Actions.CutbackBlazeHalfAngle);
        ShowFire(EObjId.CutbackFire, new Placement(state.CutbackOrigin, bait));
    }

    private void PyrotationHit()
    {
        if (world.Party.Get(state.PyrotationTarget) is not { } target || !target.IsAlive()) return;
        var at = target.Position;
        state.PyrotationSpots.Add(at);
        stackHelper?.SetPosition(new Placement(at, 0f));
        stackHelper?.Cast(Actions.PyrotationStack, target);
        ground.AddCircle(ActionId.Pyrotation, world.Events.Elapsed + FireArmDelay, at, PuddleRadius);
        ShowFire(EObjId.PyrotationFire, new Placement(at, 0f));
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
        foreach (var tether in tethers) tether?.Despawn();
    }
}
