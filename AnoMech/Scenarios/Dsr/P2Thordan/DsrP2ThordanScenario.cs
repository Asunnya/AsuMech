using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using AnoMech.Scenarios.Legacy;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P2Thordan;

// From Thordan's arrival through Strength and Sanctity of the Ward to Ultimate End and Broad Swing.
// Thordan's HP is not simulated: the run ends as the enrage (never reached in the logs) would fire.
public sealed class DsrP2ThordanScenario : IScenario
{
    public string Name => "Thordan";
    public IPhase Phase => DsrZone.Thordan;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP2ThordanAi()];

    private readonly DsrP2ThordanStateOverrides overrides = new();
    public object SettingsOverrides => overrides;

    private const uint ThordanMaxHp = 7439000;
    private const float ThordanHitboxRadius = 5f;
    private const float AutoAttackDamage = 0.3f;
    private const float AutoAttackHalfAngle = MathF.PI / 4f;
    private const float AutoAttackRange = 10f;
    private const float AscalonsMightDamage = 0.45f;
    private const float MercyCastSeconds = 2.7f;
    private const float MercyConeCastSeconds = 1.2f;
    private const float MercyConeHalfAngle = MathF.PI / 8f;
    private const float LightningStormRadius = 5f;
    private const float LightningStormDamage = 0.3f;
    private const float HeavyImpactRingWidth = 6f;
    private const float PuddleRadius = 9f;
    private const float SkywardLeapRadius = 24f;
    private const float SkywardLeapDamage = 0.5f;
    private const int DragonsRageMinTargets = 3;
    private const float DragonsRageDamage = 0.5f;
    private const float TowerSoakRadius = 3f;
    private const float TowerDamage = 0.3f;
    private const float HolyBladedanceDamage = 0.25f;
    // UNVERIFIED: Bladedance falls off with tether length; the tanks stretched it to ~28y.
    private const float MinShieldBashTetherLength = 20f;
    private const float AncientQuagaDamage = 0.8f;
    private const float HeavenlyHeelDamage = 0.6f;
    private const float KnightRingRadius = 8f;
    private const float GrinnauxRelativeRadius = 15f;
    private const float ShiningBladeHalfWidth = 3f;
    private const float ShiningBladeLandDelay = 0.35f;
    private const float BrightFlareRadius = 9f;
    private const float BrightFlareCastSeconds = 0.7f;
    private const float BrightFlareCastLead = 0.98f;
    private const int SacredSeverMinTargets = 3;
    private const float SacredSeverDamage = 0.4f;
    private const float SacredSeverVulnSeconds = 2f;
    private const float HiemalStormRadius = 7f;
    private const float HiemalStormDamage = 0.3f;
    private const float HeavensStakeCircleRadius = 7f;
    // UNVERIFIED: no player was ever inside it; only the arena's outer edge is assumed to burn.
    private const float HeavensStakeDonutInner = 20f;
    private const float BleedPuddleRadius = 6f;
    private const float BleedSeconds = 60f;
    private const float BleedDeathDelay = 3f;
    private const float BleedCheckStep = 0.5f;
    // UNVERIFIED: the ice puddles stay on the floor but stop applying Frostbite a few seconds in.
    private const float IcePuddleBleedEnds = 138.6f;
    private const float HolyCometRadius = 20f;
    private const float HolyCometDamage = 0.1f;
    private const float HolyCometMinSpacing = 5f;
    private const float HolyImpactDelay = 3.1f;
    private const float HolyCometExplodeDelay = 1.04f;
    private const float FaithUnmovingDamage = 0.1f;
    private const float BotAntiKnockbackRadius = 10f;
    private const float AntiKnockbackSeconds = 6f;
    private const float UltimateEndDamage = 0.7f;
    private const float UltimateEndKnightRadius = 19f;
    private const float BroadSwingHalfAngle = MathF.PI / 3f;
    private const float BroadSwingRange = 40f;
    private const float BroadSwingDamage = 0.75f;
    private const float DamageDownSeconds = 180f;
    private const float DimensionalCollapseDamage = 0.6f;
    private const float SlashingResistanceDownSeconds = 15f;
    // UNVERIFIED: how much mitigation Ascalon's Might needs on a tank carrying Slashing Resistance Down.
    private const float SlashingDownMitigation = 0.6f;
    private const float ShieldBashHalfWidth = 4f;
    private const float ShieldBashLandShort = 5f;
    private const float BladedanceHalfAngle = MathF.PI / 6f;
    private const float BladedanceRange = 16f;
    private static readonly Vector3 BroadSwingSpot = new(0f, 0f, -9f);
    // Despawning an effect's caster cuts its VFX short.
    private const float EffectCarrierLinger = 3f;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private DsrP2ThordanState state = null!;

    private SimEnemy? thordan;
    private readonly List<SimEnemy> mercyCasters = [];
    private readonly List<SimEnemy> helpers = [];
    private readonly List<(SimEnemy Caster, Vector3 At)> puddles = [];
    private readonly List<Vector3> towers = [];
    private SimEnemy? heavyImpactCaster;
    private SimEnemy? ignasse, paulecrain, vellguine, guerrique, hermenost, janlenoux, adelphel, grinnaux;
    private readonly SimTether?[] shieldBashTethers = new SimTether?[2];
    private readonly bool[] shieldBashLocked = new bool[2];
    private readonly bool[] shieldBashTooShort = new bool[2];
    private SimEnemy? zephirin, charibert, noudenet, haumeric;
    private SimEnemy? sanctityThordanHelper, eyeHelper;
    private readonly List<SimEnemy> ultimateEndKnights = [];
    private readonly List<SimEnemy> comets = [];
    private readonly List<(uint EObjId, SimEventObject Puddle)> puddleObjects = [];
    private readonly List<(uint EObjId, Vector3 At)> bleedPuddles = [];
    // Flying through a puddle on a knockback doesn't apply its bleed.
    private float knockbackLandsAt = -1f;
    private readonly List<(SimEnemy Caster, Vector3 At)> heavensStakes = [];
    private readonly List<(Vector3 At, float Radius)> sanctityTowers = [];
    private readonly List<SimEnemy> sanctityTowerCasters = [];
    private float playerAntiKnockbackUntil = -1f;

    private SimEnemy?[] DashKnights => [ignasse, paulecrain, vellguine];
    private SimEnemy?[] ShieldBashKnights => [adelphel, janlenoux];
    private SimEnemy?[] AllKnights => [ignasse, paulecrain, vellguine, guerrique, hermenost, janlenoux, adelphel, grinnaux, zephirin, charibert, noudenet, haumeric];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        damage.SetMitigableStatuses(DamageType.TankBuster, SlashingDownMitigation, StatusId.SlashingResistanceDown);
        state = new DsrP2ThordanState(world.Rng, overrides);
        mercyCasters.Clear();
        helpers.Clear();
        puddles.Clear();
        towers.Clear();
        Array.Clear(shieldBashTethers);
        Array.Clear(shieldBashLocked);
        Array.Clear(shieldBashTooShort);
        ultimateEndKnights.Clear();
        comets.Clear();
        puddleObjects.Clear();
        bleedPuddles.Clear();
        knockbackLandsAt = -1f;
        heavensStakes.Clear();
        sanctityTowers.Clear();
        sanctityTowerCasters.Clear();
        playerAntiKnockbackUntil = -1f;

        if (Plugin.PlayerInputHooks is { } hooks)
        {
            hooks.ActionExecuted -= OnPlayerAction;
            hooks.ActionExecuted += OnPlayerAction;
        }

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP2ThordanState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.ThordanArenaRadius, "Touched the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(NaurWaymarks));
        // The game's own checkpoint restart: without the map reset and restore, the zone keeps its default knights map.
        world.Events.Add(0f, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.NoMap));
        world.Events.Add(0f, () => world.Map.DirectorUpdate(ArenaDirector.CheckpointRestore, 1U));
        world.Events.Add(0f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 0U, ArenaDirector.ThordanLayout));
        world.Events.Add(0f, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.ThordanMap));
        world.Events.Add(0f, () => world.Map.DirectorUpdate(ArenaDirector.Music, ArenaDirector.ThordanMusic));
        world.Events.Add(0f, SpawnThordan);
        world.Events.Add(2f, () => world.Map.LogArena("DSR P2 start"));
        world.Events.Add(2.60f, ThordanEngages);

        world.Events.Add(6.08f, AutoAttack);
        world.Events.Add(9.20f, AutoAttack);

        world.Events.Add(11.25f, () => CastSelf(thordan, ActionId.AscalonsMercyConcealed, MercyCastSeconds));
        world.Events.Add(13.95f, SnapshotMercy);
        world.Events.Add(14.24f, () => PlayEffect(thordan, ActionId.AscalonsMercyConcealed, 2.1f));
        world.Events.Add(14.33f, CastMercyCones);
        world.Events.Add(15.80f, ResolveMercyCones);

        world.Events.Add(17.36f, AscalonsMight);
        world.Events.Add(19.04f, AscalonsMight);
        world.Events.Add(20.70f, AscalonsMight);

        world.Events.Add(23.29f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 0U, ArenaDirector.WardLayout));
        world.Events.Add(24.50f, ThordanLeapsToCentre);
        world.Events.Add(25.61f, () => PlayEffect(thordan, ActionId.KnightsOfTheRound, 2.1f));

        world.Events.Add(27.62f, SpawnKnightRing);
        world.Events.Add(28.73f, () => CastSelf(thordan, ActionId.StrengthOfTheWard, 3.7f));
        world.Events.Add(32.69f, () => PlayEffect(thordan, ActionId.StrengthOfTheWard, 2.1f));
        world.Events.Add(35.76f, () => thordan?.SetTargetable(false));
        world.Events.Add(35.82f, KnightsWarpOut);
        world.Events.Add(37.06f, PlaceDashKnightsAndGuerrique);
        world.Events.Add(39.02f, () => heavyImpactCaster = SpawnHelper(new Placement(state.GuerriqueSpot, 0f), BNpcNameId.Guerrique));
        world.Events.Add(39.38f, () => CastSelf(guerrique, ActionId.HeavyImpactWindup, 4.0f));
        world.Events.Add(39.38f, () => heavyImpactCaster?.NativeCast(ActionId.HeavyImpact, ActionType.Action, 0f, 5.7f, false, position: state.GuerriqueSpot));
        world.Events.Add(39.38f, () => CastSelf(thordan, ActionId.LightningStorm, 5.4f));
        world.Events.Add(39.38f, CastSpiralThrusts);
        world.Events.Add(43.66f, () => PlayEffect(guerrique, ActionId.HeavyImpactWindup, 2.1f));
        world.Events.Add(45.07f, () => PlayEffect(thordan, ActionId.LightningStorm, 2.1f));
        world.Events.Add(45.36f, ResolveSpiralThrusts);
        world.Events.Add(45.36f, () => ResolveHeavyImpact(0));
        world.Events.Add(45.55f, ResolveLightningStorm);
        world.Events.Add(46.78f, () => guerrique?.FadeOut());
        world.Events.Add(47.28f, () => ResolveHeavyImpact(1));
        world.Events.Add(47.44f, GuideKnightsAppear);
        world.Events.Add(47.45f, () => CastSelf(thordan, ActionId.AscalonsMercyConcealed, MercyCastSeconds));
        world.Events.Add(49.14f, () => ResolveHeavyImpact(2));
        world.Events.Add(50.15f, SnapshotMercy);
        world.Events.Add(50.44f, () => PlayEffect(thordan, ActionId.AscalonsMercyConcealed, 2.1f));
        world.Events.Add(50.53f, CastMercyCones);
        world.Events.Add(51.02f, () => ResolveHeavyImpact(3));
        world.Events.Add(52.00f, ResolveMercyCones);
        world.Events.Add(52.11f, SpawnPuddles);
        world.Events.Add(52.39f, MarkDefamations);
        world.Events.Add(52.48f, CastPuddles);
        world.Events.Add(52.58f, ThordanLeapsToWall);
        world.Events.Add(52.89f, () => ResolveHeavyImpact(4));
        world.Events.Add(53.89f, SpawnTowers);
        world.Events.Add(54.23f, CastTowers);
        world.Events.Add(55.79f, () => CastSelf(thordan, ActionId.DragonsRage, 4.4f));
        world.Events.Add(56.84f, TetherShieldBashes);
        world.Events.Add(60.46f, () => PlayEffect(thordan, ActionId.DragonsRage, 2.1f));
        world.Events.Add(60.46f, () => PlayEffect(grinnaux, ActionId.DimensionalCollapseWindup, 2.1f));
        world.Events.Add(61.40f, DashKnightsLeapOntoDefamations);
        world.Events.Add(61.47f, ResolvePuddles);
        world.Events.Add(61.51f, ResolveSkywardLeaps);
        world.Events.Add(61.84f, ResolveShieldBashes);
        world.Events.Add(61.89f, ResolveDragonsRage);
        world.Events.Add(62.42f, () => PlayEffect(hermenost, ActionId.ConvictionWindup, 2.1f));
        world.Events.Add(63.54f, () => FadeOut(ignasse, paulecrain, vellguine, grinnaux));
        world.Events.Add(63.94f, () => HolyBladedance(ActionId.HolyBladedanceWindup));
        foreach (var hit in new[] { 64.89f, 65.35f, 65.80f, 66.24f, 66.70f, 67.14f })
            world.Events.Add(hit, () => HolyBladedance(ActionId.HolyBladedance));
        world.Events.Add(64.92f, ThordanLeapsToCentre);
        world.Events.Add(65.21f, ResolveTowers);
        world.Events.Add(66.84f, ThordanEngages);
        world.Events.Add(66.97f, () => CastSelf(thordan, ActionId.AncientQuaga, 5.7f));
        world.Events.Add(67.51f, () => FadeOut(hermenost, adelphel, janlenoux));
        world.Events.Add(67.51f, () => Array.ForEach(shieldBashTethers, t => t?.Despawn()));
        world.Events.Add(72.94f, () => Raidwide(ActionId.AncientQuaga, AncientQuagaDamage));

        world.Events.Add(77.09f, AutoAttack);
        world.Events.Add(79.13f, CastHeavenlyHeel);
        world.Events.Add(83.11f, ResolveHeavenlyHeel);
        world.Events.Add(86.06f, AscalonsMight);
        world.Events.Add(87.72f, AscalonsMight);
        world.Events.Add(89.38f, AscalonsMight);

        world.Events.Add(92.01f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 0U, ArenaDirector.SanctityLayout));
        world.Events.Add(92.41f, ThordanLeapsToCentre);
        world.Events.Add(93.35f, () => PlayEffect(thordan, ActionId.KnightsOfTheRound, 2.1f));

        world.Events.Add(94.50f, DespawnStrengthKnights);
        world.Events.Add(95.31f, SpawnSanctityKnightRing);
        world.Events.Add(96.47f, () => CastSelf(thordan, ActionId.SanctityOfTheWard, 3.7f));
        world.Events.Add(100.44f, () => PlayEffect(thordan, ActionId.SanctityOfTheWard, 2.1f));
        world.Events.Add(103.55f, () => thordan?.SetTargetable(false));
        world.Events.Add(103.59f, SanctityKnightsWarpOut);
        world.Events.Add(104.39f, PlaceSanctityKnights);
        world.Events.Add(104.88f, () => WarpInPlace(adelphel, janlenoux, zephirin));
        world.Events.Add(105.95f, MarkPlungeTargets);
        world.Events.Add(107.06f, () => thordan?.PlayActionTimeline(TimelineId.WarpEnd));
        world.Events.Add(109.11f, () => CastSelf(thordan, ActionId.DragonsGaze, 3.7f));
        world.Events.Add(113.08f, () => PlayEffect(thordan, ActionId.DragonsGaze, 2.1f));
        world.Events.Add(114.06f, () => SacredSever(state.FirstPlungeTarget));
        world.Events.Add(114.23f, ResolveDragonsGaze);
        world.Events.Add(115.83f, () => SacredSever(state.SecondPlungeTarget));
        world.Events.Add(117.62f, () => SacredSever(state.FirstPlungeTarget));
        world.Events.Add(119.41f, () => SacredSever(state.SecondPlungeTarget));
        ScheduleSanctityCharges([114.14f, 115.70f, 117.26f]);
        world.Events.Add(118.83f, () => FadeOut(adelphel, janlenoux));
        world.Events.Add(120.17f, () => WarpInPlace(charibert, hermenost, haumeric, noudenet));
        world.Events.Add(120.21f, ThordanLeavesArena);
        world.Events.Add(120.97f, () => FadeOut(zephirin));

        world.Events.Add(124.09f, SpawnFirstSanctityTowers);
        world.Events.Add(124.41f, MarkMeteorTargets);
        world.Events.Add(124.45f, () => CastSelf(hermenost, ActionId.FirstConvictionWindup, 8.9f));
        world.Events.Add(124.45f, () => CastSanctityTowers(ActionId.FirstConviction, 11.7f));
        world.Events.Add(124.50f, () => CastSelf(noudenet, ActionId.HolyComet, 11.7f));
        world.Events.Add(124.54f, CastHeavensStake);
        world.Events.Add(124.59f, () => CastSelf(haumeric, ActionId.HiemalStorm, 6.7f));
        world.Events.Add(131.50f, () => PlayEffect(charibert, ActionId.HeavensStake, 2.1f));
        world.Events.Add(131.55f, () => PlayEffect(haumeric, ActionId.HiemalStorm, 2.1f));
        world.Events.Add(132.04f, ResolveHeavensStake);
        world.Events.Add(132.08f, SpawnFirePuddles);
        world.Events.Add(132.17f, ResolveHiemalStorm);
        world.Events.Add(133.60f, () => PlayEffect(hermenost, ActionId.FirstConvictionWindup, 2.1f));
        world.Events.Add(133.60f, () => FadeOut(charibert));
        world.Events.Add(133.60f, () => WarpInPlace(grinnaux));
        world.Events.Add(133.69f, () => FadeOut(haumeric));
        world.Events.Add(134.70f, () => SetPuddleState(EObjId.FirePuddle, 1));
        world.Events.Add(135.08f, () => SetPuddleState(EObjId.IcePuddle, 1));
        ScheduleBleedChecks(EObjId.FirePuddle, StatusId.Burns, ActionId.HeavensStakeCircle, 134.70f, 148.64f);
        ScheduleBleedChecks(EObjId.IcePuddle, StatusId.Frostbite, ActionId.HiemalStormHit, 135.08f, IcePuddleBleedEnds);
        world.Events.Add(136.41f, () => ResolveSanctityTowers(ActionId.FirstConviction));
        world.Events.Add(136.45f, () => PlayEffect(noudenet, ActionId.HolyComet, 2.1f));
        foreach (var drop in new[] { 136.80f, 138.23f, 139.66f, 141.08f, 142.50f, 143.93f, 145.36f })
            world.Events.Add(drop, DropHolyComets);
        world.Events.Add(138.55f, () => FadeOut(noudenet));
        world.Events.Add(138.77f, SpawnSecondSanctityTowers);
        world.Events.Add(138.82f, () => CastSelf(hermenost, ActionId.SecondConvictionWindup, 7.9f));
        world.Events.Add(138.82f, () => CastSanctityTowers(ActionId.SecondConviction, 10.7f));
        world.Events.Add(142.83f, () => CastSelf(grinnaux, ActionId.FaithUnmoving, 3.7f));
        world.Events.Add(146.80f, ResolveFaithUnmoving);
        world.Events.Add(146.96f, () => PlayEffect(hermenost, ActionId.SecondConvictionWindup, 2.1f));
        world.Events.Add(148.64f, () => SetPuddleState(EObjId.FirePuddle, 0));
        world.Events.Add(148.74f, () => SetPuddleState(EObjId.IcePuddle, 0));
        world.Events.Add(148.99f, () => FadeOut(grinnaux));
        world.Events.Add(149.34f, DespawnPuddles);
        world.Events.Add(149.74f, () => ResolveSanctityTowers(ActionId.SecondConviction));
        world.Events.Add(153.04f, () => FadeOut(hermenost));
        world.Events.Add(155.04f, DespawnComets);

        world.Events.Add(154.11f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 0U, ArenaDirector.UltimateEndLayout));
        world.Events.Add(154.20f, ThordanReturnsNorth);
        world.Events.Add(154.29f, () => PlayEffect(thordan, ActionId.UltimateEndArrival, 2.1f));
        world.Events.Add(159.24f, SpawnUltimateEndKnights);
        world.Events.Add(159.38f, () => PlayEffect(thordan, ActionId.UltimateEnd, 2.1f));
        world.Events.Add(167.81f, () => Raidwide(ActionId.UltimateEndHit, UltimateEndDamage));
        world.Events.Add(169.01f, DespawnUltimateEndKnights);
        world.Events.Add(173.44f, ThordanStepsIntoTheArena);
        world.Events.Add(177.07f, () => CastBroadSwing(0));
        world.Events.Add(180.90f, () => ResolveBroadSwing(0, 0));
        world.Events.Add(181.84f, () => ResolveBroadSwing(0, 1));
        world.Events.Add(182.86f, () => ResolveBroadSwing(0, 2));
        world.Events.Add(185.47f, () => CastBroadSwing(1));
        world.Events.Add(189.30f, () => ResolveBroadSwing(1, 0));
        world.Events.Add(190.24f, () => ResolveBroadSwing(1, 1));
        world.Events.Add(191.26f, () => ResolveBroadSwing(1, 2));
        world.Events.Add(193.90f, () => CastSelf(thordan, ActionId.AethericBurst, 6f));
        world.Events.Add(199.80f, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        for (var i = 0; i < shieldBashTethers.Length; i++)
            LockShieldBashOnTank(i);
    }

    private SimEnemy? SpawnHelper(Placement placement, uint nameId = BNpcNameId.Thordan)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Dummy,
            NameId: nameId,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Placement: placement));
        if (helper != null) helpers.Add(helper);
        return helper;
    }

    private SimEnemy? SpawnKnight(uint baseId, uint nameId, float bearing, float radius = KnightRingRadius) =>
        world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Placement: FacingCentre(AtBearing(bearing, radius)),
            NpcSpawnTemplate: DsrNpcSpawn.Build(baseId, nameId, 1)));

    private void OnPlayerAction(ActionType actionType, uint actionId, ulong targetId)
    {
        if (actionId is ActionId.ArmsLength or ActionId.Surecast)
            playerAntiKnockbackUntil = world.Events.Elapsed + AntiKnockbackSeconds;
    }

    private static Placement FacingCentre(Vector3 at) => new(at, RotationTowards(at, Vector3.Zero));

    private static void FadeOut(params SimEnemy?[] knights)
    {
        foreach (var knight in knights) knight?.FadeOut();
    }

    private static void CastSelf(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, targetId: caster.GameObjectId);

    // An effect without a position plays at the arena centre, so default it to the caster.
    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);

    private static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    private void SpawnThordan()
    {
        thordan = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Thordan,
            NameId: BNpcNameId.Thordan,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, 0f),
            HitboxRadius: ThordanHitboxRadius,
            NpcSpawnTemplate: DsrNpcSpawn.Build(BNpcBaseId.Thordan, BNpcNameId.Thordan, ThordanMaxHp)));
    }

    private void ThordanEngages()
    {
        thordan?.SetTargetable(true);
        thordan?.SetTarget(party.Get(PartyRole.MainTank));
    }

    // Thordan's autos cleave whoever stands in front of his target.
    private void AutoAttack()
    {
        if (thordan == null || party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        var rotation = RotationTowards(thordan.Position, tank.Position);
        PlayEffect(thordan, ActionId.ThordanAttack, 0.6f, rotation, tank.GameObjectId);
        foreach (var hit in party.Find.InsideCone(new Placement(thordan.Position, rotation), AutoAttackHalfAngle, AutoAttackRange).ToList())
            damage.ApplyDamage(hit, AutoAttackDamage, ActionId.ThordanAttack, "auto attack", lethal: false);
    }

    // Each cone is aimed at where its player stood when the cast bar ended.
    private void SnapshotMercy()
    {
        if (thordan == null) return;
        foreach (var member in AliveMembers().ToList())
        {
            var caster = SpawnHelper(new Placement(thordan.Position, RotationTowards(thordan.Position, member.Position)));
            if (caster != null) mercyCasters.Add(caster);
        }
    }

    private void CastMercyCones()
    {
        foreach (var caster in mercyCasters)
            caster.NativeCast(ActionId.AscalonsMercyConcealedCone, ActionType.Action, 0f, MercyConeCastSeconds, false, rotation: caster.Rotation, targetId: caster.GameObjectId);
    }

    private void ResolveMercyCones()
    {
        var casters = mercyCasters.ToList();
        mercyCasters.Clear();
        foreach (var caster in casters)
        {
            PlayEffect(caster, ActionId.AscalonsMercyConcealedCone, 1.1f, caster.Rotation);
            damage.Resolve(caster, ActionId.AscalonsMercyConcealedCone, [DamageType.Lethal], [], size: MercyConeHalfAngle);
        }
        world.Events.Add(EffectCarrierLinger, () => casters.ForEach(c => c.Despawn()));
    }

    private void AscalonsMight()
    {
        if (thordan == null || party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        var rotation = RotationTowards(thordan.Position, tank.Position);
        PlayEffect(thordan, ActionId.AscalonsMight, 1.1f, rotation, tank.GameObjectId);
        var hits = damage.Resolve(IPositioned.From(new Placement(thordan.Position, rotation)), ActionId.AscalonsMight, [DamageType.TankBuster], []);
        foreach (var hit in hits.Where(h => h.IsAlive()))
            damage.ApplyDamage(hit, AscalonsMightDamage, ActionId.AscalonsMight, "tank buster", lethal: false);
    }

    private void ThordanLeapsToCentre()
    {
        if (thordan == null) return;
        thordan.SetTarget(null);
        thordan.Follow(null);
        PlayEffect(thordan, ActionId.ThordanLeap, 1.1f);
        thordan.SetPosition(new Placement(Vector3.Zero, MathF.PI));
    }

    private void Raidwide(uint actionId, float fraction)
    {
        PlayEffect(thordan, actionId, 2.1f);
        foreach (var member in AliveMembers().ToList())
            damage.ApplyDamage(member, fraction, actionId, "raidwide", lethal: false);
    }

    private void SpawnKnightRing()
    {
        paulecrain = SpawnKnight(BNpcBaseId.Paulecrain, BNpcNameId.Paulecrain, 0f);
        ignasse = SpawnKnight(BNpcBaseId.Ignasse, BNpcNameId.Ignasse, 45f);
        janlenoux = SpawnKnight(BNpcBaseId.Janlenoux, BNpcNameId.Janlenoux, 90f);
        guerrique = SpawnKnight(BNpcBaseId.Guerrique, BNpcNameId.Guerrique, 135f);
        grinnaux = SpawnKnight(BNpcBaseId.Grinnaux, BNpcNameId.Grinnaux, 180f);
        hermenost = SpawnKnight(BNpcBaseId.Hermenost, BNpcNameId.Hermenost, 225f);
        adelphel = SpawnKnight(BNpcBaseId.Adelphel, BNpcNameId.Adelphel, 270f);
        vellguine = SpawnKnight(BNpcBaseId.Vellguine, BNpcNameId.Vellguine, 315f);
        foreach (var knight in AllKnights) knight?.PlayActionTimeline(TimelineId.KnightAppear);
    }

    private void KnightsWarpOut()
    {
        thordan?.PlayActionTimeline(TimelineId.WarpStart);
        foreach (var knight in AllKnights) knight?.PlayActionTimeline(TimelineId.WarpStart);
        world.Events.Add(0.8f, () =>
        {
            foreach (var knight in new[] { hermenost, janlenoux, adelphel, grinnaux }) knight?.SetVisible(false);
        });
    }

    private static void WarpIn(SimEnemy? knight, Vector3 at)
    {
        if (knight == null) return;
        knight.SetPosition(FacingCentre(at));
        knight.SetVisible(true);
        knight.PlayActionTimeline(TimelineId.WarpEnd);
    }

    private void PlaceDashKnightsAndGuerrique()
    {
        var dashers = DashKnights;
        for (var i = 0; i < dashers.Length; i++)
            WarpIn(dashers[i], AtBearing(state.DashBearings[i], DsrP2ThordanState.DashKnightRadius));
        WarpIn(guerrique, state.GuerriqueSpot);
        thordan?.PlayActionTimeline(TimelineId.WarpEnd);
    }

    private void CastSpiralThrusts()
    {
        foreach (var knight in DashKnights)
            knight?.NativeCast(ActionId.SpiralThrust, ActionType.Action, 0f, 5.7f, false, rotation: knight.Rotation, targetId: knight.GameObjectId);
    }

    // The knights charge straight through to the far wall.
    private void ResolveSpiralThrusts()
    {
        var dashers = DashKnights;
        for (var i = 0; i < dashers.Length; i++)
        {
            if (dashers[i] is not { } knight) continue;
            PlayEffect(knight, ActionId.SpiralThrust, 1.1f, knight.Rotation);
            damage.Resolve(knight, ActionId.SpiralThrust, [DamageType.Lethal], []);
            var farEnd = AtBearing(state.DashBearings[i] + 180f, DsrP2ThordanState.DashKnightRadius);
            world.Events.Add(0.5f, () => knight.SetPosition(FacingCentre(farEnd)));
        }
    }

    private static readonly uint[] HeavyImpactWaves =
        [ActionId.HeavyImpact, ActionId.HeavyImpactRing1, ActionId.HeavyImpactRing2, ActionId.HeavyImpactRing3, ActionId.HeavyImpactRing4];

    // Each wave is a ring 6y further out than the last.
    private void ResolveHeavyImpact(int wave)
    {
        var centre = state.GuerriqueSpot;
        PlayEffect(heavyImpactCaster, HeavyImpactWaves[wave], 1.1f, at: centre);
        var inner = wave * HeavyImpactRingWidth;
        foreach (var hit in party.Find.InsideRing(centre, inner, inner + HeavyImpactRingWidth).ToList())
            hit.Die(ActionId.HeavyImpact, "stood in the expanding ring");
    }

    // Everyone is hit; anyone also caught in another player's circle takes a second, lethal hit.
    private void ResolveLightningStorm()
    {
        var members = AliveMembers().ToList();
        foreach (var member in members)
            PlayEffect(thordan, ActionId.LightningStormHit, 1.1f, target: member.GameObjectId, at: member.Position);
        foreach (var member in members)
        {
            if (members.Any(other => other != member && FlatDistance(other.Position, member.Position) < LightningStormRadius))
            {
                member.Die(ActionId.LightningStormHit, "overlapped another player's circle");
                continue;
            }
            damage.ApplyDamage(member, LightningStormDamage, ActionId.LightningStormHit, "spread", lethal: false);
            member.AddStatus(StatusId.LightningResistanceDownII, 2.96f);
        }
    }

    private void GuideKnightsAppear()
    {
        WarpIn(adelphel, AtBearing(state.AdelphelBearing, DsrP2ThordanState.GuideKnightRadius));
        WarpIn(janlenoux, AtBearing(state.JanlenouxBearing, DsrP2ThordanState.GuideKnightRadius));
        WarpIn(grinnaux, state.RelativeToThordan(180f, GrinnauxRelativeRadius));
        WarpIn(hermenost, state.RelativeToThordan(180f, DsrP2ThordanState.DashKnightRadius));
    }

    private void MarkDefamations()
    {
        foreach (var role in state.Defamations)
            party.Get(role)?.AttachLockonVfx(LockonId.SkywardLeap, 9.1f, persistent: false);
    }

    private void ThordanLeapsToWall()
    {
        if (thordan == null) return;
        PlayEffect(thordan, ActionId.ThordanLeap, 1.1f);
        thordan.SetPosition(FacingCentre(state.ThordanLeapSpot));
    }

    private void SpawnPuddles()
    {
        foreach (var (bearing, radius) in DsrP2ThordanState.PuddleSpots)
        {
            var at = state.RelativeToThordan(bearing, radius);
            if (SpawnHelper(new Placement(at, 0f), BNpcNameId.Grinnaux) is { } caster) puddles.Add((caster, at));
        }
    }

    // UNVERIFIED: the omen is drawn from the action's 3y range, but the portals hit out to 9y.
    private void CastPuddles()
    {
        CastSelf(grinnaux, ActionId.DimensionalCollapseWindup, 7.7f);
        foreach (var (caster, at) in puddles)
            caster.NativeCast(ActionId.DimensionalCollapse, ActionType.Action, 0f, 8.7f, false, position: at);
    }

    private void ResolvePuddles()
    {
        foreach (var (caster, at) in puddles)
        {
            PlayEffect(caster, ActionId.DimensionalCollapse, 1.1f, at: at);
            foreach (var hit in party.Find.InsideCircle(at, PuddleRadius).Where(m => m.IsAlive()).ToList())
            {
                damage.ApplyDamage(hit, DimensionalCollapseDamage, ActionId.DimensionalCollapse, "stood in a portal", lethal: false);
                hit.AddStatus(StatusId.DamageDown, DamageDownSeconds);
            }
        }
        puddles.Clear();
    }

    private void SpawnTowers()
    {
        foreach (var bearing in DsrP2ThordanState.TowerBearings)
            towers.Add(state.RelativeToThordan(bearing, DsrP2ThordanState.TowerRadius));
    }

    private readonly List<SimEnemy> towerCasters = [];

    private void CastTowers()
    {
        CastSelf(hermenost, ActionId.ConvictionWindup, 7.9f);
        towerCasters.Clear();
        foreach (var at in towers)
        {
            if (SpawnHelper(new Placement(at, 0f), BNpcNameId.Hermenost) is not { } caster) continue;
            caster.NativeCast(ActionId.Conviction, ActionType.Action, 0f, 10.7f, false, position: at);
            towerCasters.Add(caster);
        }
    }

    private void ResolveTowers()
    {
        var unsoaked = false;
        for (var i = 0; i < towers.Count; i++)
        {
            var at = towers[i];
            if (i < towerCasters.Count) PlayEffect(towerCasters[i], ActionId.Conviction, 1.1f, at: at);
            var soakers = party.Find.InsideCircle(at, TowerSoakRadius).Where(m => m.IsAlive()).ToList();
            if (soakers.Count == 0) unsoaked = true;
            foreach (var soaker in soakers)
                damage.ApplyDamage(soaker, TowerDamage, ActionId.Conviction, "tower", lethal: false);
        }
        towers.Clear();
        if (!unsoaked) return;
        PlayEffect(hermenost, ActionId.EternalConviction, 2.1f);
        foreach (var member in AliveMembers().ToList())
            member.Die(ActionId.EternalConviction, "a tower was left unsoaked");
    }

    private void TetherShieldBashes()
    {
        var knights = ShieldBashKnights;
        for (var i = 0; i < knights.Length; i++)
        {
            if (knights[i] is not { } knight) continue;
            var first = party.Get(state.ShieldBashTethers[i]) is { } holder && holder.IsAlive() ? holder : null;
            shieldBashTethers[i] = world.Tether(knight, End.Passable(first), TetherId.HolyShieldBash);
        }
    }

    // Bots crossing the beam would otherwise pass it on after the tank has it.
    private void LockShieldBashOnTank(int i)
    {
        if (shieldBashLocked[i] || ShieldBashKnights[i] is not { } knight || shieldBashTethers[i] is not { IsActive: true } tether) return;
        if (tether.B is not ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank } tank) return;
        tether.Despawn();
        shieldBashTethers[i] = world.Tether(knight, (SimCharacter)tank, TetherId.HolyShieldBash);
        shieldBashLocked[i] = true;
    }

    private void ResolveShieldBashes()
    {
        var knights = ShieldBashKnights;
        for (var i = 0; i < knights.Length; i++)
        {
            if (knights[i] is not { } knight || shieldBashTethers[i]?.B is not { } holder) continue;
            // The Paladin charges the holder, stunning anyone in the way, and stops just short of them.
            var rotation = RotationTowards(knight.Position, holder.Position);
            var length = FlatDistance(knight.Position, holder.Position);
            PlayEffect(knight, ActionId.HolyShieldBash, 2.1f, rotation, holder.GameObjectId);
            foreach (var hit in party.Find.InsideRect(new Placement(knight.Position, rotation), ShieldBashHalfWidth, length).Where(m => m.IsAlive()).ToList())
                hit.AddStatus(StatusId.DownForTheCount, 5.96f);
            holder.AddStatus(StatusId.DownForTheCount, 5.96f);
            if (holder is not ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank })
                holder.Die(ActionId.HolyShieldBash, "only a tank survives the tether");
            shieldBashTooShort[i] = length < MinShieldBashTetherLength;
            var landing = holder.Position - new Vector3(MathF.Sin(rotation), 0f, MathF.Cos(rotation)) * ShieldBashLandShort;
            knight.SetPosition(new Placement(landing, rotation));
        }
    }

    private void HolyBladedance(uint actionId)
    {
        var knights = ShieldBashKnights;
        for (var i = 0; i < knights.Length; i++)
        {
            if (knights[i] is not { } knight || shieldBashTethers[i]?.B is not { } holder || !holder.IsAlive()) continue;
            PlayEffect(knight, actionId, 0.6f, RotationTowards(knight.Position, holder.Position), holder.GameObjectId);
            if (actionId != ActionId.HolyBladedance) continue;
            var cone = new Placement(knight.Position, RotationTowards(knight.Position, holder.Position));
            foreach (var caught in party.Find.InsideCone(cone, BladedanceHalfAngle, BladedanceRange).Where(m => m != holder && m.IsAlive()).ToList())
                caught.Die(ActionId.HolyBladedance, "stood in the tank's cleave");
            damage.ApplyDamage(holder, HolyBladedanceDamage, actionId,
                $"tether shorter than {MinShieldBashTetherLength:F0}y, cross the tethers to Thordan", lethal: shieldBashTooShort[i]);
        }
    }

    private void DashKnightsLeapOntoDefamations()
    {
        var dashers = DashKnights;
        for (var i = 0; i < dashers.Length; i++)
        {
            if (dashers[i] is not { } knight || party.Get(state.Defamations[i]) is not { } target || !target.IsAlive()) continue;
            knight.SetPosition(FacingCentre(target.Position));
        }
    }

    private void ResolveSkywardLeaps()
    {
        // One leap is survivable; the vulnerability it leaves makes a second one lethal.
        var dashers = DashKnights;
        var targets = state.Defamations.Select(role => party.Get(role)).ToList();
        var hitCount = new Dictionary<SimCharacter, int>();
        for (var i = 0; i < dashers.Length; i++)
        {
            if (targets[i] is not { } target || !target.IsAlive()) continue;
            var at = target.Position;
            PlayEffect(dashers[i], ActionId.SkywardLeap, 1.1f, target: target.GameObjectId, at: at);
            foreach (var hit in party.Find.InsideCircle(at, SkywardLeapRadius))
                hitCount[hit] = hitCount.GetValueOrDefault(hit) + 1;
        }
        foreach (var (hit, count) in hitCount)
        {
            if (count > 1)
            {
                hit.Die(ActionId.SkywardLeap, "hit by two defamations");
                continue;
            }
            damage.ApplyDamage(hit, SkywardLeapDamage, ActionId.SkywardLeap, "defamation", lethal: false);
            hit.AddStatus(StatusId.MagicVulnerabilityUp, 2.96f);
            hit.AddStatus(StatusId.PhysicalVulnerabilityUp, 5.96f);
        }
    }

    private void ResolveDragonsRage()
    {
        if (party.Get(state.StackTarget) is not { } target || !target.IsAlive()) return;
        var at = target.Position;
        PlayEffect(thordan, ActionId.DragonsRageHit, 1.1f, target: target.GameObjectId, at: at);
        var hits = damage.Resolve(IPositioned.From(at), ActionId.DragonsRageHit, [DamageType.Magic], [], stackMinTargets: DragonsRageMinTargets);
        foreach (var hit in hits.Where(h => h.IsAlive()))
            damage.ApplyDamage(hit, DragonsRageDamage, ActionId.DragonsRageHit, "stack", lethal: false);
    }

    private void CastHeavenlyHeel()
    {
        if (party.Get(PartyRole.MainTank) is not { } tank) return;
        thordan?.NativeCast(ActionId.HeavenlyHeel, ActionType.Action, 0f, 3.7f, false, targetId: tank.GameObjectId);
    }

    private void ResolveHeavenlyHeel()
    {
        if (party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        PlayEffect(thordan, ActionId.HeavenlyHeel, 1.1f, thordan == null ? null : RotationTowards(thordan.Position, tank.Position), tank.GameObjectId);
        damage.ApplyDamage(tank, HeavenlyHeelDamage, ActionId.HeavenlyHeel, "tank buster", lethal: false);
        tank.AddStatus(StatusId.SlashingResistanceDown, SlashingResistanceDownSeconds);
    }

    private void DespawnStrengthKnights()
    {
        foreach (var knight in AllKnights) knight?.Despawn();
        ignasse = paulecrain = vellguine = guerrique = hermenost = janlenoux = adelphel = grinnaux = null;
    }

    private void SpawnSanctityKnightRing()
    {
        hermenost = SpawnKnight(BNpcBaseId.Hermenost, BNpcNameId.Hermenost, 0f);
        grinnaux = SpawnKnight(BNpcBaseId.Grinnaux, BNpcNameId.Grinnaux, 45f);
        noudenet = SpawnKnight(BNpcBaseId.Noudenet, BNpcNameId.Noudenet, 90f);
        janlenoux = SpawnKnight(BNpcBaseId.Janlenoux, BNpcNameId.Janlenoux, 135f);
        adelphel = SpawnKnight(BNpcBaseId.Adelphel, BNpcNameId.Adelphel, 180f);
        charibert = SpawnKnight(BNpcBaseId.Charibert, BNpcNameId.Charibert, 225f);
        zephirin = SpawnKnight(BNpcBaseId.Zephirin, BNpcNameId.Zephirin, 270f);
        haumeric = SpawnKnight(BNpcBaseId.Haumeric, BNpcNameId.Haumeric, 315f);
        foreach (var knight in AllKnights) knight?.PlayActionTimeline(TimelineId.KnightAppear);
    }

    private void SanctityKnightsWarpOut()
    {
        thordan?.PlayActionTimeline(TimelineId.WarpStart);
        foreach (var knight in AllKnights) knight?.PlayActionTimeline(TimelineId.WarpStart);
        world.Events.Add(0.6f, () =>
        {
            foreach (var knight in AllKnights) knight?.SetVisible(false);
        });
    }

    private static void Place(SimEnemy? knight, Vector3 at, float rotation)
    {
        knight?.SetPosition(new Placement(at, rotation));
    }

    private void PlaceSanctityKnights()
    {
        var adelphelPath = state.AdelphelChargePath();
        var janlenouxPath = state.JanlenouxChargePath();
        // Adelphel faces south and Janlenoux north; which side Adelphel stands on gives the rotation.
        Place(adelphel, adelphelPath[0], 0f);
        Place(janlenoux, janlenouxPath[0], MathF.PI);
        Place(zephirin, state.DarkKnightSpot, RotationTowards(state.DarkKnightSpot, Vector3.Zero));
        Place(charibert, new Vector3(0f, 0f, -6f), 0f);
        Place(hermenost, new Vector3(0f, 0f, 6f), MathF.PI);
        Place(haumeric, new Vector3(-6f, 0f, 0f), MathF.PI / 2f);
        Place(noudenet, new Vector3(6f, 0f, 0f), -MathF.PI / 2f);
        Place(grinnaux, Vector3.Zero, 0f);
        thordan?.SetPosition(FacingCentre(state.SanctityThordanSpot));
        sanctityThordanHelper = SpawnHelper(FacingCentre(state.SanctityThordanSpot));
        eyeHelper = SpawnHelper(FacingCentre(state.EyeSpot));
    }

    private static void WarpInPlace(params SimEnemy?[] knights)
    {
        foreach (var knight in knights)
        {
            if (knight == null) continue;
            knight.SetVisible(true);
            knight.PlayActionTimeline(TimelineId.WarpEnd);
        }
    }

    private void MarkPlungeTargets()
    {
        party.Get(state.FirstPlungeTarget)?.AttachLockonVfx(LockonId.OneSword, 8.1f, persistent: false);
        party.Get(state.SecondPlungeTarget)?.AttachLockonVfx(LockonId.TwoSwords, 13.5f, persistent: false);
    }

    // Shared proximity damage on the marked player's light party.
    private void SacredSever(PartyRole marked)
    {
        if (zephirin == null || party.Get(marked) is not { } target || !target.IsAlive()) return;
        var at = target.Position;
        zephirin.SetPosition(new Placement(at, zephirin.Rotation));
        PlayEffect(zephirin, ActionId.SacredSever, 1.1f, target: target.GameObjectId, at: at);
        var hits = damage.Resolve(IPositioned.From(at), ActionId.SacredSever, [DamageType.Any],
            [(StatusId.PhysicalVulnerabilityUp, SacredSeverVulnSeconds)], stackMinTargets: SacredSeverMinTargets);
        foreach (var hit in hits.Where(h => h.IsAlive()))
            damage.ApplyDamage(hit, SacredSeverDamage, ActionId.SacredSever, "shared plunge", lethal: false);
    }

    private void ResolveDragonsGaze()
    {
        // UNVERIFIED as lethal: the gaze inflicts Hysteria (running about out of control), not modelled.
        PlayEffect(sanctityThordanHelper, ActionId.DragonsGazeHit, 1.1f);
        PlayEffect(eyeHelper, ActionId.DragonsGlory, 1.1f);
        damage.ResolveGaze(sanctityThordanHelper, lookAway: true);
        damage.ResolveGaze(eyeHelper, lookAway: true);
    }

    private static readonly float[][] BrightsphereFractions = [[0f, 0.5f, 1f], [1f / 3f, 2f / 3f, 1f], [1f / 3f, 2f / 3f, 1f]];
    private static readonly float[][] BrightsphereSpawns = [[114.52f, 114.97f, 114.97f], [116.12f, 116.58f, 116.58f], [117.77f, 117.77f, 118.15f]];
    private static readonly float[][] BrightsphereFlares = [[116.50f, 116.82f, 117.17f], [118.11f, 118.43f, 118.78f], [119.68f, 119.99f, 120.35f]];

    private void ScheduleSanctityCharges(float[] chargeTimes)
    {
        for (var charge = 0; charge < chargeTimes.Length; charge++)
        {
            var index = charge;
            world.Events.Add(chargeTimes[charge], () =>
            {
                Charge(adelphel, state.AdelphelChargePath(), index);
                Charge(janlenoux, state.JanlenouxChargePath(), index);
            });
            for (var orb = 0; orb < 3; orb++)
            {
                var o = orb;
                foreach (var path in new Func<IReadOnlyList<Vector3>>[] { state.AdelphelChargePath, state.JanlenouxChargePath })
                {
                    var spawnAt = BrightsphereSpawns[charge][o];
                    world.Events.Add(spawnAt, () =>
                    {
                        var p = path();
                        var at = Vector3.Lerp(p[index], p[index + 1], BrightsphereFractions[index][o]);
                        DropBrightsphere(at, BrightsphereFlares[index][o] - spawnAt);
                    });
                }
            }
        }
    }

    private void Charge(SimEnemy? knight, IReadOnlyList<Vector3> path, int charge)
    {
        if (knight == null) return;
        var from = path[charge];
        var to = path[charge + 1];
        var rotation = RotationTowards(from, to);
        knight.SetRotation(rotation);
        PlayEffect(knight, ActionId.SanctityShiningBlade, 1.0f, rotation, at: to);
        world.Events.Add(ShiningBladeLandDelay, () => knight.SetPosition(new Placement(to, rotation)));
        foreach (var hit in party.Find.InsideRect(new Placement(from, rotation), ShiningBladeHalfWidth, FlatDistance(from, to)).ToList())
            hit.Die(ActionId.SanctityShiningBlade, "stood in a knight's charge");
    }

    private void DropBrightsphere(Vector3 at, float flareDelay)
    {
        var orb = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.Brightsphere, NameId: BNpcNameId.Brightsphere, Level: Level,
            Targetable: false, EnemyList: EnemyListMode.Never, Placement: new Placement(at, 0f)));
        if (orb != null) helpers.Add(orb);
        world.Events.Add(flareDelay - BrightFlareCastLead, () => orb?.NativeCast(ActionId.BrightFlare, ActionType.Action, 0f, BrightFlareCastSeconds, false, position: at));
        world.Events.Add(flareDelay, () =>
        {
            PlayEffect(orb, ActionId.BrightFlare, 1.1f, at: at);
            foreach (var hit in party.Find.InsideCircle(at, BrightFlareRadius).ToList())
                hit.Die(ActionId.BrightFlare, "too close to a Brightsphere");
            world.Events.Add(EffectCarrierLinger, () => orb?.Despawn());
        });
    }

    private void ThordanLeavesArena()
    {
        PlayEffect(thordan, ActionId.ThordanLeap, 1.1f);
        world.Events.Add(0.3f, () => thordan?.SetVisible(false));
    }

    private void SpawnFirstSanctityTowers()
    {
        sanctityTowers.Clear();
        foreach (var (bearing, radius) in state.FirstTowers)
            sanctityTowers.Add((AtBearing(bearing, radius), TowerSoakRadius));
    }

    private void SpawnSecondSanctityTowers()
    {
        sanctityTowers.Clear();
        for (var bearing = 0f; bearing < 360f; bearing += 45f)
            sanctityTowers.Add((AtBearing(bearing, DsrP2ThordanState.OuterTowerRadius), TowerSoakRadius));
    }

    private void CastSanctityTowers(uint actionId, float castSeconds)
    {
        sanctityTowerCasters.Clear();
        foreach (var (at, _) in sanctityTowers)
        {
            if (SpawnHelper(new Placement(at, 0f), BNpcNameId.Hermenost) is not { } caster) continue;
            caster.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, position: at);
            sanctityTowerCasters.Add(caster);
        }
    }

    private void ResolveSanctityTowers(uint actionId)
    {
        var unsoaked = false;
        for (var i = 0; i < sanctityTowers.Count; i++)
        {
            var (at, radius) = sanctityTowers[i];
            if (i < sanctityTowerCasters.Count) PlayEffect(sanctityTowerCasters[i], actionId, 1.1f, at: at);
            var soakers = party.Find.InsideCircle(at, radius).Where(m => m.IsAlive()).ToList();
            if (soakers.Count == 0) unsoaked = true;
            foreach (var soaker in soakers)
                damage.ApplyDamage(soaker, TowerDamage, actionId, "tower", lethal: false);
        }
        sanctityTowers.Clear();
        if (!unsoaked) return;
        PlayEffect(hermenost, ActionId.EternalConviction, 2.1f);
        foreach (var member in AliveMembers().ToList())
            member.Die(ActionId.EternalConviction, "a tower was left unsoaked");
    }

    private void MarkMeteorTargets()
    {
        foreach (var role in state.MeteorTargets)
            party.Get(role)?.AttachLockonVfx(LockonId.MeteorPrey, 12f, persistent: false);
    }

    private void CastHeavensStake()
    {
        CastSelf(charibert, ActionId.HeavensStake, 6.7f);
        heavensStakes.Clear();
        foreach (var bearing in new[] { 45f, 135f, 225f, 315f })
        {
            var at = AtBearing(bearing, DsrP2ThordanState.TowerRadius);
            if (SpawnHelper(new Placement(at, 0f), BNpcNameId.Charibert) is not { } caster) continue;
            caster.NativeCast(ActionId.HeavensStakeCircle, ActionType.Action, 0f, 7.2f, false, position: at);
            heavensStakes.Add((caster, at));
        }
        if (SpawnHelper(new Placement(Vector3.Zero, 0f), BNpcNameId.Charibert) is { } donut)
        {
            donut.NativeCast(ActionId.HeavensStakeDonut, ActionType.Action, 0f, 7.2f, false, position: Vector3.Zero);
            heavensStakes.Add((donut, Vector3.Zero));
        }
    }

    private void ResolveHeavensStake()
    {
        foreach (var (caster, at) in heavensStakes)
        {
            var donut = at == Vector3.Zero;
            PlayEffect(caster, donut ? ActionId.HeavensStakeDonut : ActionId.HeavensStakeCircle, 1.1f, at: at);
            var hits = donut
                ? party.Find.InsideRing(at, HeavensStakeDonutInner, 30f)
                : party.Find.InsideCircle(at, HeavensStakeCircleRadius);
            foreach (var hit in hits.ToList())
                hit.Die(donut ? ActionId.HeavensStakeDonut : ActionId.HeavensStakeCircle, "stood in the fire");
        }
        heavensStakes.Clear();
    }

    private void SpawnFirePuddles()
    {
        foreach (var bearing in new[] { 45f, 135f, 225f, 315f })
            SpawnPuddle(EObjId.FirePuddle, AtBearing(bearing, DsrP2ThordanState.TowerRadius));
    }

    private void SpawnPuddle(uint eobjId, Vector3 at)
    {
        bleedPuddles.Add((eobjId, at));
        if (world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = eobjId, Placement = new Placement(at, 0f) }) is { } puddle)
            puddleObjects.Add((eobjId, puddle));
    }

    private void SetPuddleState(uint eobjId, ushort puddleState)
    {
        foreach (var (id, puddle) in puddleObjects)
            if (id == eobjId) puddle.SetState(puddleState);
    }

    private void ScheduleBleedChecks(uint eobjId, ushort bleed, uint actionId, float from, float to)
    {
        for (var t = from; t < to; t += BleedCheckStep)
            world.Events.Add(t, () => ApplyPuddleBleed(eobjId, bleed, actionId));
    }

    private void ApplyPuddleBleed(uint eobjId, ushort bleed, uint actionId)
    {
        var centres = bleedPuddles.Where(p => p.EObjId == eobjId).Select(p => p.At).ToList();
        if (world.Events.Elapsed < knockbackLandsAt) return;
        foreach (var member in AliveMembers().Where(m => !m.HasStatus(bleed)).ToList())
        {
            if (!centres.Any(c => FlatDistance(c, member.Position) < BleedPuddleRadius)) continue;
            member.AddStatus(bleed, BleedSeconds);
            world.Events.Add(BleedDeathDelay, () =>
            {
                if (member.IsAlive() && member.HasStatus(bleed)) member.Die(actionId, "bled out from a lingering puddle");
            });
        }
    }

    private void DespawnPuddles()
    {
        foreach (var (_, puddle) in puddleObjects) puddle.Despawn();
        puddleObjects.Clear();
        bleedPuddles.Clear();
    }

    // UNVERIFIED: the bleed for standing in the ice and fire left behind is not modelled.
    // One ice circle per cardinal pair, centred on the pair's support; a second circle is lethal.
    private void ResolveHiemalStorm()
    {
        var cardinals = state.MeteorCardinals();
        var centres = new List<Vector3>();
        foreach (var role in Enum.GetValues<PartyRole>().Where(DsrP2ThordanState.IsSupport))
        {
            if (party.Get(role) is not { } support || !support.IsAlive())
            {
                var partner = cardinals.First(kv => kv.Value == cardinals[role] && kv.Key != role).Key;
                if (party.Get(partner) is not { } stand || !stand.IsAlive()) continue;
                support = stand;
            }
            centres.Add(support.Position);
        }
        foreach (var centre in centres)
        {
            PlayEffect(haumeric, ActionId.HiemalStormHit, 1.1f, at: centre);
            world.Events.Add(0.36f, () => SpawnPuddle(EObjId.IcePuddle, centre));
        }
        foreach (var member in AliveMembers().ToList())
        {
            var circles = centres.Count(c => FlatDistance(c, member.Position) < HiemalStormRadius);
            if (circles == 0) continue;
            if (circles > 1)
            {
                member.Die(ActionId.HiemalStormHit, "caught in two ice circles");
                continue;
            }
            damage.ApplyDamage(member, HiemalStormDamage, ActionId.HiemalStormHit, "ice", lethal: false);
            member.AddStatus(StatusId.IceResistanceDownII, 2.96f);
        }
    }

    // A comet dropped within 5y of another sets off Holy Impact a few seconds later.
    private void DropHolyComets()
    {
        foreach (var role in state.MeteorTargets)
        {
            if (party.Get(role) is not { } prey || !prey.IsAlive()) continue;
            var at = prey.Position;
            var tooClose = comets.Any(c => FlatDistance(c.Position, at) < HolyCometMinSpacing);
            var comet = world.SpawnEnemy(new EnemySpawnConfig(BNpcBaseId: BNpcBaseId.HolyComet, NameId: BNpcNameId.HolyComet, Level: Level,
                Targetable: false, EnemyList: EnemyListMode.Never, Placement: new Placement(at, 0f)));
            if (comet != null) comets.Add(comet);
            world.Events.Add(HolyCometExplodeDelay, () =>
            {
                PlayEffect(comet, ActionId.HolyCometHit, 1.1f, at: at);
                foreach (var hit in party.Find.InsideCircle(at, HolyCometRadius).Where(m => m.IsAlive()).ToList())
                    damage.ApplyDamage(hit, HolyCometDamage, ActionId.HolyCometHit, "meteor", lethal: false);
            });
            if (!tooClose) continue;
            world.Events.Add(HolyImpactDelay, () =>
            {
                PlayEffect(comet, ActionId.HolyImpact, 2.1f, at: at);
                foreach (var member in AliveMembers().ToList())
                    member.Die(ActionId.HolyImpact, "meteors dropped too close together");
            });
        }
    }

    private void DespawnComets()
    {
        foreach (var comet in comets) comet.Despawn();
        comets.Clear();
    }

    private void ResolveFaithUnmoving()
    {
        if (grinnaux == null) return;
        PlayEffect(grinnaux, ActionId.FaithUnmoving, 2.1f);
        if (!KnockbackLookup.TryGet(KnockbackId.FaithUnmoving, out var distance, out var speed)) return;
        knockbackLandsAt = world.Events.Elapsed + distance / speed;
        foreach (var member in AliveMembers().ToList())
        {
            damage.ApplyDamage(member, FaithUnmovingDamage, ActionId.FaithUnmoving, "knockback", false);
            var resists = party.IsBotDriven(member)
                ? FlatDistance(member.Position, Vector3.Zero) > BotAntiKnockbackRadius
                : world.Events.Elapsed < playerAntiKnockbackUntil;
            if (!resists) (member as ISimPartyMember)?.Knockback(grinnaux.Position, distance, speed);
        }
    }

    private void ThordanReturnsNorth()
    {
        if (thordan == null) return;
        thordan.SetPosition(new Placement(new Vector3(0f, 0f, -UltimateEndKnightRadius), 0f));
        thordan.SetVisible(true);
        thordan.SetTargetable(true);
        thordan.SetTarget(party.Get(PartyRole.MainTank), follow: false);
    }

    private static readonly (uint BaseId, uint NameId, float Bearing)[] UltimateEndLineup =
    [
        (BNpcBaseId.Vellguine, BNpcNameId.Vellguine, 27f), (BNpcBaseId.Grinnaux, BNpcNameId.Grinnaux, 54f),
        (BNpcBaseId.Paulecrain, BNpcNameId.Paulecrain, 81f), (BNpcBaseId.Guerrique, BNpcNameId.Guerrique, 108f),
        (BNpcBaseId.Noudenet, BNpcNameId.Noudenet, 135f), (BNpcBaseId.Ignasse, BNpcNameId.Ignasse, 162f),
        (BNpcBaseId.Janlenoux, BNpcNameId.Janlenoux, 198f), (BNpcBaseId.Hermenost, BNpcNameId.Hermenost, 225f),
        (BNpcBaseId.Haumeric, BNpcNameId.Haumeric, 252f), (BNpcBaseId.Adelphel, BNpcNameId.Adelphel, 279f),
        (BNpcBaseId.Charibert, BNpcNameId.Charibert, 306f), (BNpcBaseId.Zephirin, BNpcNameId.Zephirin, 333f),
    ];

    private void SpawnUltimateEndKnights()
    {
        foreach (var knight in AllKnights) knight?.Despawn();
        ignasse = paulecrain = vellguine = guerrique = hermenost = janlenoux = adelphel = grinnaux = zephirin = charibert = noudenet = haumeric = null;
        foreach (var (baseId, nameId, bearing) in UltimateEndLineup)
        {
            if (SpawnKnight(baseId, nameId, bearing, UltimateEndKnightRadius) is not { } knight) continue;
            knight.PlayActionTimeline(TimelineId.KnightAppear);
            ultimateEndKnights.Add(knight);
        }
    }

    private void DespawnUltimateEndKnights()
    {
        foreach (var knight in ultimateEndKnights) knight.Despawn();
        ultimateEndKnights.Clear();
    }

    private void ThordanStepsIntoTheArena()
    {
        PlayEffect(thordan, ActionId.BroadSwingWindup, 2.1f);
        thordan?.SetPosition(new Placement(BroadSwingSpot, 0f));
    }

    // Thordan turns to a random player before each Broad Swing.
    private void CastBroadSwing(int swing)
    {
        if (thordan == null) return;
        var alive = AliveMembers().ToList();
        var facing = alive.Count > 0 ? RotationTowards(thordan.Position, alive[world.Rng.Next(alive.Count)].Position) : 0f;
        state.BroadSwingFacing[swing] = facing;
        thordan.SetRotation(facing);
        CastSelf(thordan, state.BroadSwingRightFirst[swing] ? ActionId.BroadSwingRightFirst : ActionId.BroadSwingLeftFirst, 2.7f);
    }

    // Right, left, back (or left, right, back), each a third of the circle.
    public static float BroadSwingOffset(bool rightFirst, int slash) => slash switch
    {
        0 => rightFirst ? -MathF.PI / 3f : MathF.PI / 3f,
        1 => rightFirst ? MathF.PI / 3f : -MathF.PI / 3f,
        _ => MathF.PI,
    };

    private void ResolveBroadSwing(int swing, int slash)
    {
        if (thordan == null) return;
        var rotation = state.BroadSwingFacing[swing] + BroadSwingOffset(state.BroadSwingRightFirst[swing], slash);
        PlayEffect(thordan, ActionId.BroadSwing, 0.6f, rotation);
        foreach (var hit in party.Find.InsideCone(new Placement(thordan.Position, rotation), BroadSwingHalfAngle, BroadSwingRange).Where(m => m.IsAlive()).ToList())
        {
            damage.ApplyDamage(hit, BroadSwingDamage, ActionId.BroadSwing, "Broad Swing", lethal: false);
            hit.AddStatus(StatusId.DamageDown, DamageDownSeconds);
        }
    }

    private void DespawnAll()
    {
        foreach (var caster in mercyCasters) caster.Despawn();
        mercyCasters.Clear();
        foreach (var tether in shieldBashTethers) tether?.Despawn();
        foreach (var helper in helpers) helper.Despawn();
        helpers.Clear();
        foreach (var knight in AllKnights) knight?.Despawn();
        DespawnUltimateEndKnights();
        DespawnComets();
        DespawnPuddles();
        if (Plugin.PlayerInputHooks is { } hooks) hooks.ActionExecuted -= OnPlayerAction;
        thordan?.Despawn();
    }
}
