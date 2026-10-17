using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P5Wrath;

// Dark Thordan's arrival through Wrath of the Heavens and the tank busters, up to Death of the Heavens.
public sealed class DsrP5WrathScenario : IScenario
{
    public string Name => "Wrath of the Heavens";
    public IPhase Phase => DsrZone.DarkThordan;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP5WrathAi()];

    private readonly DsrP5WrathStateOverrides overrides = new();
    public object SettingsOverrides => overrides;

    public const float Wrath = 29.10f;
    public const float TwistingDiveLands = Wrath + 15.87f;
    public const float MercyLands = Wrath + 22.17f;
    public static readonly float[] EruptionCasts = [Wrath + 22.17f, Wrath + 23.68f, Wrath + 25.20f, Wrath + 26.71f];
    public static readonly float[] LiquidHeavenDrops = [Wrath + 22.21f, Wrath + 23.37f, Wrath + 24.53f, Wrath + 25.69f, Wrath + 26.85f];
    public const float CauterizeCast = Wrath + 23.05f;
    public const float EmptyDimensionLands = Wrath + 29.22f;
    public const float ThunderstruckEnds = Wrath + 29.44f;

    private const uint ThordanMaxHp = 5821796;
    private const uint KnightMaxHp = 6920000;
    private const float AutoAttackHalfAngle = MathF.PI / 4f;
    private const float AutoAttackRange = 10f;
    private const float AutoAttackDamage = 0.3f;
    private const float SpiralPierceHalfWidth = 8f;
    private const float SpiralPierceDamage = 0.5f;
    private const float MagicVulnerabilitySeconds = 11.96f;
    private const float SkywardLeapRadius = 24f;
    private const float SkywardLeapDamage = 0.5f;
    // UNVERIFIED: how close to a twister sets it off.
    public const float TwisterTouchRadius = 1.5f;
    private const float TwisterArms = Wrath + 17.6f;
    private const float TwistersGone = Wrath + 21.8f;
    // UNVERIFIED: the cone's width; players 18.6 degrees apart from Thordan were each hit once.
    private const float MercyConeHalfAngle = 7.5f * MathF.PI / 180f;
    private const float MercyConeRange = 50f;
    private const float MercyDamage = 0.4f;
    private const float EruptionDelay = 3.96f;
    public const float LiquidHeavenRadius = 6f;
    private const float LiquidHeavenDamage = 0.3f;
    private const float EmptyDimensionInnerRadius = 6f;
    public const float ChainLightningRadius = 5f;
    private const float ChainLightningDamage = 0.3f;
    private const float ThunderstruckSeconds = 15f;
    private const float AncientQuagaDamage = 0.8f;
    private const float HeavenlyHeelDamage = 0.6f;
    private const float SlashingResistanceDownSeconds = 15f;
    private const float AscalonsMightDamage = 0.45f;
    private const float KnightLandDelay = 0.35f;
    private const float EffectCarrierLinger = 3f;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private DsrP5WrathState state = null!;

    private SimEnemy? thordan;
    private SimEnemy? vedrfolnir;
    private SimEnemy? darkscale;
    private SimEnemy? vidofnir;
    private SimEnemy? paulecrain;
    private SimEnemy? charibert;
    private SimEnemy? grinnaux;
    private readonly SimEnemy?[] dragoons = new SimEnemy?[2];
    private readonly SimTether?[] spiralTethers = new SimTether?[2];
    private readonly List<SimEnemy> spawned = [];
    private readonly List<SimEventObject> eventObjects = [];
    private readonly List<(Vector3 At, SimEventObject? Obj)> twisters = [];
    private bool twistersArmed;
    private readonly List<SimEnemy> mercyCarriers = [];
    private readonly Queue<SimEnemy> eruptionCarriers = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new DsrP5WrathState(world.Rng, overrides);
        spawned.Clear();
        eventObjects.Clear();
        twisters.Clear();
        twistersArmed = false;
        mercyCarriers.Clear();
        eruptionCarriers.Clear();
        Array.Clear(dragoons);
        Array.Clear(spiralTethers);

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP5WrathState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.ThordanArenaRadius, "Touched the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(NaurWaymarks));
        world.Events.Add(0f, SpawnThordanAndHisDragons);
        world.Events.Add(0.20f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 0U, ArenaDirector.WrathLayout));
        world.Events.Add(0.20f, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.ThordanMap));
        world.Events.Add(4.27f, () => CastSelf(thordan, ActionId.Incarnation, 3.7f));
        world.Events.Add(4.36f, ThordanEngages);
        world.Events.Add(8.26f, () => PlayEffect(thordan, ActionId.Incarnation, 1.1f));
        world.Events.Add(11.41f, () => CastSelf(thordan, ActionId.DragonsEye, 2.7f));
        world.Events.Add(14.20f, DragonsLeave);
        world.Events.Add(14.41f, () => PlayEffect(thordan, ActionId.DragonsEye, 1.1f));
        world.Events.Add(18.55f, AutoAttack);
        world.Events.Add(21.68f, AutoAttack);
        world.Events.Add(25.97f, () => PlayEffect(thordan, ActionId.KnightsOfTheRound, 1.1f));

        world.Events.Add(Wrath, () => CastSelf(thordan, ActionId.WrathOfTheHeavens, 3.7f));
        world.Events.Add(Wrath + 3.98f, () => PlayEffect(thordan, ActionId.WrathOfTheHeavens, 1.1f));
        world.Events.Add(Wrath + 7.07f, () => thordan?.SetTargetable(false));
        world.Events.Add(Wrath + 8.30f, SpawnDragonsAndKnights);
        world.Events.Add(Wrath + 8.60f, () => { foreach (var k in new[] { vedrfolnir, dragoons[0], dragoons[1], paulecrain }) k?.PlayActionTimeline(TimelineId.WarpEnd); });
        world.Events.Add(Wrath + 8.60f, () => { darkscale?.PlayActionTimeline(TimelineId.DragonAppear); vidofnir?.PlayActionTimeline(TimelineId.DragonAppear); });
        world.Events.Add(Wrath + 9.79f, TetherDragoonsAndMarkDefamation);
        world.Events.Add(Wrath + 9.88f, CastTwistingDive);
        world.Events.Add(Wrath + 13.46f, ChainLightning);
        world.Events.Add(Wrath + 14.50f, SpawnMageAndWarrior);
        world.Events.Add(Wrath + 15.00f, () => { charibert?.PlayActionTimeline(TimelineId.WarpEnd); grinnaux?.PlayActionTimeline(TimelineId.WarpEnd); });
        world.Events.Add(TwistingDiveLands, TwistingDive);
        world.Events.Add(Wrath + 15.90f, () => party.Get(state.Divebomb)?.AttachLockonVfx(LockonId.Divebomb));
        world.Events.Add(Wrath + 15.96f, SpiralPierce);
        world.Events.Add(Wrath + 16.00f, SkywardLeap);
        world.Events.Add(Wrath + 16.60f, SpawnTwisters);
        world.Events.Add(Wrath + 17.20f, () => { foreach (var (_, obj) in twisters) obj?.SetState(1); });
        world.Events.Add(TwisterArms, () => twistersArmed = true);
        world.Events.Add(Wrath + 18.10f, () => CastSelf(thordan, ActionId.AscalonsMercyRevealed, 3.0f));
        world.Events.Add(Wrath + 18.10f, SpawnEffectCarriers);
        world.Events.Add(Wrath + 18.50f, () => { foreach (var k in new[] { dragoons[0], dragoons[1], paulecrain }) k?.FadeOut(); });
        world.Events.Add(TwistersGone, ClearTwisters);
        world.Events.Add(MercyLands, MercyCones);
        world.Events.Add(Wrath + 22.17f, () => CastSelf(charibert, ActionId.AltarFlare, 3.2f));
        foreach (var at in EruptionCasts) world.Events.Add(at, CastEruption);
        for (var i = 0; i < LiquidHeavenDrops.Length; i++) world.Events.Add(LiquidHeavenDrops[i], DropLiquidHeaven);
        world.Events.Add(CauterizeCast, CastCauterize);
        world.Events.Add(Wrath + 24.26f, () => CastWithoutOmen(grinnaux, ActionId.EmptyDimension, 4.7f));
        world.Events.Add(Wrath + 29.04f, Cauterize);
        world.Events.Add(EmptyDimensionLands, EmptyDimension);
        world.Events.Add(ThunderstruckEnds, ChainLightningBursts);
        world.Events.Add(Wrath + 30.25f, () => thordan?.SetTargetable(true));
        world.Events.Add(Wrath + 30.33f, () => CastSelf(thordan, ActionId.AncientQuaga, 5.7f));
        world.Events.Add(Wrath + 33.00f, () => { foreach (var k in new[] { charibert, grinnaux, darkscale, vidofnir, vedrfolnir }) k?.FadeOut(); });
        world.Events.Add(Wrath + 36.32f, () => Raidwide(ActionId.AncientQuaga, AncientQuagaDamage));
        world.Events.Add(Wrath + 40.69f, AutoAttack);
        world.Events.Add(Wrath + 42.74f, CastHeavenlyHeel);
        world.Events.Add(Wrath + 46.71f, ResolveHeavenlyHeel);
        foreach (var might in new[] { Wrath + 49.84f, Wrath + 51.48f, Wrath + 53.14f })
            world.Events.Add(might, AscalonsMight);
        world.Events.Add(Wrath + 57.10f, () => PlayEffect(thordan, ActionId.KnightsOfTheRound, 1.1f));
        world.Events.Add(Wrath + 59.00f, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        if (twistersArmed) TouchTwisters();
    }

    private void SpawnThordanAndHisDragons()
    {
        thordan = SpawnNpc(BNpcBaseId.DarkThordan, BNpcNameId.Thordan, ThordanMaxHp, new Placement(Vector3.Zero, 0f), EnemyListMode.Always);
        darkscale = SpawnNpc(BNpcBaseId.Darkscale, BNpcNameId.Darkscale, KnightMaxHp, new Placement(new Vector3(-15f, 0f, 0f), MathF.PI / 2f), EnemyListMode.Never);
        vidofnir = SpawnNpc(BNpcBaseId.Vidofnir, BNpcNameId.Vidofnir, KnightMaxHp, new Placement(new Vector3(11f, 0f, 0f), -MathF.PI / 2f), EnemyListMode.Never);
        vedrfolnir = SpawnNpc(BNpcBaseId.Vedrfolnir, BNpcNameId.Vedrfolnir, KnightMaxHp, new Placement(new Vector3(0f, 0f, 14f), MathF.PI), EnemyListMode.Never);
    }

    private void ThordanEngages()
    {
        thordan?.SetTargetable(true);
        thordan?.SetTarget(party.Get(PartyRole.MainTank), follow: false);
        if (thordan == null) return;
        foreach (var dragon in new[] { darkscale, vidofnir, vedrfolnir })
            if (dragon != null) world.Tether(dragon, thordan, TetherId.DragonToThordan, duration: 9.8f);
    }

    private void DragonsLeave()
    {
        foreach (var dragon in new[] { darkscale, vidofnir, vedrfolnir }) dragon?.FadeOut();
        darkscale = vidofnir = vedrfolnir = null;
    }

    private void AutoAttack()
    {
        if (thordan == null || party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        var rotation = RotationTowards(thordan.Position, tank.Position);
        PlayEffect(thordan, ActionId.ThordanAttack, 0.6f, rotation, tank.GameObjectId);
        foreach (var hit in party.Find.InsideCone(new Placement(thordan.Position, rotation), AutoAttackHalfAngle, AutoAttackRange).ToList())
            damage.ApplyDamage(hit, AutoAttackDamage, ActionId.ThordanAttack, "auto attack", lethal: false);
    }

    private void SpawnDragonsAndKnights()
    {
        vedrfolnir = SpawnNpc(BNpcBaseId.Vedrfolnir, BNpcNameId.Vedrfolnir, KnightMaxHp, Facing(state.DragonSpot), EnemyListMode.Never);
        dragoons[0] = SpawnNpc(BNpcBaseId.Ignasse, BNpcNameId.Ignasse, KnightMaxHp, Facing(state.DragoonSpot(0)), EnemyListMode.Never);
        dragoons[1] = SpawnNpc(BNpcBaseId.Vellguine, BNpcNameId.Vellguine, KnightMaxHp, Facing(state.DragoonSpot(1)), EnemyListMode.Never);
        paulecrain = SpawnNpc(BNpcBaseId.Paulecrain, BNpcNameId.Paulecrain, KnightMaxHp, Facing(AtBearing(state.DragonBearing + 180f, DsrP5WrathState.KnightRadius)), EnemyListMode.Never);
        darkscale = SpawnNpc(BNpcBaseId.DarkscaleDiver, BNpcNameId.Darkscale, KnightMaxHp, Facing(AtBearing(state.DragonBearing + 90f, DsrP5WrathState.KnightRadius)), EnemyListMode.Never);
        vidofnir = SpawnNpc(BNpcBaseId.VidofnirDiver, BNpcNameId.Vidofnir, KnightMaxHp, Facing(AtBearing(state.DragonBearing - 90f, DsrP5WrathState.KnightRadius)), EnemyListMode.Never);
    }

    private void TetherDragoonsAndMarkDefamation()
    {
        for (var i = 0; i < dragoons.Length; i++)
            if (dragoons[i] is { } dragoon && party.Get(state.Tethered[i]) is { } target && target.IsAlive())
                spiralTethers[i] = world.Tether(dragoon, target, TetherId.SpiralPierce);
        party.Get(state.Defamation)?.AttachLockonVfx(LockonId.WrathDefamation);
    }

    private void CastTwistingDive()
    {
        if (vedrfolnir == null) return;
        vedrfolnir.NativeCast(ActionId.TwistingDive, ActionType.Action, 0f, 5.7f, false, rotation: vedrfolnir.Rotation, position: vedrfolnir.Position);
    }

    // Twisters spawn under everyone as the dive lands; the dive itself cuts a line through the centre.
    private void TwistingDive()
    {
        if (vedrfolnir != null)
        {
            PlayEffect(vedrfolnir, ActionId.TwistingDive, 1.1f, vedrfolnir.Rotation);
            damage.Resolve(vedrfolnir, ActionId.TwistingDive, [DamageType.Lethal], []);
        }
        twisters.Clear();
        foreach (var member in AliveMembers())
            twisters.Add((member.Position, null));
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
            foreach (var member in touched) member.Die(ActionId.Twister, "stood on a twister (step off it as the dive lands)");
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

    private void ChainLightning()
    {
        foreach (var role in state.Lightning)
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            PlayEffect(darkscale, ActionId.ChainLightning, 1.1f, target: member.GameObjectId, at: member.Position);
            member.AddStatus(StatusId.Thunderstruck, ThunderstruckSeconds);
        }
    }

    private void SpawnMageAndWarrior()
    {
        charibert = SpawnNpc(BNpcBaseId.Charibert, BNpcNameId.Charibert, KnightMaxHp, Facing(state.MageSpot), EnemyListMode.Never);
        grinnaux = SpawnNpc(BNpcBaseId.Grinnaux, BNpcNameId.Grinnaux, KnightMaxHp, Facing(state.WarriorSpot), EnemyListMode.Never);
    }

    // Each dragoon charges its tethered player; anyone else in the way dies.
    private void SpiralPierce()
    {
        for (var i = 0; i < dragoons.Length; i++)
        {
            spiralTethers[i]?.Despawn();
            if (dragoons[i] is not { } dragoon || party.Get(state.Tethered[i]) is not { } target || !target.IsAlive()) continue;
            var from = dragoon.Position;
            var at = target.Position;
            var rotation = RotationTowards(from, at);
            PlayEffect(dragoon, ActionId.SpiralPierce, 1.1f, rotation, target.GameObjectId, at);
            world.Events.Add(KnightLandDelay, () => dragoon.SetPosition(new Placement(at, rotation)));
            foreach (var hit in party.Find.InsideRect(new Placement(from, rotation), SpiralPierceHalfWidth, FlatDistance(from, at)).Where(m => m.IsAlive()).ToList())
            {
                if (hit != target)
                {
                    hit.Die(ActionId.SpiralPierce, "stood in a dragoon's charge");
                    continue;
                }
                damage.ApplyDamage(hit, SpiralPierceDamage, ActionId.SpiralPierce, "dragoon charge", lethal: false);
                hit.AddStatus(StatusId.MagicVulnerabilityUp, MagicVulnerabilitySeconds);
            }
        }
    }

    // One leap is survivable; on top of a dragoon's Magic Vulnerability it kills.
    private void SkywardLeap()
    {
        if (paulecrain == null || party.Get(state.Defamation) is not { } target || !target.IsAlive()) return;
        var at = target.Position;
        PlayEffect(paulecrain, ActionId.WrathSkywardLeap, 1.1f, target: target.GameObjectId, at: at);
        world.Events.Add(KnightLandDelay, () => paulecrain?.SetPosition(new Placement(at, RotationTowards(at, Vector3.Zero))));
        foreach (var hit in party.Find.InsideCircle(at, SkywardLeapRadius).Where(m => m.IsAlive()).ToList())
        {
            if (hit != target && hit.HasStatus(StatusId.MagicVulnerabilityUp))
            {
                hit.Die(ActionId.WrathSkywardLeap, "hit by the defamation with Magic Vulnerability Up");
                continue;
            }
            damage.ApplyDamage(hit, SkywardLeapDamage, ActionId.WrathSkywardLeap, "defamation", lethal: false);
            if (hit == target) continue;
            hit.AddStatus(StatusId.MagicVulnerabilityUp, 2.96f);
            hit.AddStatus(StatusId.PhysicalVulnerabilityUp, 5.96f);
        }
    }

    // A helper spawned in the frame it acts drops the effect, so the cones' and eruptions' carriers
    // arrive a few seconds early.
    private void SpawnEffectCarriers()
    {
        if (thordan == null) return;
        for (var i = 0; i < 8; i++)
            if (SpawnHelper(new Placement(thordan.Position, 0f)) is { } helper) mercyCarriers.Add(helper);
        for (var i = 0; i < EruptionCasts.Length; i++)
            if (SpawnHelper(new Placement(Vector3.Zero, 0f)) is { } helper) eruptionCarriers.Enqueue(helper);
    }

    // One cone from Thordan at every player; standing in a second one kills.
    private void MercyCones()
    {
        if (thordan == null) return;
        var targets = AliveMembers().ToList();
        var hits = new Dictionary<SimCharacter, int>();
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var rotation = RotationTowards(thordan.Position, target.Position);
            if (i < mercyCarriers.Count)
            {
                var helper = mercyCarriers[i];
                helper.SetRotation(rotation);
                PlayEffect(helper, ActionId.AscalonsMercyRevealedCone, 1.1f, rotation);
                world.Events.Add(EffectCarrierLinger, helper.Despawn);
            }
            foreach (var hit in party.Find.InsideCone(new Placement(thordan.Position, rotation), MercyConeHalfAngle, MercyConeRange))
                hits[hit] = hits.GetValueOrDefault(hit) + 1;
        }
        foreach (var (hit, count) in hits.Where(kv => kv.Key.IsAlive()).ToList())
        {
            if (count > 1) hit.Die(ActionId.AscalonsMercyRevealedCone, "overlapped Ascalon's Mercy Revealed cones (spread)");
            else damage.ApplyDamage(hit, MercyDamage, ActionId.AscalonsMercyRevealedCone, "protean", lethal: false);
        }
    }

    // Each eruption lands where its target stood as it began.
    private void CastEruption()
    {
        if (party.Get(state.Eruptions) is not { } target || !target.IsAlive()) return;
        var at = target.Position;
        if (!eruptionCarriers.TryDequeue(out var helper)) return;
        helper.SetPosition(new Placement(at, 0f));
        helper.NativeCast(ActionId.AltarFlareEruption, ActionType.Action, 0f, 3.7f, false, position: at);
        world.Events.Add(EruptionDelay, () =>
        {
            PlayEffect(helper, ActionId.AltarFlareEruption, 1.1f, at: at);
            damage.Resolve(helper, ActionId.AltarFlareEruption, [DamageType.Lethal], []);
            world.Events.Add(EffectCarrierLinger, helper.Despawn);
        });
    }

    // The drops hurt everyone in reach without killing; UNVERIFIED: the puddle left behind is not modelled.
    private void DropLiquidHeaven()
    {
        if (party.Get(state.LiquidHeaven) is not { } target || !target.IsAlive()) return;
        var at = target.Position;
        PlayEffect(vedrfolnir, ActionId.LiquidHeaven, 1.1f, target: target.GameObjectId, at: at);
        foreach (var hit in party.Find.InsideCircle(at, LiquidHeavenRadius).Where(m => m.IsAlive()).ToList())
            damage.ApplyDamage(hit, LiquidHeavenDamage, ActionId.LiquidHeaven, "Liquid Heaven", lethal: false);
        world.Events.Add(0.8f, () =>
        {
            if (world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.LiquidHeaven, Placement = new Placement(at, 0f) }) is not { } puddle) return;
            eventObjects.Add(puddle);
            world.Events.Add(0.7f, () => puddle.SetState(1));
            world.Events.Add(10.0f, () => puddle.SetState(0));
            world.Events.Add(12.1f, puddle.Despawn);
        });
    }

    // Both dragons dive through where the divebomb stood as the casts began.
    private void CastCauterize()
    {
        var aim = party.Get(state.Divebomb) is { } marked && marked.IsAlive() ? marked.Position : state.MageSpot;
        foreach (var (dragon, action) in new[] { (darkscale, ActionId.CauterizeDarkscale), (vidofnir, ActionId.CauterizeVidofnir) })
        {
            if (dragon == null) continue;
            var rotation = RotationTowards(dragon.Position, aim);
            dragon.SetRotation(rotation);
            dragon.NativeCast(action, ActionType.Action, 0f, 5.7f, false, rotation: rotation, position: dragon.Position);
        }
    }

    private void Cauterize()
    {
        foreach (var (dragon, action) in new[] { (darkscale, ActionId.CauterizeDarkscale), (vidofnir, ActionId.CauterizeVidofnir) })
        {
            if (dragon == null) continue;
            PlayEffect(dragon, action, 1.1f, dragon.Rotation);
            damage.Resolve(dragon, action, [DamageType.Lethal], []);
        }
    }

    private void EmptyDimension()
    {
        if (grinnaux == null) return;
        PlayEffect(grinnaux, ActionId.EmptyDimension, 1.1f);
        damage.Resolve(grinnaux, ActionId.EmptyDimension, [DamageType.Lethal], [], size: EmptyDimensionInnerRadius);
    }

    // Each Thunderstruck player bursts as the debuff ends; caught in two bursts kills.
    private void ChainLightningBursts()
    {
        var hits = new Dictionary<SimCharacter, int>();
        foreach (var role in state.Lightning)
        {
            if (party.Get(role) is not { } struck || !struck.IsAlive()) continue;
            PlayEffect(darkscale ?? thordan, ActionId.ChainLightningHit, 1.1f, target: struck.GameObjectId, at: struck.Position);
            foreach (var hit in party.Find.InsideCircle(struck.Position, ChainLightningRadius))
                hits[hit] = hits.GetValueOrDefault(hit) + 1;
        }
        foreach (var (hit, count) in hits.Where(kv => kv.Key.IsAlive()).ToList())
        {
            if (count > 1) hit.Die(ActionId.ChainLightningHit, "caught in two Chain Lightning bursts");
            else damage.ApplyDamage(hit, ChainLightningDamage, ActionId.ChainLightningHit, "Chain Lightning", lethal: false);
        }
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

    private void AscalonsMight()
    {
        if (thordan == null || party.Get(PartyRole.MainTank) is not { } tank || !tank.IsAlive()) return;
        var rotation = RotationTowards(thordan.Position, tank.Position);
        PlayEffect(thordan, ActionId.AscalonsMight, 1.1f, rotation, tank.GameObjectId);
        var hits = damage.Resolve(IPositioned.From(new Placement(thordan.Position, rotation)), ActionId.AscalonsMight, [DamageType.TankBuster], []);
        foreach (var hit in hits.Where(h => h.IsAlive()))
            damage.ApplyDamage(hit, AscalonsMightDamage, ActionId.AscalonsMight, "tank buster", lethal: false);
    }

    private void Raidwide(uint actionId, float fraction)
    {
        PlayEffect(thordan, actionId, 1.1f);
        foreach (var member in AliveMembers().ToList())
            damage.ApplyDamage(member, fraction, actionId, "raidwide", lethal: false);
    }

    private void DespawnAll()
    {
        ClearTwisters();
        foreach (var tether in spiralTethers) tether?.Despawn();
        foreach (var obj in eventObjects) obj.Despawn();
        eventObjects.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
        foreach (var helper in mercyCarriers.Concat(eruptionCarriers)) helper.Despawn();
        mercyCarriers.Clear();
        eruptionCarriers.Clear();
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

    // Empty Dimension shows no ground omen in game.
    private static void CastWithoutOmen(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.NativeCast(actionId, ActionType.Action, castSeconds + 1f, castSeconds, false, targetId: caster.GameObjectId);

    // An effect without a position plays at the arena centre, so default it to the caster.
    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);

    private static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    public static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));
}
