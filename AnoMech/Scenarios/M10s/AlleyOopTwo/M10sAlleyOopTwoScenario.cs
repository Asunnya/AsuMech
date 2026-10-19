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

namespace AnoMech.Scenarios.M10s.AlleyOopTwo;

// After the second Insane Air: Deep Blue fires its proteans from the middle while Red Hot's spreads
// land on all eight, Xtreme Aether bursts at every fire puddle, then Pyrotation's three stacks walk
// round Red Hot as Deep Impact busts the furthest player, and the brothers' Divers' Dare until the
// kill. The enrage was never reached in the logs and is not played.
public sealed class M10sAlleyOopTwoScenario : IScenario
{
    public string Name => "Double Alley-oop to Divers' Dare";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sAlleyOopTwoAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sAlleyOopTwoSettingsWindow settingsWindow = new();

    public const float AlleyOopCastAt = 8.09f;
    public const float InfernoCastAt = 10.74f;
    public const float FirstConesAt = 13.45f;
    public const float InfernoHitAt = 15.93f;
    public const float SecondConesAt = 16.03f;
    public const float AethersRiseAt = 16.82f;
    public const float SteamCastAt = 17.94f;
    public const float SteamHitAt = 21f;
    public const float DeepImpactCastAt = 19.94f;
    public const float DeepImpactHitAt = 25.06f;
    public static readonly float[] PyrotationHitAt = [24.84f, 26.85f, 28.86f];
    public static readonly float[] DiversDareAt = [33.22f, 42.41f];

    public static readonly Vector3 RedHotSpot = new(5f, 0f, -6f);
    public static readonly Vector3 DeepBlueImpactSpot = new(4f, 0f, -10.5f);
    public const float PuddleRadius = 4f;

    private const float ReverseTurn = MathF.PI / 8f;
    private const float InfernoFireDelay = 0.45f;
    private const float FireArmDelay = 1.5f;
    private const float BurnToDeath = 1f;
    private const uint FireArg2Base = 0x50;

