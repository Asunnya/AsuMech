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

namespace AnoMech.Scenarios.M10s.DeepVarial;

// From Deep Varial to the Divers' Dare after Deep Impact, with fire and water already split: Deep
// Blue's wide fan from the wall opposite the big wave and its orbs, Red Hot's spreads and four Hot
// Aerial jumps on the furthest fire player, the fire puddles bursting, Sickest Take-off down a lane
// while the wave carries everyone, Cutback Blaze baited by the fire tank and Deep Impact.
public sealed class M10sDeepVarialScenario : IScenario
{
    public string Name => "Deep Varial to Deep Impact";
    public IPhase Phase => M10sZone.Fight;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M10sDeepVarialAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M10sDeepVarialSettingsWindow settingsWindow = new();

    public const float DeepVarialAt = 4.48f;
    public const float DeepVarialHitAt = 11.28f;
    public const float InfernoCastAt = 9.47f;
    public const float InfernoHitAt = 14.47f;
    public const float FirstSteamAt = 12.55f;
    public const float VarialOrbsAt = 14.33f;
    public const float HotAerialCastAt = 18.29f;
    public static readonly float[] JumpAt = [23.55f, 25.69f, 27.78f, 29.83f];
    public const float JumpToLanding = 1.16f;
    public const float TakeoffCastAt = 22.97f;
    public const float TakeoffAt = 29.03f;
    public const float WaveHitsAt = 36.03f;
    public const float CutbackCastAt = 37.01f;
    public const float SecondSteamAt = 37.32f;
    public const float TakeoffOrbsAt = 38.61f;
    public const float CutbackHitAt = 42.18f;
    public const float DeepImpactCastAt = 43.24f;
    public const float DeepImpactHitAt = 48.5f;
    public const float DiversDareAt = 50.64f;

    public static readonly Vector3 DeepBlueLands = new(-2f, 0f, 1f);
    public static readonly Vector3[] FirstInfernoPuddles = [new(12.6f, 0f, 4f), new(12.6f, 0f, -2.2f), new(18.7f, 0f, 2.5f), new(19.1f, 0f, -3.7f)];

    // UNVERIFIED: measured off a single clear, where the party moved ~10y with the wave.
    public const float WaveCarries = 10f;
    private const float WaveCarryTime = 0.8f;
    private const ushort VarialOrbs = 0x435;
    private const ushort TakeoffStackOrbs = 0x3EF;
    private const ushort TakeoffSpreadOrbs = 0x3F0;
    private const byte SurfingModelState = 0x2F;
    private const byte VarialModelState = 0x04;
    private const float LeapSpeed = 30f;
    private const float ChaseSpeed = 12f;
    private const float InfernoFireDelay = 0.18f;
    private const uint FireArg2Base = 0x50;

