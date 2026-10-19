using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P6DoubleDragons;

// Nidhogg and Hraesvelgr from their arrival to Revenge of the Horde, played in two halves: up to the
// second Mortal Vow pass after Wroth Flames, then from there to the enrage cast. The dragons' shared
// HP check and the kill are not simulated.
public sealed class DsrP6DoubleDragonsScenario : IScenario
{
    private readonly bool secondHalf;

    public DsrP6DoubleDragonsScenario() : this(secondHalf: false) { }

    internal DsrP6DoubleDragonsScenario(bool secondHalf) => this.secondHalf = secondHalf;

    public string Name => secondHalf ? "Hallowed Wings 2 to Cauterize" : "Wyrmsbreath to Wroth Flames";
    public float BgmSecondsAtStart => secondHalf ? SecondHalfStart : 0f;
    public IPhase Phase => DsrZone.DoubleDragons;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP6DoubleDragonsAi()];

    private readonly DsrP6DoubleDragonsStateOverrides overrides = new();
    public object SettingsOverrides => overrides;

    public const float TethersOut = 14.65f;
    public const float BreathsLand = 21.83f;
    public const float VowApplied = 29.0f;
    public const float AkhAfahCast = 32.17f;
    public const float AkhAfahLands = 40.36f;
    public const float HallowedWingsCast = 45.98f;
    public const float NidhoggCauterizeCast = 49.53f;
    public const float HallowedWingsLands = 54.26f;
    public static readonly float[] VowPasses = [62.97f, 97.03f, 131.06f, 165.10f];
    public const float WrothFlames = 66.08f;
    public static readonly float[] OrbSpawns = [70.37f, 72.39f, 75.58f];
    public static readonly float[] FlameBlastCasts = [77.69f, 79.69f, 82.68f];
    public const float FlameBlastDelay = 4.99f;
    public const float AkhMornCast = 71.38f;
    public static readonly float[] AkhMornHits = [79.36f, 81.01f, 82.57f, 84.13f];
    public const float HraesvelgrCauterizeCast = 74.46f;
    public const float HotWingCast = 85.78f;
    public const float HotWingLands = 92.26f;
    public const float FlamesResolve = 93.27f;
    public const float FirstHalfEnd = 100.0f;
    public const float SecondHalfStart = 99.0f;
    public const float SecondAkhAfahCast = 101.37f;
    public const float SecondAkhAfahLands = 109.56f;
    public const float SecondHallowedWingsCast = 114.05f;
    public const float SecondHotWingCast = 116.05f;
    public const float SecondHallowedWingsLands = 122.34f;
    public const float SecondHotWingLands = 122.52f;
    public const float SecondTethersOut = 135.00f;
    public const float SecondBreathsLand = 142.17f;
    public const float ElementsTurn = 153.13f;
    public const float CauterizeCast = 149.70f;
    public const float CauterizeLands = 154.70f;
    public const float TouchdownLands = 161.69f;
    public const float RevengeOfTheHorde = 162.94f;
    public const float SecondHalfEnd = 169.0f;

    private const uint DragonMaxHp = 4535368;
    private const uint OrbMaxHp = 100000;
    private static readonly Vector3 NidhoggHome = new(-22f, 0f, 0f);
    private static readonly Vector3 HraesvelgrHome = new(22f, 0f, 0f);
    private static readonly float[] NidhoggAutos = [8.47f, 11.58f, 24.82f, 27.94f, 59.80f, 62.92f, 66.03f, 85.87f, 88.94f, 92.02f, 95.09f, 98.21f];
    private static readonly float[] HraesvelgrAutos = [8.51f, 11.63f, 24.86f, 27.94f, 59.84f, 62.96f, 95.14f, 98.26f];
    private const float AutoAttackDamage = 0.15f;
    // UNVERIFIED: how far a breath tether must stretch; tethered players at 23y were taut, 22.6y slack.
    public const float BreathTautDistance = 21f;
    // UNVERIFIED: the breath cones' width; players 8 degrees apart from a dragon were each hit once.
    private const float BreathHalfAngle = 6f * MathF.PI / 180f;
    private const float BreathRange = 100f;
    private const float BreathDamage = 0.3f;
    private const float ResistanceDownSeconds = 3f;
    private const float ElementSeconds = 11f;
    // UNVERIFIED: the glowing dragon's cone width; a tank 20 degrees off its axis was not hit.
    private const float GlowBreathHalfAngle = 15f * MathF.PI / 180f;
    private const float GlowBreathRange = 50f;
    private const float StaggeringBreathRadius = 15f;
    private const float OrbRadius = 6f;
    private const float TankBusterDamage = 0.6f;
    // UNVERIFIED: Swirling Blizzard's safe hole; players stood alive at 19.4y.
    public const float SwirlingBlizzardInner = 20f;
    private const float SwirlingBlizzardOuter = 60f;
    private const float MortalVowRadius = 5f;
    private const float MortalVowSeconds = 34f;
    private const float MortalAtonementSeconds = 100f;
    private const float MortalVowDamage = 0.2f;
    private const float AkhAfahRadius = 4f;
    // UNVERIFIED: how few may share Akh Afah before it kills.
    private const int AkhAfahMinimum = 3;
    private const float AkhAfahDamage = 0.6f;
    private const float WingsHalfWidth = 11f;
    private const float WingsLength = 50f;
    private const float PlumeRadius = 10f;
    private const float WrothDebuffSeconds = 23f;
    private const float FlameBlastHalfWidth = 3f;
    private const float FlameBlastHalfLength = 44f;
    private const float AkhMornRadius = 6f;
    private const float AkhMornDamage = 0.25f;
    public const float SpreadRadius = 5f;
    public const float StackRadius = 4f;
    private const float FlamesDamage = 0.4f;
    private const float ElementDebuffSeconds = 30f;
    // UNVERIFIED: how far a Pyretic player may drift before the burn costs too much HP for Cauterize.
    private const float PyreticDrift = 1f;
    private const float CauterizeHalfWidth = 11f;
    private const float CauterizeLength = 80f;
    private const float CauterizeDamage = 0.5f;
    private const float CauterizeDotSeconds = 27f;
    private static readonly Vector3 NidhoggTouchdown = Vector3.Zero;
    private static readonly Vector3 HraesvelgrTouchdown = new(0f, 0f, 25f);
    // UNVERIFIED: Touchdown falls off with distance; how close turns lethal was never seen.
    public const float TouchdownLethalRadius = 10f;
    private const float TouchdownDamage = 0.3f;
    private const float EffectCarrierLinger = 3f;
    // UNVERIFIED: Hot Wing and Hot Tail only show near the end of the cast in game; the log can't time an omen.
    private const float HotWingOmenDelay = 5.3f;
    // UNVERIFIED: when a diving dragon is out of sight; it is untargetable until it is back home.
    private const float DiveOutOfSight = 2f;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private DsrP6DoubleDragonsState state = null!;

    private SimEnemy? nidhogg;
    private SimEnemy? hraesvelgr;
    private SimTether? hpLink;
    private SimCharacter? vowHolder;
    private readonly List<BreathTether> breathTethers = [];
    private readonly List<SimEnemy>[] orbSets = [[], [], []];
    private readonly List<SimEnemy> hotWingCasters = [];
    private readonly List<SimEnemy> spawned = [];
    private readonly Dictionary<SimCharacter, Vector3> pyretic = [];
    private readonly Queue<SimEnemy> helperPool = [];
    private const int HelperPoolSize = 10;

    private sealed class BreathTether(SimCharacter member, SimEnemy dragon, ushort tautId, uint breathAction)
    {
        public SimCharacter Member { get; } = member;
        public SimEnemy Dragon { get; } = dragon;
        public ushort TautId { get; } = tautId;
        public uint BreathAction { get; } = breathAction;
        public SimTether? Tether { get; set; }
        public bool Taut { get; set; }
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new DsrP6DoubleDragonsState(world.Rng, overrides);
        breathTethers.Clear();
        foreach (var set in orbSets) set.Clear();
        hotWingCasters.Clear();
        spawned.Clear();
        pyretic.Clear();
        helperPool.Clear();
        hpLink = null;
        vowHolder = null;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP6DoubleDragonsState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Touched the arena wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(NaurWaymarks));
        world.Events.Add(0f, SpawnDragons);
        world.Events.Add(0f, FillHelperPool);
        world.Events.Add(0.21f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 1U, ArenaDirector.DoubleDragonsLayout));
        world.Events.Add(0.21f, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.KnightsMap));
        world.Events.Add(3.20f, () => { nidhogg?.SetTargetable(true); hraesvelgr?.SetTargetable(true); });
        foreach (var at in NidhoggAutos) world.Events.Add(at, () => AutoAttack(nidhogg, ActionId.NidhoggP6Attack, PartyRole.MainTank));
        foreach (var at in HraesvelgrAutos) world.Events.Add(at, () => AutoAttack(hraesvelgr, ActionId.HraesvelgrAttack, PartyRole.OffTank));

        world.Events.Add(TethersOut, () => TetherBreaths(0));
        world.Events.Add(14.74f, () => CastWyrmsbreath(0));
        world.Events.Add(21.02f, () => WyrmsbreathEffects(0));
        world.Events.Add(21.74f, SwirlingBlizzard);
        world.Events.Add(BreathsLand, () => Breaths(cleansedLater: false));
        world.Events.Add(BreathsLand, () => TankBreaths(0));

        world.Events.Add(VowApplied, ApplyMortalVow);
        world.Events.Add(AkhAfahCast, CastAkhAfah);
        world.Events.Add(40.14f, AkhAfahEffects);
        world.Events.Add(AkhAfahLands, () => AkhAfah(0));

        world.Events.Add(HallowedWingsCast, () => CastSelf(hraesvelgr, HallowedWingsCastId(0), 7.2f));
        world.Events.Add(47.50f, () => FlyTo(nidhogg, state.NidhoggDiveStart));
        world.Events.Add(NidhoggCauterizeCast, () => CastDive(nidhogg, ActionId.CauterizeNidhogg));
        world.Events.Add(53.46f, () => PlayEffect(hraesvelgr, HallowedWingsCastId(0), 1.1f));
        world.Events.Add(HallowedWingsLands, () => HallowedWings(0));
        world.Events.Add(54.53f, () => Dive(nidhogg, ActionId.CauterizeNidhogg));
        // UNVERIFIED: when Nidhogg is back on his side; he autos again at 59.8.
        world.Events.Add(58.00f, () => ReturnHome(nidhogg, NidhoggHome));
        foreach (var at in VowPasses) world.Events.Add(at, PassMortalVow);

        world.Events.Add(WrothFlames, () => CastSelf(nidhogg, ActionId.WrothFlames, 2.2f));
        world.Events.Add(68.57f, () => PlayEffect(nidhogg, ActionId.WrothFlames, 1.1f));
        world.Events.Add(70.09f, MarkWrothFlames);
        for (var i = 0; i < OrbSpawns.Length; i++)
        {
            var set = i;
            world.Events.Add(OrbSpawns[i], () => SpawnOrbs(set));
            world.Events.Add(FlameBlastCasts[i], () => CastFlameBlasts(set));
            world.Events.Add(FlameBlastCasts[i] + FlameBlastDelay, () => FlameBlasts(set));
        }
        world.Events.Add(AkhMornCast, CastAkhMorn);
        world.Events.Add(73.50f, () => FlyTo(hraesvelgr, state.HraesvelgrDiveStart));
        world.Events.Add(HraesvelgrCauterizeCast, () => CastDive(hraesvelgr, ActionId.CauterizeHraesvelgr));
        for (var i = 0; i < AkhMornHits.Length; i++)
        {
            var action = i == 0 ? ActionId.AkhMorn : ActionId.AkhMornFollowUp;
            world.Events.Add(AkhMornHits[i], () => AkhMorn(action));
        }
        world.Events.Add(79.45f, () => Dive(hraesvelgr, ActionId.CauterizeHraesvelgr));
        // UNVERIFIED: when Hraesvelgr is back; he autos again at 95.1.
        world.Events.Add(85.00f, () => ReturnHome(hraesvelgr, HraesvelgrHome));
        world.Events.Add(HotWingCast, () => CastHotWingOrTail(0));
        world.Events.Add(91.27f, () => PlayEffect(nidhogg, state.HotWingAt(0) ? ActionId.HotWing : ActionId.HotTail, 1.1f));
        world.Events.Add(HotWingLands, () => HotWingOrTail(0));
        world.Events.Add(FlamesResolve, ResolveWrothFlames);
        world.Events.Add(94.00f, () => DespawnOrbs(0));
        world.Events.Add(96.10f, () => DespawnOrbs(1));
        world.Events.Add(99.30f, () => DespawnOrbs(2));

        world.Events.Add(SecondAkhAfahCast, CastAkhAfah);
        world.Events.Add(109.33f, AkhAfahEffects);
        world.Events.Add(SecondAkhAfahLands, () => AkhAfah(1));
        world.Events.Add(SecondHallowedWingsCast, () => CastSelf(hraesvelgr, HallowedWingsCastId(1), 7.2f));
        world.Events.Add(SecondHotWingCast, () => CastHotWingOrTail(1));
        world.Events.Add(121.54f, () =>
        {
            PlayEffect(hraesvelgr, HallowedWingsCastId(1), 1.1f);
            PlayEffect(nidhogg, state.HotWingAt(1) ? ActionId.HotWing : ActionId.HotTail, 1.1f);
        });
        world.Events.Add(SecondHallowedWingsLands, () => HallowedWings(1));
        world.Events.Add(SecondHotWingLands, () => HotWingOrTail(1));
        world.Events.Add(134.85f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 1U, ArenaDirector.SecondWyrmsbreathLayout));
        world.Events.Add(SecondTethersOut, () => TetherBreaths(1));
        world.Events.Add(135.08f, () => CastWyrmsbreath(1));
        world.Events.Add(141.36f, () => WyrmsbreathEffects(1));
        world.Events.Add(142.08f, SwirlingBlizzard);
        world.Events.Add(SecondBreathsLand, () => Breaths(cleansedLater: true));
        world.Events.Add(SecondBreathsLand, () => TankBreaths(1));
        // UNVERIFIED: when the dragons reach the north wall; they cast from there at 149.7.
        world.Events.Add(148.50f, () => { FlyTo(nidhogg, state.NidhoggCauterizeStart); FlyTo(hraesvelgr, state.HraesvelgrCauterizeStart); });
        world.Events.Add(CauterizeCast, () => { CastDive(nidhogg, ActionId.CauterizeNidhogg); CastDive(hraesvelgr, ActionId.CauterizeHraesvelgr); });
        world.Events.Add(ElementsTurn, TurnElementsIntoPyreticAndDeepFreeze);
        world.Events.Add(CauterizeLands, CleansingCauterize);
        world.Events.Add(CauterizeLands + 1.3f, UncleansedElementsKill);
        // UNVERIFIED: when the dragons land again; Touchdown plays from these spots at 160.8.
        world.Events.Add(159.50f, () => { ReturnHome(nidhogg, NidhoggTouchdown); ReturnHome(hraesvelgr, HraesvelgrTouchdown); });
        world.Events.Add(160.80f, () => { PlayEffect(nidhogg, ActionId.Touchdown, 1.1f); PlayEffect(hraesvelgr, ActionId.Touchdown, 1.1f); });
        world.Events.Add(TouchdownLands, Touchdown);
        world.Events.Add(RevengeOfTheHorde, () => { CastSelf(nidhogg, ActionId.DragonsRevengeOfTheHorde, 24.7f); CastSelf(hraesvelgr, ActionId.DragonsRevengeOfTheHorde, 24.7f); });
        world.Events.Add(SecondHalfEnd, DespawnAll);

        if (secondHalf) StartAtTheSecondAkhAfah();
        else EndAfterWrothFlames();
    }

    private void EndAfterWrothFlames()
    {
        world.Events.KeepWindow(0f, FirstHalfEnd);
        world.Events.Add(FirstHalfEnd, DespawnAll);
    }

    // The Vow sits on the off tank as the second half opens; the main tank and the first holder are atoned.
    private void StartAtTheSecondAkhAfah()
    {
        world.Events.KeepWindow(SecondHalfStart, SecondHalfEnd);
        world.Events.Add(0f, () => world.EnforceArenaBoundary(new SquareArena(Geometry.ArenaHalfWidth), "Touched the arena wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(NaurWaymarks));
        world.Events.Add(0f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 1U, ArenaDirector.DoubleDragonsLayout));
        world.Events.Add(0f, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.KnightsMap));
        world.Events.Add(0f, FillHelperPool);
        world.Events.Add(0f, () =>
        {
            SpawnDragons();
            nidhogg?.SetTargetable(true);
            hraesvelgr?.SetTargetable(true);
            if (party.Get(PartyRole.OffTank) is { } offTank)
            {
                offTank.AddStatus(StatusId.MortalVow, VowPasses[2] - SecondHalfStart + 0.2f);
                vowHolder = offTank;
            }
            party.Get(PartyRole.MainTank)?.AddStatus(StatusId.MortalAtonement, VowPasses[1] + MortalAtonementSeconds - SecondHalfStart);
            party.Get(state.VowTarget)?.AddStatus(StatusId.MortalAtonement, VowPasses[0] + MortalAtonementSeconds - SecondHalfStart);
        });
    }

    public void Tick(float delta, float elapsed)
    {
        if (breathTethers.Count > 0) UpdateBreathTethers();
    }

    private uint HallowedWingsCastId(int wings) => (state.WingsNorthAt(wings), state.PlumesNearAt(wings)) switch
    {
        (true, true) => ActionId.HallowedWingsNorthTanksNear,
        (true, false) => ActionId.HallowedWingsNorthTanksFar,
        (false, true) => ActionId.HallowedWingsSouthTanksNear,
        _ => ActionId.HallowedWingsSouthTanksFar,
    };

    private void SpawnDragons()
    {
        nidhogg = SpawnNpc(BNpcBaseId.NidhoggP6, BNpcNameId.Nidhogg, DragonMaxHp, new Placement(NidhoggHome, MathF.PI / 2f), EnemyListMode.Always);
        hraesvelgr = SpawnNpc(BNpcBaseId.Hraesvelgr, BNpcNameId.Hraesvelgr, DragonMaxHp, new Placement(HraesvelgrHome, -MathF.PI / 2f), EnemyListMode.Always);
    }

    private void AutoAttack(SimEnemy? dragon, uint actionId, PartyRole tankRole)
    {
        if (dragon == null || party.Get(tankRole) is not { } tank || !tank.IsAlive()) return;
        PlayEffect(dragon, actionId, 0.6f, RotationTowards(dragon.Position, tank.Position), tank.GameObjectId, tank.Position);
        damage.ApplyDamage(tank, AutoAttackDamage, actionId, "auto attack", lethal: false);
    }

    private void TetherBreaths(int wyrmsbreath)
    {
        foreach (var role in Enum.GetValues<PartyRole>().Where(r => !r.IsTank()))
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            var toNidhogg = state.NidhoggTethersAt(wyrmsbreath).Contains(role);
            var dragon = toNidhogg ? nidhogg : hraesvelgr;
            if (dragon == null) continue;
            breathTethers.Add(new BreathTether(member, dragon, toNidhogg ? TetherId.NidhoggBreath : TetherId.HraesvelgrBreath, toNidhogg ? ActionId.FlameBreath : ActionId.IceBreath));
        }
        UpdateBreathTethers(force: true);
    }

    private void UpdateBreathTethers(bool force = false)
    {
        foreach (var breath in breathTethers)
        {
            var taut = FlatDistance(breath.Member.Position, breath.Dragon.Position) >= BreathTautDistance;
            if (!force && taut == breath.Taut) continue;
            breath.Taut = taut;
            breath.Tether?.Despawn();
            breath.Tether = world.Tether(breath.Member, breath.Dragon, taut ? breath.TautId : TetherId.BreathTooClose);
        }
    }

    private uint DreadWyrmsbreathId(int wyrmsbreath) =>
        DsrP6DoubleDragonsState.NidhoggGlows(state.GlowAt(wyrmsbreath)) ? ActionId.DreadWyrmsbreathGlowing : ActionId.DreadWyrmsbreath;

    private uint GreatWyrmsbreathId(int wyrmsbreath) =>
        DsrP6DoubleDragonsState.HraesvelgrGlows(state.GlowAt(wyrmsbreath)) ? ActionId.GreatWyrmsbreathGlowing : ActionId.GreatWyrmsbreath;

    private void WyrmsbreathEffects(int wyrmsbreath)
    {
        PlayEffect(nidhogg, DreadWyrmsbreathId(wyrmsbreath), 1.1f);
        PlayEffect(hraesvelgr, GreatWyrmsbreathId(wyrmsbreath), 1.1f);
    }

    private void CastWyrmsbreath(int wyrmsbreath)
    {
        CastSelf(nidhogg, DreadWyrmsbreathId(wyrmsbreath), 6.0f);
        CastSelf(hraesvelgr, GreatWyrmsbreathId(wyrmsbreath), 6.0f);
        if (TakeHelper(new Placement(Vector3.Zero, 0f)) is not { } centre) return;
        centre.NativeCast(ActionId.SwirlingBlizzard, ActionType.Action, 0f, 6.7f, false, targetId: centre.GameObjectId, position: Vector3.Zero);
        world.Events.Add(7.1f + EffectCarrierLinger, () => ReturnHelper(centre));
    }

    private void SwirlingBlizzard()
    {
        PlayEffect(hraesvelgr, ActionId.SwirlingBlizzard, 1.1f, at: Vector3.Zero);
        foreach (var hit in party.Find.InsideRing(Vector3.Zero, SwirlingBlizzardInner, SwirlingBlizzardOuter).Where(m => m.IsAlive()).ToList())
            hit.Die(ActionId.SwirlingBlizzard, "stood in Swirling Blizzard (stay inside the donut)");
    }

    // Two breaths of one element kill. One alone leaves a debuff: lethal on the first Wyrmsbreath, which
    // the strat answers by taking one of each, and cleansed by Cauterize after the second.
    private void Breaths(bool cleansedLater)
    {
        var flame = new Dictionary<SimCharacter, int>();
        var ice = new Dictionary<SimCharacter, int>();
        foreach (var breath in breathTethers)
        {
            breath.Tether?.Despawn();
            if (!breath.Member.IsAlive()) continue;
            if (!breath.Taut)
            {
                // UNVERIFIED: what a slack tether does; the guide only says to stretch them.
                breath.Member.Die(breath.BreathAction, "breath tether too short (stretch it away from the dragon)");
                continue;
            }
            var rotation = RotationTowards(breath.Dragon.Position, breath.Member.Position);
            if (TakeHelper(new Placement(breath.Dragon.Position, rotation)) is { } helper)
            {
                PlayEffect(helper, breath.BreathAction, 1.1f, rotation, breath.Member.GameObjectId);
                world.Events.Add(EffectCarrierLinger, () => ReturnHelper(helper));
            }
            var counts = breath.BreathAction == ActionId.FlameBreath ? flame : ice;
            foreach (var hit in party.Find.InsideCone(new Placement(breath.Dragon.Position, rotation), BreathHalfAngle, BreathRange))
                counts[hit] = counts.GetValueOrDefault(hit) + 1;
        }
        breathTethers.Clear();
        foreach (var member in AliveMembers().ToList())
        {
            var flames = flame.GetValueOrDefault(member);
            var ices = ice.GetValueOrDefault(member);
            if (flames > 1) member.Die(ActionId.FlameBreath, "hit by two Flame Breaths");
            else if (ices > 1) member.Die(ActionId.IceBreath, "hit by two Ice Breaths");
            else if (flames + ices > 0)
            {
                damage.ApplyDamage(member, BreathDamage * (flames + ices), flames > 0 ? ActionId.FlameBreath : ActionId.IceBreath, "breath", lethal: false);
                if (flames > 0) member.AddStatus(StatusId.FireResistanceDownII, ResistanceDownSeconds);
                if (ices > 0) member.AddStatus(StatusId.IceResistanceDownII, ResistanceDownSeconds);
                if (flames != ices) LeaveElement(member, flames > 0, cleansedLater);
            }
        }
    }

    private void LeaveElement(SimCharacter member, bool boiling, bool cleansedLater)
    {
        var status = boiling ? StatusId.Boiling : StatusId.Freezing;
        member.AddStatus(status, ElementSeconds);
        if (cleansedLater) return;
        world.Events.Add(ElementSeconds - 0.05f, () =>
        {
            if (member.IsAlive() && member.HasStatus(status))
                member.Die(boiling ? ActionId.FlameBreath : ActionId.IceBreath, boiling ? "Boiling ran out (take one breath of each element)" : "Freezing ran out (take one breath of each element)");
        });
    }

    private void TankBreaths(int wyrmsbreath)
    {
        var glow = state.GlowAt(wyrmsbreath);
        if (glow == BreathGlow.Both)
        {
            SharedOrb(nidhogg, ActionId.DarkOrb, PartyRole.MainTank);
            SharedOrb(hraesvelgr, ActionId.HolyOrb, PartyRole.OffTank);
            return;
        }
        var (glowing, glowAction, quiet, tankRole) = glow == BreathGlow.Nidhogg
            ? (nidhogg, ActionId.DarkBreath, hraesvelgr, PartyRole.OffTank)
            : (hraesvelgr, ActionId.HolyBreath, nidhogg, PartyRole.MainTank);
        if (glowing != null)
        {
            var rotation = RotationTowards(glowing.Position, Vector3.Zero);
            PlayEffect(glowing, glowAction, 1.1f, rotation);
            foreach (var hit in party.Find.InsideCone(new Placement(glowing.Position, rotation), GlowBreathHalfAngle, GlowBreathRange).Where(m => m.IsAlive()).ToList())
                hit.Die(glowAction, "stood in the glowing dragon's breath (leave the middle)");
        }
        if (quiet == null || party.Get(tankRole) is not { } tank || !tank.IsAlive()) return;
        PlayEffect(quiet, ActionId.StaggeringBreath, 1.1f, target: tank.GameObjectId, at: tank.Position);
        foreach (var hit in party.Find.InsideCircle(tank.Position, StaggeringBreathRadius).Where(m => m.IsAlive()).ToList())
        {
            if (hit == tank) damage.ApplyDamage(hit, TankBusterDamage, ActionId.StaggeringBreath, "tank buster", lethal: false);
            else hit.Die(ActionId.StaggeringBreath, "too close to the tank taking Staggering Breath");
        }
    }

    // UNVERIFIED: never seen in the logs; each orb has to land on both tanks.
    private void SharedOrb(SimEnemy? dragon, uint actionId, PartyRole tankRole)
    {
        if (party.Get(tankRole) is not { } tank || !tank.IsAlive()) return;
        PlayEffect(dragon, actionId, 1.1f, target: tank.GameObjectId, at: tank.Position);
        var hits = party.Find.InsideCircle(tank.Position, OrbRadius).Where(m => m.IsAlive()).ToList();
        foreach (var hit in hits)
        {
            if (!IsTank(hit)) hit.Die(actionId, "stood in the tanks' shared buster");
            else if (hits.Count(IsTank) < 2) hit.Die(actionId, "took the shared buster alone (tanks stack)");
            else damage.ApplyDamage(hit, TankBusterDamage / 2f, actionId, "shared tank buster", lethal: false);
        }
    }

    // UNVERIFIED: the real game halves the max HP of anyone else caught; here it kills.
    private void ApplyMortalVow()
    {
        if (party.Get(state.VowTarget) is not { } target || !target.IsAlive()) return;
        PlayEffect(nidhogg, ActionId.MortalVow, 1.1f, target: target.GameObjectId, at: target.Position);
        foreach (var hit in party.Find.InsideCircle(target.Position, MortalVowRadius).Where(m => m.IsAlive() && m != target).ToList())
            hit.Die(ActionId.MortalVow, "stood next to the Mortal Vow target (DPS spread)");
        damage.ApplyDamage(target, MortalVowDamage, ActionId.MortalVow, "Mortal Vow", lethal: false);
        target.AddStatus(StatusId.MortalVow, MortalVowSeconds);
        vowHolder = target;
    }

    // The holder's Vow bursts on whoever stands within 5y; exactly one player must take it.
    private void PassMortalVow()
    {
        if (vowHolder is not { } holder) return;
        vowHolder = null;
        holder.RemoveStatus(StatusId.MortalVow);
        if (holder.IsAlive()) holder.AddStatus(StatusId.MortalAtonement, MortalAtonementSeconds);
        var hits = party.Find.InsideCircle(holder.Position, MortalVowRadius).Where(m => m.IsAlive() && m != holder).ToList();
        var shownOn = hits.Count == 1 ? hits[0] : holder;
        PlayEffect(nidhogg, ActionId.MortalVowPass, 1.1f, RotationTowards(holder.Position, shownOn.Position), shownOn.GameObjectId, holder.Position);
        if (hits.Count == 0)
        {
            foreach (var member in AliveMembers().ToList())
                member.Die(ActionId.MortalVowPass, "nobody took the Mortal Vow pass (Nidhogg enrages)");
            return;
        }
        if (hits.Count > 1)
        {
            foreach (var hit in hits) hit.Die(ActionId.MortalVowPass, "caught in a Mortal Vow pass meant for one");
            return;
        }
        var next = hits[0];
        if (next.HasStatus(StatusId.MortalAtonement))
        {
            next.Die(ActionId.MortalVowPass, "took the Mortal Vow back while atoned");
            return;
        }
        damage.ApplyDamage(next, MortalVowDamage, ActionId.MortalVowPass, "Mortal Vow", lethal: false);
        next.AddStatus(StatusId.MortalVow, MortalVowSeconds);
        vowHolder = next;
    }

    private void CastAkhAfah()
    {
        CastSelf(nidhogg, ActionId.AkhAfahNidhogg, 7.7f);
        CastSelf(hraesvelgr, ActionId.AkhAfahHraesvelgr, 7.7f);
        if (nidhogg != null && hraesvelgr != null) hpLink = world.Tether(nidhogg, hraesvelgr, TetherId.DragonHpLink, duration: 8.2f);
    }

    private void AkhAfahEffects()
    {
        PlayEffect(nidhogg, ActionId.AkhAfahNidhogg, 1.1f);
        PlayEffect(hraesvelgr, ActionId.AkhAfahHraesvelgr, 1.1f);
    }

    // A light party stack on each healer; one player in both, or too few in one, dies.
    private void AkhAfah(int akhAfah)
    {
        var hits = new Dictionary<SimCharacter, int>();
        var stacks = new List<(uint Action, List<SimCharacter> Hits)>();
        foreach (var (dragon, action, role) in new[] { (hraesvelgr, ActionId.AkhAfahHraesvelgrStack, state.HraesvelgrStackTarget(akhAfah)), (nidhogg, ActionId.AkhAfahNidhoggStack, state.NidhoggStackTarget(akhAfah)) })
        {
            if (party.Get(role) is not { } healer || !healer.IsAlive()) continue;
            PlayEffect(dragon, action, 1.1f, target: healer.GameObjectId, at: healer.Position);
            var inside = party.Find.InsideCircle(healer.Position, AkhAfahRadius).Where(m => m.IsAlive()).ToList();
            stacks.Add((action, inside));
            foreach (var hit in inside) hits[hit] = hits.GetValueOrDefault(hit) + 1;
        }
        foreach (var (action, inside) in stacks)
            foreach (var hit in inside.Where(h => h.IsAlive()))
            {
                if (hits[hit] > 1) hit.Die(action, "stood in both Akh Afah stacks");
                else if (inside.Count < AkhAfahMinimum) hit.Die(action, "Akh Afah not shared by a light party");
                else damage.ApplyDamage(hit, AkhAfahDamage, action, "Akh Afah", lethal: false);
            }
    }

    private void HallowedWings(int wings)
    {
        if (hraesvelgr == null) return;
        var north = state.WingsNorthAt(wings);
        var near = state.PlumesNearAt(wings);
        var wingAction = north ? ActionId.HallowedWingsNorth : ActionId.HallowedWingsSouth;
        var side = north ? -WingsHalfWidth : WingsHalfWidth;
        var wing = new Placement(new Vector3(HraesvelgrHome.X + 3f, 0f, side), -MathF.PI / 2f);
        if (TakeHelper(wing) is { } helper)
        {
            PlayEffect(helper, wingAction, 1.1f, wing.Rotation);
            world.Events.Add(EffectCarrierLinger, () => ReturnHelper(helper));
        }
        foreach (var hit in party.Find.InsideRect(wing, WingsHalfWidth, WingsLength).Where(m => m.IsAlive()).ToList())
            hit.Die(wingAction, "stood under Hraesvelgr's glowing wing");

        var byDistance = AliveMembers().OrderBy(m => FlatDistance(m.Position, hraesvelgr.Position)).ToList();
        var targets = (near ? byDistance.Take(2) : byDistance.AsEnumerable().Reverse().Take(2)).ToList();
        var plumes = new Dictionary<SimCharacter, int>();
        foreach (var target in targets)
        {
            PlayEffect(hraesvelgr, ActionId.HallowedPlume, 1.1f, RotationTowards(hraesvelgr.Position, target.Position), target.GameObjectId, target.Position);
            foreach (var hit in party.Find.InsideCircle(target.Position, PlumeRadius))
                plumes[hit] = plumes.GetValueOrDefault(hit) + 1;
        }
        foreach (var (hit, count) in plumes.Where(kv => kv.Key.IsAlive()).ToList())
        {
            if (!IsTank(hit)) hit.Die(ActionId.HallowedPlume, near ? "hit by Hallowed Plume (head down: tanks close, party far)" : "hit by Hallowed Plume (head up: tanks far, party close)");
            else if (count > 1) hit.Die(ActionId.HallowedPlume, "took both Hallowed Plumes (tanks spread)");
            else damage.ApplyDamage(hit, TankBusterDamage, ActionId.HallowedPlume, "tank buster", lethal: false);
        }
    }

    private void FlyTo(SimEnemy? dragon, Vector3 at)
    {
        if (dragon == null) return;
        dragon.SetTargetable(false);
        dragon.SetPosition(new Placement(at, RotationTowards(at, new Vector3(at.X, 0f, 0f))));
    }

    private void ReturnHome(SimEnemy? dragon, Vector3 home)
    {
        if (dragon == null) return;
        dragon.SetPosition(new Placement(home, RotationTowards(home, Vector3.Zero)));
        dragon.SetVisible(true);
        dragon.SetTargetable(true);
    }

    private static void CastDive(SimEnemy? dragon, uint actionId) =>
        dragon?.NativeCast(actionId, ActionType.Action, 0f, 4.7f, false, rotation: dragon.Rotation, position: dragon.Position);

    private void Dive(SimEnemy? dragon, uint actionId)
    {
        if (dragon == null) return;
        PlayEffect(dragon, actionId, 1.1f, dragon.Rotation);
        damage.Resolve(dragon, actionId, [DamageType.Lethal], []);
        world.Events.Add(DiveOutOfSight, () => dragon.SetVisible(false));
    }

    private void MarkWrothFlames()
    {
        foreach (var role in state.Spreads) party.Get(role)?.AddStatus(StatusId.SpreadingFlames, WrothDebuffSeconds);
        foreach (var role in state.Stacks) party.Get(role)?.AddStatus(StatusId.EntangledFlames, WrothDebuffSeconds);
    }

    private void SpawnOrbs(int set)
    {
        foreach (var at in state.OrbSet(set))
            if (SpawnNpc(BNpcBaseId.ScarletPrice, BNpcNameId.ScarletPrice, OrbMaxHp, new Placement(at, 0f), EnemyListMode.Never) is { } orb)
                orbSets[set].Add(orb);
    }

    private void CastFlameBlasts(int set)
    {
        foreach (var orb in orbSets[set])
            orb.NativeCast(ActionId.FlameBlast, ActionType.Action, 0f, 4.7f, false, rotation: 0f, position: orb.Position);
    }

    private void FlameBlasts(int set)
    {
        foreach (var orb in orbSets[set])
        {
            PlayEffect(orb, ActionId.FlameBlast, 1.1f, 0f);
            foreach (var hit in party.Find.InsideCross(new Placement(orb.Position, 0f), FlameBlastHalfWidth, FlameBlastHalfLength).Where(m => m.IsAlive()).ToList())
                hit.Die(ActionId.FlameBlast, "stood in a Flame Blast cross");
        }
    }

    private void DespawnOrbs(int set)
    {
        foreach (var orb in orbSets[set]) orb.Despawn();
        orbSets[set].Clear();
    }

    private void CastAkhMorn()
    {
        if (nidhogg == null || party.Get(state.AkhMornTarget) is not { } target) return;
        nidhogg.NativeCast(ActionId.AkhMorn, ActionType.Action, 0f, 7.7f, false, targetId: target.GameObjectId);
    }

    // UNVERIFIED: the puddles each hit leaves are not modelled; anyone outside the stack wipes the party.
    private void AkhMorn(uint actionId)
    {
        if (party.Get(state.AkhMornTarget) is not { } target || !target.IsAlive()) return;
        PlayEffect(nidhogg, actionId, 1.1f, target: target.GameObjectId, at: target.Position);
        var alive = AliveMembers().ToList();
        var hits = party.Find.InsideCircle(target.Position, AkhMornRadius).Where(m => m.IsAlive()).ToList();
        foreach (var member in alive)
        {
            if (hits.Count < alive.Count) member.Die(actionId, "Akh Morn not shared by the whole party");
            else damage.ApplyDamage(member, AkhMornDamage, actionId, "Akh Morn", lethal: false);
        }
    }

    private void CastHotWingOrTail(int hotWing)
    {
        if (nidhogg == null) return;
        var wing = state.HotWingAt(hotWing);
        CastSelf(nidhogg, wing ? ActionId.HotWing : ActionId.HotTail, 5.2f);
        var lanes = wing ? new[] { -14.5f, 14.5f } : [0f];
        foreach (var z in lanes)
        {
            if (TakeHelper(new Placement(new Vector3(NidhoggHome.X, 0f, z), MathF.PI / 2f)) is not { } helper) continue;
            helper.NativeCast(wing ? ActionId.HotWingSides : ActionId.HotTailMiddle, ActionType.Action, HotWingOmenDelay, 6.2f, false, rotation: helper.Rotation, position: helper.Position);
            hotWingCasters.Add(helper);
        }
    }

    private void HotWingOrTail(int hotWing)
    {
        var action = state.HotWingAt(hotWing) ? ActionId.HotWingSides : ActionId.HotTailMiddle;
        foreach (var helper in hotWingCasters)
        {
            PlayEffect(helper, action, 1.1f, helper.Rotation);
            damage.Resolve(helper, action, [DamageType.Lethal], []);
            world.Events.Add(EffectCarrierLinger, () => ReturnHelper(helper));
        }
        hotWingCasters.Clear();
    }

    // Spreads hit everyone near them; each stack needs exactly one partner and no one else's debuff.
    private void ResolveWrothFlames()
    {
        var deaths = new Dictionary<SimCharacter, (uint Action, string Reason)>();
        var stackHits = new Dictionary<SimCharacter, int>();
        foreach (var role in state.Spreads)
        {
            if (party.Get(role) is not { } spread) continue;
            spread.RemoveStatus(StatusId.SpreadingFlames);
            if (!spread.IsAlive()) continue;
            PlayEffect(nidhogg, ActionId.SpreadingFlames, 1.1f, target: spread.GameObjectId, at: spread.Position);
            foreach (var hit in party.Find.InsideCircle(spread.Position, SpreadRadius).Where(m => m.IsAlive() && m != spread))
                deaths.TryAdd(hit, (ActionId.SpreadingFlames, "too close to a Spreading Flames player"));
        }
        foreach (var role in state.Stacks)
        {
            if (party.Get(role) is not { } stack) continue;
            stack.RemoveStatus(StatusId.EntangledFlames);
            if (!stack.IsAlive()) continue;
            PlayEffect(nidhogg, ActionId.EntangledFlames, 1.1f, target: stack.GameObjectId, at: stack.Position);
            var inside = party.Find.InsideCircle(stack.Position, StackRadius).Where(m => m.IsAlive()).ToList();
            if (inside.Count < 2) deaths.TryAdd(stack, (ActionId.EntangledFlames, "took Entangled Flames alone (needs a partner)"));
            foreach (var hit in inside) stackHits[hit] = stackHits.GetValueOrDefault(hit) + 1;
        }
        foreach (var (hit, count) in stackHits)
        {
            if (count > 1) deaths.TryAdd(hit, (ActionId.EntangledFlames, "stood in both Entangled Flames"));
            else if (state.Spreads.Contains(RoleOf(hit))) deaths.TryAdd(hit, (ActionId.EntangledFlames, "took an Entangled Flames with Spreading Flames"));
        }
        foreach (var (member, (action, reason)) in deaths) member.Die(action, reason);
        foreach (var hit in stackHits.Keys.Where(h => h.IsAlive()))
        {
            damage.ApplyDamage(hit, FlamesDamage, ActionId.EntangledFlames, "Entangled Flames", lethal: false);
            hit.AddStatus(StatusId.FireResistanceDownII, ResistanceDownSeconds);
        }
    }

    private void TurnElementsIntoPyreticAndDeepFreeze()
    {
        foreach (var member in AliveMembers().ToList())
        {
            if (member.HasStatus(StatusId.Boiling))
            {
                member.RemoveStatus(StatusId.Boiling);
                member.AddStatus(StatusId.Pyretic, ElementDebuffSeconds);
                pyretic[member] = member.Position;
            }
            if (member.HasStatus(StatusId.Freezing))
            {
                member.RemoveStatus(StatusId.Freezing);
                member.AddStatus(StatusId.DeepFreeze, ElementDebuffSeconds);
            }
        }
    }

    // Each dragon dives its half; the first player in line takes a tank buster, the rest are cleansed
    // of the opposite element and killed by their own.
    private void CleansingCauterize()
    {
        foreach (var (dragon, action, cleanses, kills, dot) in new[]
                 {
                     (nidhogg, ActionId.CauterizeNidhogg, StatusId.DeepFreeze, StatusId.Pyretic, StatusId.Burns),
                     (hraesvelgr, ActionId.CauterizeHraesvelgr, StatusId.Pyretic, StatusId.DeepFreeze, StatusId.Frostbite),
                 })
        {
            if (dragon == null) continue;
            PlayEffect(dragon, action, 1.1f, dragon.Rotation);
            var lane = new Placement(dragon.Position, dragon.Rotation);
            var hits = party.Find.InsideRect(lane, CauterizeHalfWidth, CauterizeLength).Where(m => m.IsAlive())
                .OrderBy(m => FlatDistance(m.Position, dragon.Position)).ToList();
            for (var i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                if (i == 0 && !IsTank(hit))
                {
                    hit.Die(action, "first in line for Cauterize (tanks stand in front of the dragons)");
                    continue;
                }
                if (hit.HasStatus(kills))
                {
                    hit.Die(action, "dived on by the dragon of your own element (go to the other one)");
                    continue;
                }
                if (pyretic.TryGetValue(hit, out var from) && FlatDistance(from, hit.Position) > PyreticDrift)
                {
                    hit.Die(action, "moved with Pyretic before Cauterize");
                    continue;
                }
                hit.RemoveStatus(cleanses);
                pyretic.Remove(hit);
                damage.ApplyDamage(hit, CauterizeDamage, action, "Cauterize", lethal: false);
                if (i == 0) hit.AddStatus(dot, CauterizeDotSeconds);
            }
        }
    }

    // UNVERIFIED: an uncleansed Pyretic or Deep Freeze is taken as lethal.
    private void UncleansedElementsKill()
    {
        foreach (var member in AliveMembers().ToList())
        {
            if (member.HasStatus(StatusId.Pyretic)) member.Die(ActionId.CauterizeHraesvelgr, "Pyretic never cleansed (stand in Hraesvelgr's dive)");
            else if (member.HasStatus(StatusId.DeepFreeze)) member.Die(ActionId.CauterizeNidhogg, "Deep Freeze never cleansed (stand in Nidhogg's dive)");
        }
        pyretic.Clear();
    }

    private void Touchdown()
    {
        foreach (var (dragon, at) in new[] { (nidhogg, NidhoggTouchdown), (hraesvelgr, HraesvelgrTouchdown) })
        {
            PlayEffect(dragon, ActionId.TouchdownHit, 1.1f, at: at);
            foreach (var member in AliveMembers().ToList())
            {
                if (FlatDistance(member.Position, at) <= TouchdownLethalRadius) member.Die(ActionId.TouchdownHit, "too close to a dragon's Touchdown (wait at the north wall)");
                else damage.ApplyDamage(member, TouchdownDamage, ActionId.TouchdownHit, "Touchdown", lethal: false);
            }
        }
    }

    private void DespawnAll()
    {
        foreach (var breath in breathTethers) breath.Tether?.Despawn();
        breathTethers.Clear();
        hpLink?.Despawn();
        foreach (var helper in hotWingCasters.Concat(helperPool)) helper.Despawn();
        helperPool.Clear();
        hotWingCasters.Clear();
        for (var i = 0; i < orbSets.Length; i++) DespawnOrbs(i);
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
    }

    private SimEnemy? SpawnNpc(uint baseId, uint nameId, uint maxHp, Placement placement, EnemyListMode enemyList)
    {
        var enemy = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: false,
            EnemyList: enemyList,
            Placement: placement,
            NpcSpawnTemplate: DsrNpcSpawn.Build(baseId, nameId, maxHp)));
        if (enemy != null) spawned.Add(enemy);
        return enemy;
    }

    // A helper spawned in the frame it acts drops the effect or cast, so they wait in a pool.
    private void FillHelperPool()
    {
        for (var i = 0; i < HelperPoolSize; i++)
            if (SpawnHelper(new Placement(Vector3.Zero, 0f)) is { } helper) helperPool.Enqueue(helper);
    }

    private SimEnemy? TakeHelper(Placement placement)
    {
        if (!helperPool.TryDequeue(out var helper)) return SpawnHelper(placement);
        helper.SetPosition(placement);
        return helper;
    }

    private void ReturnHelper(SimEnemy helper) => helperPool.Enqueue(helper);

    private SimEnemy? SpawnHelper(Placement placement) =>
        world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Dummy,
            NameId: BNpcNameId.Nidhogg,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Visibility: SimEnemy.LegacyVisibility(true, true),
            Placement: placement));

    private PartyRole RoleOf(SimCharacter member) => Enum.GetValues<PartyRole>().First(r => party.Get(r) == member);

    private bool IsTank(SimCharacter member) => RoleOf(member).IsTank();

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    private static void CastSelf(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, targetId: caster.GameObjectId);

    // An effect without a position plays at the arena centre, so default it to the caster.
    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);

    private static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    public static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));
}