    private SimWorld world = null!;
    private M10sAlleyOopTwoState state = null!;
    private BurningGround ground = null!;
    private SimEnemy? redHot;
    private SimEnemy? deepBlue;
    private SimEnemy? stackHelper;
    private readonly SimEnemy?[] redHelpers = new SimEnemy?[8];
    private readonly SimEnemy?[] coneHelpers = new SimEnemy?[16];
    private readonly SimEnemy?[] aethers = new SimEnemy?[8];
    private readonly List<SimEventObject> fireVisuals = [];
    private uint nextFireArg;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sAlleyOopTwoState(world.Rng, settingsWindow.Overrides);
        ground = new BurningGround(StatusId.Burns, BurnToDeath, "stood in its lingering fire");
        fireVisuals.Clear();
        nextFireArg = 0;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sAlleyOopTwoState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(3.77f, () =>
        {
            redHot?.Cast(ActionId.EpicBrotherhood, deepBlue, animationLock: 0.2f);
            deepBlue?.Cast(ActionId.EpicBrotherhoodBlue, redHot, animationLock: 0.2f);
        });
        world.Events.Add(5.91f, () => deepBlue?.Cast(ActionId.DeepBlueLeap, animationLock: 0.8f));
        world.Events.Add(6.3f, () => deepBlue?.SetPosition(new Placement(Vector3.Zero, 0f)));
        world.Events.Add(AlleyOopCastAt, () => deepBlue?.Cast(state.DoubleDip ? ActionId.DoubleDip : ActionId.ReverseAlleyOop, animationLock: 0.2f));
        world.Events.Add(InfernoCastAt, AlleyOopInferno);
        world.Events.Add(FirstConesAt - 0.04f, AimFirstCones);
        world.Events.Add(FirstConesAt, () => CastCones(state.DoubleDip ? Actions.DoubleDipFirst : Actions.ReverseFirst, state.FirstCones.Count));
        world.Events.Add(InfernoHitAt, DropInfernoFire);
        world.Events.Add(SecondConesAt - 0.04f, AimSecondCones);
        world.Events.Add(SecondConesAt, () => CastCones(state.DoubleDip ? Actions.DoubleDipSecond : Actions.ReverseSecond, SecondCones().Count));
        world.Events.Add(AethersRiseAt, RaiseAethers);
        world.Events.Add(SteamCastAt, () => { foreach (var aether in aethers.Where(a => a is { Visible: true })) aether!.Cast(Actions.SteamBurst); });
        world.Events.Add(19.5f, () => deepBlue?.MoveTo(DeepBlueImpactSpot, 8f));
        world.Events.Add(19.59f, () => world.Party.Get(state.PyrotationTarget)?.AttachLockonVfx(LockonId.Pyrotation));
        world.Events.Add(19.67f, () => redHot?.Cast(ActionId.Pyrotation, animationLock: 0.2f));
        world.Events.Add(DeepImpactCastAt, () => deepBlue?.Cast(ActionId.DeepImpact, animationLock: 0.2f));
        world.Events.Add(SteamHitAt + 0.5f, () => { foreach (var aether in aethers) aether?.SetVisible(false); });
        foreach (var at in PyrotationHitAt)
            world.Events.Add(at, PyrotationHit);
        world.Events.Add(DeepImpactHitAt, DeepImpact);
        foreach (var at in DiversDareAt)
            world.Events.Add(at, () =>
            {
                redHot?.Cast(Actions.DiversDare);
                deepBlue?.Cast(Actions.DiversDareBlue);
            });
        world.Events.Add(50f, DespawnAll);
    }

    public void Tick(float delta, float elapsed) => ground.Tick(world.Party, delta, elapsed);

    private void SpawnActors()
    {
        redHot = SpawnBrother(BNpcBaseId.RedHot, BNpcNameId.RedHot, new Placement(RedHotSpot, 0f));
        deepBlue = SpawnBrother(BNpcBaseId.DeepBlue, BNpcNameId.DeepBlue, new Placement(new Vector3(-5f, 0f, 0f), MathF.PI / 2f));
        stackHelper = SpawnHelper(BNpcNameId.RedHot);
        for (var i = 0; i < redHelpers.Length; i++) redHelpers[i] = SpawnHelper(BNpcNameId.RedHot);
        for (var i = 0; i < coneHelpers.Length; i++) coneHelpers[i] = SpawnHelper(BNpcNameId.DeepBlue);
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

    private SimEnemy? SpawnBrother(uint baseId, uint nameId, Placement at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: baseId,
        NameId: nameId,
        Level: Level,
        Targetable: true,
        EnemyList: EnemyListMode.Always,
        Placement: at));

    private SimEnemy? SpawnHelper(uint nameId) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: nameId,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private List<SimCharacter> Alive() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList();

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
        state.InfernoPuddles.Clear();
        foreach (var member in Alive())
        {
            var at = member.Position;
            state.InfernoPuddles.Add(at);
            ground.AddCircle(ActionId.AlleyOopInferno, InfernoHitAt + InfernoFireDelay + FireArmDelay, at, PuddleRadius);
            world.Events.Add(InfernoFireDelay, () => ShowFire(EObjId.InfernoFire, new Placement(at, 0f)));
        }
    }

    private void RaiseAethers()
    {
        for (var i = 0; i < state.InfernoPuddles.Count && i < aethers.Length; i++)
        {
            aethers[i]?.SetPosition(new Placement(state.InfernoPuddles[i], 0f));
            aethers[i]?.SetVisible(true);
            aethers[i]?.PlayActionTimeline(TimelineId.AetherRises);
        }
    }

    // One cone per player, aimed from Deep Blue at where each stands.
    private void AimFirstCones()
    {
        state.FirstCones.Clear();
        if (deepBlue is not { } boss) return;
        var members = Alive();
        for (var i = 0; i < members.Count && i < coneHelpers.Length; i++)
        {
            var aim = boss.Placement().Face(members[i].Position).Rotation;
            state.FirstCones.Add(aim);
            coneHelpers[i]?.SetPosition(new Placement(boss.Position, aim));
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
        for (var i = 0; i < aims.Count && i < coneHelpers.Length; i++)
            coneHelpers[i]?.SetPosition(new Placement(boss.Position, aims[i]));
    }

    private void CastCones(EnemyAction cone, int count)
    {
        for (var i = 0; i < count && i < coneHelpers.Length; i++)
            coneHelpers[i]?.Cast(cone);
    }

    private void PyrotationHit()
    {
        if (world.Party.Get(state.PyrotationTarget) is not { } target || !target.IsAlive()) return;
        var at = target.Position;
        stackHelper?.SetPosition(new Placement(at, 0f));
        stackHelper?.Cast(Actions.PyrotationStack, target);
        ground.AddCircle(ActionId.Pyrotation, world.Events.Elapsed + FireArmDelay, at, PuddleRadius);
        ShowFire(EObjId.PyrotationFire, new Placement(at, 0f));
    }

    private void DeepImpact()
    {
        if (deepBlue is not { } boss) return;
        var furthest = Alive().OrderByDescending(m => Vector3.DistanceSquared(m.Position, boss.Position)).FirstOrDefault();
        if (furthest != null) boss.Cast(Actions.DeepImpactHit, furthest);
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

    private void DespawnAll()
    {
        ground.PutOut(world.Party);
        foreach (var fire in fireVisuals) fire.Despawn();
        fireVisuals.Clear();
    }
}