    private SimWorld world = null!;
    private M10sDeepVarialState state = null!;
    private SimEnemy? redHot;
    private SimEnemy? deepBlue;
    private SimEnemy? wave;
    private SimEnemy? varialHelper;
    private SimEnemy? laneHelper;
    private SimEnemy? waveHelper;
    private readonly SimEnemy?[] spreadHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] orbHelpers = new SimEnemy?[4];
    private readonly SimEnemy?[] landingHelpers = new SimEnemy?[M10sDeepVarialState.Jumps];
    private readonly SimEnemy?[] aethers = new SimEnemy?[12];
    private readonly List<SimEventObject> fireVisuals = [];
    private uint nextFireArg;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M10sDeepVarialState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        state.AerialLandings.Clear();
        state.InfernoPuddles.Clear();
        fireVisuals.Clear();
        nextFireArg = 0;
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M10sDeepVarialState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(ToxicWaymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(0.1f, SplitFireAndWater);
        world.Events.Add(DeepVarialAt - 0.05f, () =>
        {
            world.Map.AddEffect(state.VarialSpread ? MapEffect.FourOrbs : MapEffect.OneOrb, MapEffect.OrbsSlot);
            deepBlue?.AddStatus(StatusId.Orbs, stacks: VarialOrbs);
        });
        world.Events.Add(DeepVarialAt, DeepVarial);
        world.Events.Add(InfernoCastAt, AlleyOopInferno);
        world.Events.Add(9.74f, () => deepBlue?.RemoveStatus(StatusId.Orbs));
        world.Events.Add(10.77f, () => world.Map.AddEffect(MapEffect.OrbsOff, MapEffect.OrbsSlot));
        world.Events.Add(11.39f, () => RaiseAethers(FirstInfernoPuddles));
        world.Events.Add(11.84f, () => deepBlue?.SetModelState(0));
        world.Events.Add(FirstSteamAt, () => BurstAethers(FirstInfernoPuddles.Length));
        world.Events.Add(VarialOrbsAt, () => ResolveOrbs(state.VarialSpread, Actions.AwesomeSplashSmallWave, Actions.AwesomeSlabSmallWave));
        world.Events.Add(InfernoHitAt, DropInfernoFire);
        world.Events.Add(HotAerialCastAt, HotAerial);
        world.Events.Add(22.88f, () => deepBlue?.AddStatus(StatusId.Orbs, stacks: state.TakeoffSpread ? TakeoffSpreadOrbs : TakeoffStackOrbs));
        world.Events.Add(TakeoffCastAt, () =>
        {
            deepBlue?.SetModelState(SurfingModelState);
            deepBlue?.Cast(ActionId.SickestTakeoff, animationLock: 0.2f);
        });
        for (var i = 0; i < M10sDeepVarialState.Jumps; i++)
        {
            var n = i;
            world.Events.Add(JumpAt[n], () => Jump(n));
        }
        world.Events.Add(28.68f, () => { deepBlue?.PlayActionTimeline(TimelineId.Leave); deepBlue?.SetVisible(false); });
        world.Events.Add(TakeoffAt - 0.04f, SurfIntoTheLane);
        world.Events.Add(TakeoffAt, () =>
        {
            laneHelper?.Cast(Actions.SickestTakeoffLane);
            waveHelper?.Cast(Actions.SickSwellWave);
        });
        world.Events.Add(30.2f, () => deepBlue?.SetVisible(true));
        world.Events.Add(31.3f, () => redHot?.Follow(FireTank(), ChaseSpeed));
        world.Events.Add(32.37f, () => redHot?.SetModelState(0));
        world.Events.Add(36.16f, () => RaiseAethers([.. FirstInfernoPuddles, .. state.InfernoPuddles, .. state.AerialLandings]));
        world.Events.Add(WaveHitsAt + 0.05f, CarryWithTheWave);
        world.Events.Add(36.35f, () => deepBlue?.SetVisible(false));
        world.Events.Add(36.96f, () =>
        {
            deepBlue?.RemoveStatus(StatusId.Orbs);
            deepBlue?.SetModelState(0);
            deepBlue?.SetPosition(new Placement(DeepBlueLands, 0f).Face(Vector3.Zero));
            deepBlue?.SetVisible(true);
            deepBlue?.PlayActionTimeline(TimelineId.Arrive);
        });
        world.Events.Add(CutbackCastAt, AimCutback);
        world.Events.Add(SecondSteamAt, () => BurstAethers(aethers.Length));
        world.Events.Add(TakeoffOrbsAt, () => ResolveOrbs(state.TakeoffSpread, Actions.AwesomeSplash, Actions.AwesomeSlab));
        world.Events.Add(CutbackHitAt, () => redHot?.Cast(Actions.CutbackBlazeCleave));
        world.Events.Add(CutbackHitAt + 0.3f, DropCutbackFire);
        world.Events.Add(DeepImpactCastAt, () => deepBlue?.Cast(ActionId.DeepImpact, animationLock: 0.2f));
        world.Events.Add(DeepImpactHitAt, DeepImpact);
        world.Events.Add(DiversDareAt, () =>
        {
            redHot?.Cast(Actions.DiversDare);
            deepBlue?.Cast(Actions.DiversDareBlue);
        });
        world.Events.Add(56f, () => { });
    }

    private void SpawnActors()
    {
        redHot = SpawnBrother(BNpcBaseId.RedHot, BNpcNameId.RedHot, new Vector3(4f, 0f, 5f * state.Side));
        deepBlue = SpawnBrother(BNpcBaseId.DeepBlue, BNpcNameId.DeepBlue, Vector3.Zero);
        wave = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.SickSwell,
            NameId: BNpcNameId.SickSwell,
            Level: Level,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(Vector3.Zero, state.PushHeading)));
        varialHelper = SpawnHelper(BNpcNameId.DeepBlue);
        laneHelper = SpawnHelper(BNpcNameId.DeepBlue);
        waveHelper = SpawnHelper(BNpcNameId.DeepBlue);
        for (var i = 0; i < spreadHelpers.Length; i++) spreadHelpers[i] = SpawnHelper(BNpcNameId.RedHot);
        for (var i = 0; i < orbHelpers.Length; i++) orbHelpers[i] = SpawnHelper(BNpcNameId.DeepBlue);
        for (var i = 0; i < landingHelpers.Length; i++) landingHelpers[i] = SpawnHelper(BNpcNameId.RedHot);
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
        Placement: new Placement(at, 0f)));

    private SimEnemy? SpawnHelper(uint nameId) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.Helper,
        NameId: nameId,
        Level: 1,
        EnemyList: EnemyListMode.Never,
        Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    private List<SimCharacter> Alive() => world.Party.ActiveMembers().Where(m => m.IsAlive()).ToList();

    private List<SimCharacter> Element(bool fire) => Alive().Where(m => m.HasStatus(fire ? StatusId.Firesnaking : StatusId.Watersnaking)).ToList();

    private void SplitFireAndWater()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            world.Party.Get(role)?.AddStatus(state.IsFire(role) ? StatusId.Firesnaking : StatusId.Watersnaking);
        foreach (var puddle in FirstInfernoPuddles) ShowFire(EObjId.InfernoFire, new Placement(puddle, 0f));
        redHot?.Follow(FireTank());
    }

    private SimCharacter? FireTank()
    {
        var tank = new[] { PartyRole.MainTank, PartyRole.OffTank }.FirstOrDefault(state.IsFire);
        return world.Party.Get(tank) is { } t && t.IsAlive() ? t : Element(true).FirstOrDefault();
    }

    private void DeepVarial()
    {
        var placement = new Placement(state.DeepBlueWall, state.WaveFromNorth ? MathF.PI : 0f);
        deepBlue?.SetPosition(placement);
        deepBlue?.SetModelState(VarialModelState);
        deepBlue?.Cast(ActionId.DeepVarial, animationLock: 0.2f);
        varialHelper?.SetPosition(placement);
        varialHelper?.Cast(Actions.DeepVarialCone);
    }

    private void AlleyOopInferno()
    {
        redHot?.Cast(ActionId.AlleyOopInferno, animationLock: 0.2f);
        var fire = Element(true);
        for (var i = 0; i < fire.Count && i < spreadHelpers.Length; i++)
        {
            if (redHot is { } boss) spreadHelpers[i]?.SetPosition(boss.Placement());
            spreadHelpers[i]?.Cast(Actions.AlleyOopInfernoSpread, fire[i]);
            fire[i].AttachLockonVfx(LockonId.Spread);
        }
    }

    private void DropInfernoFire()
    {
        foreach (var member in Element(true))
        {
            var at = member.Position;
            state.InfernoPuddles.Add(at);
            world.Events.Add(InfernoFireDelay, () => ShowFire(EObjId.InfernoFire, new Placement(at, 0f)));
        }
    }

    // UNVERIFIED: the stack lands on the water healer.
    private void ResolveOrbs(bool spread, EnemyAction splash, EnemyAction slab)
    {
        var water = Element(false);
        if (spread)
        {
            for (var i = 0; i < water.Count && i < orbHelpers.Length; i++)
            {
                orbHelpers[i]?.SetPosition(new Placement(water[i].Position, 0f));
                orbHelpers[i]?.Cast(splash, water[i]);
            }
            return;
        }
        var healer = new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer }.Where(r => !state.IsFire(r)).Select(r => world.Party.Get(r)).FirstOrDefault(h => h != null && h.IsAlive())
                     ?? water.FirstOrDefault();
        if (healer == null) return;
        orbHelpers[0]?.SetPosition(new Placement(healer.Position, 0f));
        orbHelpers[0]?.Cast(slab, healer);
    }

    private void RaiseAethers(IReadOnlyList<Vector3> puddles)
    {
        for (var i = 0; i < puddles.Count && i < aethers.Length; i++)
        {
            aethers[i]?.SetPosition(new Placement(puddles[i], 0f));
            aethers[i]?.SetVisible(true);
            aethers[i]?.PlayActionTimeline(TimelineId.AetherRises);
        }
    }

    private void BurstAethers(int count)
    {
        for (var i = 0; i < count && i < aethers.Length; i++)
            if (aethers[i] is { Visible: true } aether) aether.Cast(Actions.SteamBurst);
    }

    private void HotAerial()
    {
        redHot?.StopMoving();
        redHot?.SetPosition(new Placement(Vector3.Zero, 0f));
        redHot?.SetModelState(SurfingModelState);
        redHot?.Cast(ActionId.HotAerial, animationLock: 0.2f);
    }

    // Each jump takes the fire player furthest from where Red Hot stands and lands where they stood.
    private void Jump(int n)
    {
        if (redHot is not { } boss) return;
        var target = Element(true).OrderByDescending(m => Vector3.DistanceSquared(m.Position, boss.Position)).FirstOrDefault();
        if (target == null) return;
        var landing = target.Position;
        state.AerialLandings.Add(landing);
        boss.Cast(ActionId.HotAerialJump, target, animationLock: 0.2f);
        boss.MoveTo(landing, LeapSpeed);
        world.Events.Add(JumpToLanding, () =>
        {
            landingHelpers[n]?.SetPosition(new Placement(landing, 0f));
            landingHelpers[n]?.Cast(Actions.HotAerialLandings[n]);
            ShowFire(EObjId.PyrotationFire, new Placement(landing, 0f));
        });
    }

    private void SurfIntoTheLane()
    {
        deepBlue?.SetPosition(new Placement(new Vector3(state.LaneX, 0f, 0f), state.PushHeading));
        laneHelper?.SetPosition(new Placement(state.LaneStart, state.PushHeading));
        waveHelper?.SetPosition(new Placement(new Vector3(0f, 0f, -25f * state.Side), state.PushHeading));
    }

    private void CarryWithTheWave()
    {
        foreach (var member in Alive())
            member.Push(state.PushHeading, WaveCarries, WaveCarryTime);
    }

    // As the bar starts Red Hot turns to the nearest player, the bait; the swing spares that slice.
    private void AimCutback()
    {
        if (redHot is not { } boss) return;
        boss.StopMoving();
        boss.Cast(ActionId.CutbackBlaze, animationLock: 0.2f);
        var nearest = Alive().OrderBy(m => Vector3.DistanceSquared(m.Position, boss.Position)).FirstOrDefault();
        if (nearest != null) boss.Face(nearest);
        state.CutbackOrigin = boss.Position;
        state.SafeSliceRotation = boss.Rotation;
    }

    private void DropCutbackFire()
    {
        if (state.SafeSliceRotation is { } bait) ShowFire(EObjId.CutbackFire, new Placement(state.CutbackOrigin, bait));
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
}
