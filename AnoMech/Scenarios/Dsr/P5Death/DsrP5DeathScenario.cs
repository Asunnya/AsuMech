using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Dsr.P1Knights;
using AnoMech.Scenarios.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P5Death;

// Death of the Heavens, from its cast to the Holy Meteor spawn. The meteors are not simulated:
// they fade on their own and the run ends.
public sealed class DsrP5DeathScenario : IScenario
{
    public string Name => "Death of the Heavens";
    public IPhase Phase => DsrZone.DarkThordan;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP5DeathAi()];

    private readonly DsrP5DeathSettingsWindow settingsWindow = new();
    public object SettingsOverrides => settingsWindow.Overrides;
    public void DrawSettings() => settingsWindow.Draw();

    public const float Death = 3.0f;
    public const float DoomsMarked = Death + 11.72f;
    public const float DivesLand = Death + 20.99f;
    public const float PuddlesDropped = Death + 21.08f;
    public const float LightningLands = Death + 21.17f;
    public static readonly float[] Earthquakes = [Death + 19.11f, Death + 21.03f, Death + 22.90f, Death + 24.78f, Death + 26.65f];
    public const float SymbolsMarked = Death + 29.05f;
    public const float Knockback = Death + 34.23f;
    // Just before the debuff runs out on its own.
    public const float DoomEnds = Death + 37.62f;

    private const uint ThordanMaxHp = 5821796;
    private const uint KnightMaxHp = 6920000;
    private const uint MeteorMaxHp = 116948;
    private const float ThordanLandDelay = 0.35f;
    private const float DoomSeconds = 26f;
    private const float HeavyImpactOmenDelay = 2.7f;
    private const float HeavyImpactRingWidth = 6f;
    private const float LightningStormRadius = 5f;
    private const float LightningStormDamage = 0.3f;
    // UNVERIFIED: how close to a twister sets it off.
    private const float TwisterTouchRadius = 1.5f;
    // UNVERIFIED: when a twister starts to bite; it rises first, as in Wrath of the Heavens.
    private const float TwisterArms = Death + 23.1f;
    private const float TwistersGone = Death + 26.9f;
    private const float PuddleRadius = 4f;
    private const float PuddleOpens = Death + 31.30f;
    private const float FaithUnmovingDamage = 0.1f;
    private const float ChainBreakDistance = 30f;
    private const float HolyChainDamage = 0.8f;
    private const float DamageDownSeconds = 180f;
    private const float HeavensflameDamage = 0.35f;
    private const float FireResistanceDownSeconds = 2.96f;
    private const float MeteorRadius = 14f;
    private const float EffectCarrierLinger = 3f;
    private const int CarrierCount = 12;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private DsrP5DeathState state = null!;

    private SimEnemy? thordan;
    private SimEnemy? guerrique;
    private SimEnemy? heavyImpactCaster;
    private SimEnemy? zephirin;
    private SimEnemy? vedrfolnir;
    private SimEnemy? vidofnir;
    private SimEnemy? darkscale;
    private SimEnemy? charibert;
    private SimEnemy? noudenet;
    private SimEnemy? grinnaux;
    private SimEnemy? thordanGazeHelper;
    private SimEnemy? eyeHelper;
    private readonly List<SimEnemy> spawned = [];
    private readonly List<SimEnemy> carriers = [];
    private readonly List<SimEnemy> meteors = [];
    private int nextCarrier;
    private readonly List<SimEventObject> eventObjects = [];
    private readonly List<(Vector3 At, SimEventObject? Obj)> twisters = [];
    private readonly List<(Vector3 At, SimEventObject? Obj)> openPuddles = [];
    private readonly List<SimTether> burningChains = [];
    private readonly HashSet<SimCharacter> chainBurned = [];
    private bool twistersArmed;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new DsrP5DeathState(world.Rng, settingsWindow.Overrides);
        spawned.Clear();
        carriers.Clear();
        meteors.Clear();
        nextCarrier = 0;
        eventObjects.Clear();
        twisters.Clear();
        openPuddles.Clear();
        burningChains.Clear();
        chainBurned.Clear();
        twistersArmed = false;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP5DeathState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.ThordanArenaRadius, "Touched the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(NaurWaymarks));
        world.Events.Add(0f, SpawnThordan);
        world.Events.Add(0.20f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 0U, ArenaDirector.WrathLayout));
        world.Events.Add(0.20f, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.ThordanMap));
        world.Events.Add(Death, () => CastSelf(thordan, ActionId.DeathOfTheHeavens, 3.7f));
        world.Events.Add(Death + 3.96f, () => PlayEffect(thordan, ActionId.DeathOfTheHeavens, 1.1f));
        world.Events.Add(Death + 7.04f, ThordanLeapsOut);
        world.Events.Add(Death + 8.60f, SpawnWarriorAndDiveTeam);
        world.Events.Add(Death + 9.10f, () => { foreach (var k in new[] { guerrique, zephirin, vedrfolnir }) k?.PlayActionTimeline(TimelineId.WarpEnd); });
        world.Events.Add(Death + 9.10f, () => { vidofnir?.PlayActionTimeline(TimelineId.DragonAppear); darkscale?.PlayActionTimeline(TimelineId.DragonAppear); });
        world.Events.Add(Death + 10.91f, Deathstorm);
        world.Events.Add(Death + 13.14f, () => CastSelf(guerrique, ActionId.HeavyImpactWindup, 4.0f));
        world.Events.Add(Death + 13.14f, () => heavyImpactCaster?.NativeCast(ActionId.HeavyImpact, ActionType.Action, HeavyImpactOmenDelay, 5.7f, false, position: state.WarriorSpot));
        world.Events.Add(Death + 15.01f, CastStaticDives);
        world.Events.Add(Death + 17.42f, () => PlayEffect(guerrique, ActionId.HeavyImpactWindup, 2.1f));
        for (var wave = 0; wave < Earthquakes.Length; wave++)
        {
            var w = wave;
            world.Events.Add(Earthquakes[wave], () => Earthquake(w));
        }
        world.Events.Add(Death + 20.26f, () => PlayEffect(vidofnir, ActionId.WingsOfSalvation, 1.1f));
        world.Events.Add(DivesLand, StaticDives);
        world.Events.Add(PuddlesDropped, DropPuddles);
        world.Events.Add(LightningLands, LightningStorm);
        world.Events.Add(Death + 22.08f, SpawnTwisters);
        world.Events.Add(Death + 22.68f, () => { foreach (var (_, obj) in twisters) obj?.SetState(1); });
        world.Events.Add(TwisterArms, () => twistersArmed = true);
        world.Events.Add(TwistersGone, ClearTwisters);
        world.Events.Add(Death + 24.81f, () => world.Map.AddEffect(EyeEffect.Opens, state.EyeSlot));
        world.Events.Add(Death + 27.50f, SpawnPlaystationKnights);
        world.Events.Add(SymbolsMarked, MarkSymbols);
        world.Events.Add(Death + 29.14f, () => CastSelf(charibert, ActionId.Heavensflame, 6.7f));
        world.Events.Add(Death + 29.14f, () => CastSelf(thordan, ActionId.DragonsGaze, 3.7f));
        world.Events.Add(Death + 30.26f, () => CastSelf(noudenet, ActionId.HolyMeteor, 7.7f));
        world.Events.Add(Death + 30.26f, () => CastSelf(grinnaux, ActionId.FaithUnmoving, 3.7f));
        world.Events.Add(Death + 31.06f, () => PlayEffect(vidofnir, ActionId.WingsOfSalvationPuddle, 1.1f));
        world.Events.Add(PuddleOpens, OpenPuddles);
        world.Events.Add(Death + 33.06f, TetherBurningChains);
        world.Events.Add(Death + 34.22f, () => world.Map.AddEffect(EyeEffect.Glares, state.EyeSlot));
        world.Events.Add(Knockback, FaithUnmoving);
        world.Events.Add(Death + 34.27f, DragonsGaze);
        world.Events.Add(Death + 35.16f, () => world.Map.AddEffect(EyeEffect.Closes, state.EyeSlot));
        world.Events.Add(Death + 36.18f, HolyChain);
        world.Events.Add(Death + 36.77f, Heavensflame);
        world.Events.Add(DoomEnds, DoomFalls);
        world.Events.Add(Death + 38.23f, HolyMeteor);
        world.Events.Add(Death + 44.00f, () => meteors.ForEach(m => m.FadeOut()));
        world.Events.Add(Death + 46.00f, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        if (twistersArmed) TouchTwisters();
        if (openPuddles.Count > 0) CleanseInPuddles();
        BreakStretchedChains();
    }

    private void SpawnThordan()
    {
        thordan = SpawnNpc(BNpcBaseId.DarkThordan, BNpcNameId.Thordan, ThordanMaxHp, new Placement(Vector3.Zero, 0f), EnemyListMode.Always);
        world.Events.Add(0.5f, () =>
        {
            thordan?.SetTargetable(true);
            thordan?.SetTarget(party.Get(PartyRole.MainTank), follow: false);
        });
    }

    // The client animates the leap to the effect's position, so that must be the landing spot.
    private void ThordanLeapsOut()
    {
        if (thordan == null) return;
        thordan.SetTargetable(false);
        thordan.SetTarget(null, follow: false);
        var to = new Placement(state.ThordanSpot, RotationTowards(state.ThordanSpot, Vector3.Zero));
        PlayEffect(thordan, ActionId.ThordanLeap, 1.1f, to.Rotation, at: to.Position);
        world.Events.Add(ThordanLandDelay, () => thordan?.SetPosition(to));
    }

    private void SpawnWarriorAndDiveTeam()
    {
        guerrique = SpawnNpc(BNpcBaseId.Guerrique, BNpcNameId.Guerrique, KnightMaxHp, Facing(state.WarriorSpot), EnemyListMode.Never);
        zephirin = SpawnNpc(BNpcBaseId.Zephirin, BNpcNameId.Zephirin, KnightMaxHp, Facing(state.Outside(DsrP5DeathState.SpearBearing, DsrP5DeathState.OutsideRadius)), EnemyListMode.Never);
        vedrfolnir = SpawnNpc(BNpcBaseId.Vedrfolnir, BNpcNameId.Vedrfolnir, KnightMaxHp, Facing(state.Outside(DsrP5DeathState.TwisterDragonBearing, DsrP5DeathState.OutsideRadius)), EnemyListMode.Never);
        vidofnir = SpawnNpc(BNpcBaseId.VidofnirDiver, BNpcNameId.Vidofnir, KnightMaxHp, Facing(state.Outside(DsrP5DeathState.WingsBearing, DsrP5DeathState.OutsideRadius)), EnemyListMode.Never);
        darkscale = SpawnNpc(BNpcBaseId.DarkscaleDiver, BNpcNameId.Darkscale, KnightMaxHp, Facing(state.Outside(DsrP5DeathState.CauterizeBearing, DsrP5DeathState.OutsideRadius)), EnemyListMode.Never);
        heavyImpactCaster = SpawnHelper(new Placement(state.WarriorSpot, 0f));
        for (var i = 0; i < CarrierCount; i++)
            if (SpawnHelper(new Placement(Vector3.Zero, 0f)) is { } carrier) carriers.Add(carrier);
        thordanGazeHelper = SpawnHelper(Facing(state.ThordanSpot));
        eyeHelper = SpawnHelper(Facing(state.EyeSpot));
    }

    private void Deathstorm()
    {
        foreach (var role in state.Dooms)
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            PlayEffect(NextCarrier(), ActionId.Deathstorm, 1.1f, target: member.GameObjectId, at: member.Position);
            world.Events.Add(DoomsMarked - (Death + 10.91f), () => member.AddStatus(StatusId.Doom, DoomSeconds));
        }
    }

    private void CastStaticDives()
    {
        CastLine(zephirin, ActionId.DeathSpearOfTheFury);
        CastLine(vedrfolnir, ActionId.TwistingDive);
        CastLine(darkscale, ActionId.CauterizeDarkscale);
        CastSelf(vidofnir, ActionId.WingsOfSalvation, 5.0f);
        CastSelf(thordan, ActionId.LightningStorm, 5.4f);
    }

    private static void CastLine(SimEnemy? caster, uint actionId) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, 5.7f, false, rotation: caster.Rotation, position: caster.Position);

    // The dives always cross the same lines from the warrior; the twister dragon also drops a twister under everyone.
    private void StaticDives()
    {
        foreach (var (caster, action) in new[] { (zephirin, ActionId.DeathSpearOfTheFury), (vedrfolnir, ActionId.TwistingDive), (darkscale, ActionId.CauterizeDarkscale) })
        {
            if (caster == null) continue;
            PlayEffect(caster, action, 1.1f, caster.Rotation);
            damage.Resolve(caster, action, [DamageType.Lethal], []);
        }
        twisters.Clear();
        foreach (var member in AliveMembers())
            twisters.Add((member.Position, null));
    }

    private void Earthquake(int wave)
    {
        var centre = state.WarriorSpot;
        PlayEffect(heavyImpactCaster, HeavyImpactWaves[wave], 1.1f, at: centre);
        var inner = wave * HeavyImpactRingWidth;
        foreach (var hit in party.Find.InsideRing(centre, inner, inner + HeavyImpactRingWidth).ToList())
            hit.Die(ActionId.HeavyImpact, "stood in the expanding earthquake");
    }

    private static readonly uint[] HeavyImpactWaves =
        [ActionId.HeavyImpact, ActionId.HeavyImpactRing1, ActionId.HeavyImpactRing2, ActionId.HeavyImpactRing3, ActionId.HeavyImpactRing4];

    // Every player without Doom leaves a cleansing puddle where they stand.
    private void DropPuddles()
    {
        state.Puddles.Clear();
        foreach (var role in Enum.GetValues<PartyRole>().Where(r => !state.IsDoomed(r)))
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            var at = member.Position;
            state.Puddles.Add(at);
            NextCarrier()?.NativeCast(ActionId.WingsOfSalvationPuddle, ActionType.Action, 0f, 9.7f, false, position: at);
        }
    }

    private void OpenPuddles()
    {
        foreach (var at in state.Puddles)
        {
            var obj = world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.WingsOfSalvation, Placement = new Placement(at, 0f) });
            if (obj != null)
            {
                eventObjects.Add(obj);
                world.Events.Add(0.65f, () => obj.SetState(1));
            }
            openPuddles.Add((at, obj));
        }
    }

    // A doomed player stepping in a puddle uses it up.
    private void CleanseInPuddles()
    {
        foreach (var (at, obj) in openPuddles.ToList())
        {
            var doomed = AliveMembers().FirstOrDefault(m => m.HasStatus(StatusId.Doom) && FlatDistance(m.Position, at) <= PuddleRadius);
            if (doomed == null) continue;
            doomed.RemoveStatus(StatusId.Doom);
            openPuddles.Remove((at, obj));
            obj?.Despawn();
        }
    }

    private void DoomFalls()
    {
        foreach (var member in AliveMembers().Where(m => m.HasStatus(StatusId.Doom)).ToList())
            member.Die(ActionId.Deathstorm, "Doom was not cleansed (stand in a puddle)");
    }

    private void LightningStorm()
    {
        var members = AliveMembers().ToList();
        foreach (var member in members)
        {
            PlayEffect(NextCarrier(), ActionId.LightningStormHit, 1.1f, target: member.GameObjectId, at: member.Position);
            if (members.Any(other => other != member && FlatDistance(other.Position, member.Position) < LightningStormRadius))
            {
                member.Die(ActionId.LightningStormHit, "overlapped another player's lightning");
                continue;
            }
            damage.ApplyDamage(member, LightningStormDamage, ActionId.LightningStormHit, "spread", lethal: false);
        }
    }

    private void SpawnTwisters()
    {
        for (var i = 0; i < twisters.Count; i++)
        {
            var obj = world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.Twister, Placement = new Placement(twisters[i].At, 0f) });
            if (obj != null) eventObjects.Add(obj);
            twisters[i] = (twisters[i].At, obj);
        }
    }

    private void TouchTwisters()
    {
        foreach (var (at, obj) in twisters.ToList())
        {
            var touched = AliveMembers().Where(m => FlatDistance(m.Position, at) <= TwisterTouchRadius).ToList();
            if (touched.Count == 0) continue;
            PlayEffect(vedrfolnir, ActionId.Twister, 1.1f, at: at);
            foreach (var member in touched) member.Die(ActionId.Twister, "stood on a twister (step off it as the dives land)");
            twisters.Remove((at, obj));
            obj?.Despawn();
        }
    }

    private void ClearTwisters()
    {
        twistersArmed = false;
        foreach (var (_, obj) in twisters) obj?.Despawn();
        twisters.Clear();
    }

    private void SpawnPlaystationKnights()
    {
        charibert = SpawnNpc(BNpcBaseId.Charibert, BNpcNameId.Charibert, KnightMaxHp, Facing(state.Outside(DsrP5DeathState.HeavensflameBearing, DsrP5DeathState.CasterRadius)), EnemyListMode.Never);
        noudenet = SpawnNpc(BNpcBaseId.Noudenet, BNpcNameId.Noudenet, KnightMaxHp, Facing(state.Outside(DsrP5DeathState.MeteorKnightBearing, DsrP5DeathState.CasterRadius)), EnemyListMode.Never);
        grinnaux = SpawnNpc(BNpcBaseId.Grinnaux, BNpcNameId.Grinnaux, KnightMaxHp, new Placement(Vector3.Zero, 0f), EnemyListMode.Never);
        world.Events.Add(0.1f, () => { foreach (var k in new[] { charibert, noudenet, grinnaux }) k?.PlayActionTimeline(TimelineId.WarpEnd); });
    }

    private void MarkSymbols()
    {
        var distances = new Dictionary<PartyRole, float>();
        foreach (var role in Enum.GetValues<PartyRole>())
            if (party.Get(role) is { } member && member.IsAlive())
                distances[role] = FlatDistance(member.Position, Vector3.Zero);
        state.HandOutSymbols(distances);
        foreach (var (role, symbol) in state.Symbols)
            party.Get(role)?.AttachLockonVfx(symbol switch
            {
                ChainSymbol.Circle => LockonId.Circle,
                ChainSymbol.Triangle => LockonId.Triangle,
                ChainSymbol.Square => LockonId.Square,
                _ => LockonId.Cross,
            });
    }

    private void TetherBurningChains()
    {
        burningChains.Clear();
        foreach (var (a, b, _) in state.ChainPairs())
        {
            var first = party.Get(a);
            var second = party.Get(b);
            if (!first.IsAlive() || !second.IsAlive()) continue;
            burningChains.Add(world.Tether(first, second, TetherId.BurningChains, 15f, StatusId.BurningChains));
        }
    }

    private void BreakStretchedChains()
    {
        foreach (var chain in burningChains.Where(c => c.IsActive && c.StretchGt(ChainBreakDistance)).ToList())
        {
            chain.A?.RemoveStatus(StatusId.BurningChains);
            chain.B?.RemoveStatus(StatusId.BurningChains);
            chain.Despawn();
            burningChains.Remove(chain);
        }
    }

    private void FaithUnmoving()
    {
        if (grinnaux == null) return;
        PlayEffect(grinnaux, ActionId.FaithUnmoving, 2.1f);
        if (!KnockbackLookup.TryGet(KnockbackId.FaithUnmoving, out var distance, out var speed)) return;
        foreach (var member in AliveMembers().ToList())
        {
            damage.ApplyDamage(member, FaithUnmovingDamage, ActionId.FaithUnmoving, "Faith Unmoving", false);
            member.Knockback(grinnaux.Position, distance, speed);
        }
    }

    // UNVERIFIED as lethal: the gaze inflicts Hysteria (running about out of control), not modelled.
    private void DragonsGaze()
    {
        PlayEffect(thordanGazeHelper, ActionId.DragonsGazeHit, 1.1f);
        PlayEffect(eyeHelper, ActionId.DragonsGlory, 1.1f);
        damage.ResolveGaze(thordanGazeHelper, lookAway: true);
        damage.ResolveGaze(eyeHelper, lookAway: true);
    }

    private void HolyChain()
    {
        foreach (var chain in burningChains.Where(c => c.IsActive).ToList())
        {
            foreach (var holder in new[] { chain.A, chain.B })
            {
                if (holder == null || !holder.IsAlive()) continue;
                PlayEffect(NextCarrier(), ActionId.HolyChain, 1.1f, target: holder.GameObjectId, at: holder.Position);
                damage.ApplyDamage(holder, HolyChainDamage, ActionId.HolyChain, "Burning Chains not broken", false);
                holder.AddStatus(StatusId.DamageDown, DamageDownSeconds);
                chainBurned.Add(holder);
            }
            chain.Despawn();
        }
        burningChains.Clear();
    }

    private void Heavensflame()
    {
        var members = AliveMembers().ToList();
        var hits = members.ToDictionary(m => m, _ => 0);
        foreach (var source in members)
        {
            PlayEffect(NextCarrier(), ActionId.HeavensflameHit, 1.1f, target: source.GameObjectId, at: source.Position);
            foreach (var hit in party.Find.InsideActionAoe(ActionId.HeavensflameHit, new Placement(source.Position, 0f)))
                if (hits.ContainsKey(hit)) hits[hit]++;
        }
        foreach (var (member, count) in hits)
        {
            if (chainBurned.Contains(member) && member is not ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank })
            {
                damage.ApplyDamage(member, 1f, ActionId.HeavensflameHit, "still low from an unbroken Burning Chain", lethal: true);
                continue;
            }
            if (count > 1)
            {
                damage.ApplyDamage(member, 1f, ActionId.HeavensflameHit, $"overlapped {count} Heavensflames", lethal: true);
                continue;
            }
            damage.ApplyDamage(member, HeavensflameDamage, ActionId.HeavensflameHit, "Heavensflame", false);
            member.AddStatus(StatusId.FireResistanceDownII, FireResistanceDownSeconds);
        }
    }

    private void HolyMeteor()
    {
        PlayEffect(noudenet, ActionId.HolyMeteor, 1.1f);
        for (var i = 0; i < 8; i++)
        {
            var at = AtBearing(45f * i, MeteorRadius);
            if (SpawnNpc(BNpcBaseId.MeteorCircle, BNpcNameId.MeteorCircle, MeteorMaxHp, Facing(at), EnemyListMode.Always) is { } meteor)
                meteors.Add(meteor);
        }
    }

    private void DespawnAll()
    {
        ClearTwisters();
        foreach (var chain in burningChains) chain.Despawn();
        burningChains.Clear();
        foreach (var obj in eventObjects) obj.Despawn();
        eventObjects.Clear();
        foreach (var enemy in spawned.Concat(carriers)) enemy.Despawn();
        foreach (var helper in new[] { heavyImpactCaster, thordanGazeHelper, eyeHelper }) helper?.Despawn();
        spawned.Clear();
        carriers.Clear();
    }

    private SimEnemy? NextCarrier()
    {
        if (carriers.Count == 0) return thordan;
        return carriers[nextCarrier++ % carriers.Count];
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

    private SimEnemy? SpawnHelper(Placement placement) =>
        world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Dummy,
            NameId: BNpcNameId.Thordan,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Visibility: SimEnemy.LegacyVisibility(true, true),
            Placement: placement));

    private static Placement Facing(Vector3 at) => new(at, RotationTowards(at, Vector3.Zero));

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
