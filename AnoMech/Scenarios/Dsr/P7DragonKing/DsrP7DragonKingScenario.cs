using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static AnoMech.Scenarios.Dsr.DsrConstants;

namespace AnoMech.Scenarios.Dsr.P7DragonKing;

// The Dragon-king from Alternative End to Morn Afah's Edge: three cycles of Exaflare's, Akh Morn's
// and Gigaflare's Edge, each followed by two Trinities, the third cycle without Gigaflare. He stays
// in the middle facing the tank at the top of enmity, and each Edge is laid out around the way he
// faces as its cast begins. His HP is not simulated. The run ends as Morn Afah's Edge's first towers land: they kill whoever soaks
// them, and the enrage wipes whatever happens.
public sealed class DsrP7DragonKingScenario : IScenario
{
    public string Name => "Dragon-king Thordan";
    public IPhase Phase => DsrZone.DragonKing;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP7DragonKingAi()];

    private readonly DsrP7DragonKingSettingsWindow settingsWindow = new();
    public object SettingsOverrides => settingsWindow.Overrides;
    public void DrawSettings() => settingsWindow.Draw();

    // Joins just before the boss turns targetable; the Alternative End raidwide before it is skipped.
    public const float StartAt = 34f;
    public const float Targetable = 35.95f;
    public static readonly float[] ExaflareCasts = [40.23f, 119.10f, 199.05f];
    public static readonly float[] ExaflareFirstHits = [47.12f, 125.96f, 205.91f];
    public static readonly float[] AkhMornCasts = [61.41f, 140.30f, 220.22f];
    public static readonly float[] AkhMornFirstHits = [68.10f, 146.98f, 226.90f];
    public static readonly int[] AkhMornFollowUps = [4, 5, 6];
    public const float AkhMornSecondHitDelay = 2.18f;
    public const float AkhMornHitInterval = 1.07f;
    public static readonly float[] GigaflareCasts = [85.86f, 165.81f];
    public static readonly float[][] GigaflareHits = [[94.82f, 98.83f, 102.84f], [174.81f, 178.77f, 182.78f]];
    // UNVERIFIED: retail starts all three casts together; the telegraphs come up one by one so the
    // order, and with it the way around, reads before the first blast.
    public const float GigaflareOmenStagger = 1f;
    private static readonly float[] GigaflareCastSeconds = [8.7f, 12.7f, 16.7f];
    private static readonly uint[] GigaflareActions = [ActionId.GigaflaresEdgeFirst, ActionId.GigaflaresEdgeSecond, ActionId.GigaflaresEdgeThird];
    // Exaflare, Akh Morn and Gigaflare of each cycle, in the order of DsrP7DragonKingState.Elements.
    public static readonly float[] AscalonResolves = [47.34f, 68.18f, 95.00f, 126.23f, 147.07f, 174.94f, 206.17f, 226.99f];
    private static readonly float[] EdgeCasts = [40.23f, 61.41f, 85.86f, 119.10f, 140.30f, 165.81f, 199.05f, 220.22f];
    private const float GlowAfterCast = 0.93f;
    public static readonly float[] TrinityHits =
        [55.40f, 59.45f, 79.85f, 83.91f, 113.09f, 117.14f, 134.29f, 138.34f, 159.80f, 163.85f, 193.03f, 197.09f, 214.25f, 218.26f, 240.90f, 244.95f];
    private const float TrinitySwingLead = 1.07f;
    private const float ExaflareCastSeconds = 5.7f;
    private const float AkhMornCastSeconds = 5.7f;
    private const float GigaflaresEdgeCastSeconds = 7.7f;
    private const float MornAfahsEdgeCastSeconds = 9.7f;
    // UNVERIFIED: what a lone tank needs to live through each later Akh Morn hit without an invuln.
    public const float KitchenSinkMitigation = 0.5f;
    private const float FacingCheckSeconds = 0.1f;
    private const float FacingTolerance = 0.03f;
    public const float MornAfahsEdgeCast = 246.91f;
    public const float MornAfahsEdgeFirstTowers = 257.60f;
    public const float End = MornAfahsEdgeFirstTowers - 0.1f;

    private const uint ThordanMaxHp = 12178508;
    private static readonly Vector3 ThordanHome = Vector3.Zero;
    private const float AscalonRadius = 8f;
    private const float AkhMornRadius = 4f;
    // UNVERIFIED: Gigaflare's Edge falls off with distance; everyone stood 19.8y or more from it and
    // lived, and Ice leaves only a sliver inside the Dragon-king's ring.
    public const float GigaflareLethalWithin = 18f;
    private const float TrinityRadius = 3f;
    private const float TrinityResistanceSeconds = 37f;
    private const float TrinityVulnSeconds = 60f;
    // UNVERIFIED: a third stack of the same Trinity resistance down kills; the tanks swap before it.
    private const int TrinityLethalStacks = 2;
    private const float EffectCarrierLinger = 3f;
    private const int HelperPoolSize = 32;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private DsrP7DragonKingState state = null!;
    private SimEnemy? thordan;
    private PartyRole topEnmity;
    // He can't turn from an Edge's cast until it resolves: his swings play the hits around his facing.
    private bool facingHeld;
    private readonly List<SimEnemy> spawned = [];
    private readonly Queue<SimEnemy> helperPool = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new DsrP7DragonKingState(world.Rng, settingsWindow.Overrides);
        spawned.Clear();
        helperPool.Clear();
        thordan = null;
        topEnmity = DsrP7DragonKingState.TopEnmityTank(0);
        facingHeld = false;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP7DragonKingState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(StartAt, () => world.EnforceArenaBoundary(new CircleArena(Geometry.ThordanArenaRadius), "Touched the arena wall"));
        world.Events.Add(StartAt, () => world.PlaceWaymarks(NaurWaymarks));
        world.Events.Add(StartAt, SpawnThordan);
        world.Events.Add(StartAt, FillHelperPool);
        world.Events.Add(StartAt, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.ThordanMap));
        world.Events.Add(Targetable, () => thordan?.SetTargetable(true));
        world.Events.Add(Targetable, FaceTheTopOfEnmity);
        world.Events.Add(35.99f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 1U, ArenaDirector.DragonKingLayout));

        for (var i = 0; i < EdgeCasts.Length; i++)
        {
            var mechanic = i;
            world.Events.Add(EdgeCasts[i] + GlowAfterCast, () => Glow(state.Elements[mechanic]));
            world.Events.Add(AscalonResolves[i], () => Ascalon(state.Elements[mechanic]));
        }

        for (var cycle = 0; cycle < DsrP7DragonKingState.Cycles; cycle++)
        {
            var c = cycle;
            world.Events.Add(ExaflareCasts[c], () => CastExaflares(c));
            world.Events.Add(ExaflareFirstHits[c] - 0.93f, () => PlayEffect(thordan, ActionId.ExaflaresEdge, 1.1f));
            foreach (var spot in Enum.GetValues<EdgeSpot>())
            {
                var explosions = state.ExaflareExplosions(c, spot).ToList();
                for (var k = 0; k < explosions.Count; k++)
                {
                    var (s, index) = (spot, k);
                    world.Events.Add(ExaflareFirstHits[c] + explosions[k].Delay, () => Exaflare(c, s, index));
                }
            }

            world.Events.Add(AkhMornCasts[c], () => CastAkhMorn(c));
            world.Events.Add(AkhMornFirstHits[c] - 0.72f, () => PlayEffect(thordan, ActionId.AkhMornsEdge, 1.1f));
            world.Events.Add(AkhMornFirstHits[c], () => AkhMorn(c, 0));
            for (var hit = 1; hit <= AkhMornFollowUps[c]; hit++)
            {
                var h = hit;
                var at = AkhMornHitAt(c, h);
                world.Events.Add(at - 0.8f, () => PlayEffect(thordan, h == AkhMornFollowUps[c] ? ActionId.AkhMornsEdgeLastSwing : ActionId.AkhMornsEdgeSwing, 1.1f));
                world.Events.Add(at, () => AkhMorn(c, h));
            }

            if (c >= DsrP7DragonKingState.GigaflareCycles) continue;
            world.Events.Add(GigaflareCasts[c], () => CastGigaflares(c));
            world.Events.Add(GigaflareHits[c][0] - 0.98f, () => PlayEffect(thordan, ActionId.GigaflaresEdge, 1.1f));
            for (var blast = 0; blast < 3; blast++)
            {
                var b = blast;
                world.Events.Add(GigaflareHits[c][b], () => Gigaflare(c, b));
            }
        }

        for (var i = 0; i < TrinityHits.Length; i++)
        {
            var darkTank = DsrP7DragonKingState.TopEnmityTank(i / 2);
            world.Events.Add(TrinityHits[i] - TrinitySwingLead, () => PlayEffect(thordan, ActionId.Trinity, 1.1f));
            world.Events.Add(TrinityHits[i], () => Trinity(darkTank));
        }

        world.Events.Add(MornAfahsEdgeCast, CastMornAfahsEdge);
        world.Events.Add(End, DespawnAll);
        world.Events.KeepWindow(StartAt, End);
    }

    public static float AkhMornHitAt(int cycle, int hit)
        => hit == 0 ? AkhMornFirstHits[cycle] : AkhMornFirstHits[cycle] + AkhMornSecondHitDelay + (hit - 1) * AkhMornHitInterval;

    public static float AkhMornLastHit(int cycle) => AkhMornHitAt(cycle, AkhMornFollowUps[cycle]);

    private void SpawnThordan()
    {
        thordan = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.DragonKingThordan,
            NameId: BNpcNameId.DragonKingThordan,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(ThordanHome, MathF.PI),
            NpcSpawnTemplate: DsrNpcSpawn.Build(BNpcBaseId.DragonKingThordan, BNpcNameId.DragonKingThordan, ThordanMaxHp)));
        if (thordan != null) spawned.Add(thordan);
    }

    private void SwapEnmity(PartyRole tank)
    {
        topEnmity = tank;
        thordan?.SetTarget(party.Get(tank), follow: false);
    }

    private void FaceTheTopOfEnmity()
    {
        if (thordan == null) return;
        if (!facingHeld && party.Get(topEnmity) is { } tank && tank.IsAlive())
        {
            var want = RotationTowards(thordan.Position, tank.Position);
            if (MathF.Abs(MathF.IEEERemainder(want - thordan.Rotation, MathF.Tau)) > FacingTolerance) thordan.SetRotation(want);
        }
        world.Events.Add(FacingCheckSeconds, FaceTheTopOfEnmity);
    }

    // The Edge's spots are fixed by the way he faces as the cast begins. The cast locks his facing,
    // so that is when the tanks trade enmity for the Trinities that follow it.
    private void CastEdge(uint actionId, float castSeconds, float resolvedAt, int nextTrinityPair)
    {
        if (thordan == null) return;
        state.Facing = thordan.Rotation;
        facingHeld = true;
        world.Events.Add(resolvedAt - world.Events.Elapsed, () => facingHeld = false);
        SwapEnmity(DsrP7DragonKingState.TopEnmityTank(nextTrinityPair));
        CastSelf(thordan, actionId, castSeconds);
    }

    private void Glow(AscalonElement element)
        => thordan?.AddStatusParam(StatusId.AscalonGlow, element == AscalonElement.Flames ? StatusId.FlamesGlow : StatusId.IceGlow);

    private void Ascalon(AscalonElement element)
    {
        thordan?.RemoveStatus(StatusId.AscalonGlow);
        var flames = element == AscalonElement.Flames;
        PlayFromHelper(flames ? ActionId.FlamesOfAscalon : ActionId.IceOfAscalon, ThordanHome);
        var hits = flames
            ? party.Find.InsideCircle(ThordanHome, AscalonRadius)
            : party.Find.OutsideCircle(ThordanHome, AscalonRadius);
        foreach (var hit in hits.Where(m => m.IsAlive()).ToList())
            hit.Die(flames ? ActionId.FlamesOfAscalon : ActionId.IceOfAscalon,
                flames ? "stood near the Dragon-king for Flames of Ascalon (red sword: get out)" : "stood away from the Dragon-king for Ice of Ascalon (blue sword: get in)");
    }

    private void CastExaflares(int cycle)
    {
        CastEdge(ActionId.ExaflaresEdge, ExaflareCastSeconds, ExaflareFirstHits[cycle] + DsrP7DragonKingState.ExaflareDelays[^1], 3 * cycle);
        foreach (var spot in Enum.GetValues<EdgeSpot>())
        {
            var at = ToWorld(state.TurnedSpot(spot));
            var rotation = state.ExaflareRotation(cycle, spot);
            if (TakeHelper(new Placement(at, rotation)) is not { } helper) continue;
            helper.NativeCast(ActionId.ExaflaresEdgeFirst, ActionType.Action, 0f, 6.6f, false, rotation: rotation, position: at);
            world.Events.Add(ExaflareFirstHits[cycle] - ExaflareCasts[cycle] + EffectCarrierLinger, () => ReturnHelper(helper));
        }
    }

    private void Exaflare(int cycle, EdgeSpot spot, int index)
    {
        var explosion = state.ExaflareExplosions(cycle, spot).ElementAt(index);
        var action = explosion.Delay == 0f ? ActionId.ExaflaresEdgeFirst : ActionId.ExaflaresEdgeStep;
        var at = ToWorld(explosion.At);
        PlayFromHelper(action, at, state.ExaflareRotation(cycle, spot));
        foreach (var hit in party.Find.InsideCircle(at, DsrP7DragonKingState.ExaflareRadius).Where(m => m.IsAlive()).ToList())
            hit.Die(action, "stood in an Exaflare's Edge explosion");
    }

    private readonly Dictionary<EdgeSpot, SimEnemy> akhMornCasters = [];

    private void CastAkhMorn(int cycle)
    {
        CastEdge(ActionId.AkhMornsEdge, AkhMornCastSeconds, AkhMornLastHit(cycle), 3 * cycle + 1);
        foreach (var spot in Enum.GetValues<EdgeSpot>())
        {
            var at = ToWorld(state.TurnedSpot(spot));
            if (TakeHelper(new Placement(at, 0f)) is not { } helper) continue;
            helper.NativeCast(AkhMornFirstAction(spot), ActionType.Action, 0f, 6.4f, false, targetId: helper.GameObjectId, position: at);
            akhMornCasters[spot] = helper;
        }
        world.Events.Add(AkhMornLastHit(cycle) - AkhMornCasts[cycle] + EffectCarrierLinger, () =>
        {
            foreach (var helper in akhMornCasters.Values) ReturnHelper(helper);
            akhMornCasters.Clear();
        });
    }

    private static uint AkhMornFirstAction(EdgeSpot spot) => spot switch
    {
        EdgeSpot.Party => ActionId.AkhMornsEdgePartyFirst,
        EdgeSpot.SecondParty => ActionId.AkhMornsEdgeSecondPartyFirst,
        _ => ActionId.AkhMornsEdgeTanksFirst,
    };

    // UNVERIFIED: what an unsoaked first hit or a lone non-tank costs; the logs only show a lone
    // tank living through the first hit, and through the rest only on an invuln. A spot left empty
    // after the first hit costs nothing: a tank leaving its spot for the six cleared.
    private void AkhMorn(int cycle, int hit)
    {
        foreach (var spot in Enum.GetValues<EdgeSpot>())
        {
            var at = ToWorld(state.TurnedSpot(spot));
            var action = hit == 0 ? AkhMornFirstAction(spot) : spot == EdgeSpot.Tanks ? ActionId.AkhMornsEdgeTanks : ActionId.AkhMornsEdgeParty;
            if (akhMornCasters.TryGetValue(spot, out var helper)) PlayEffect(helper, action, 1.1f, at: at);
            var hits = party.Find.InsideCircle(at, AkhMornRadius).Where(m => m.IsAlive()).ToList();
            if (hit == 0 && hits.Count == 0)
            {
                foreach (var member in AliveMembers().ToList())
                    member.Die(action, "nobody took an Akh Morn's Edge spot");
                return;
            }
            if (hits.Count != 1) continue;
            var lone = hits[0];
            if (!IsTank(lone)) lone.Die(action, "took Akh Morn's Edge alone (stack with your group)");
            else if (hit > 0 && !damage.Survives(lone, KitchenSinkMitigation))
                damage.ApplyDamage(lone, 1f, action, "held Akh Morn's Edge alone without an invuln or enough mitigation (kitchen sink)", lethal: true);
        }
    }

    private readonly List<SimEnemy> gigaflareCasters = [];

    private void CastGigaflares(int cycle)
    {
        CastEdge(ActionId.GigaflaresEdge, GigaflaresEdgeCastSeconds, GigaflareHits[cycle][^1], 3 * cycle + 2);
        for (var blast = 0; blast < 3; blast++)
        {
            var at = ToWorld(state.GigaflareSpot(cycle, blast));
            if (TakeHelper(new Placement(at, 0f)) is not { } helper) continue;
            var omenDelay = blast * GigaflareOmenStagger;
            helper.NativeCast(GigaflareActions[blast], ActionType.Action, omenDelay, GigaflareCastSeconds[blast], false, targetId: helper.GameObjectId, position: at);
            gigaflareCasters.Add(helper);
        }
    }

    private void Gigaflare(int cycle, int blast)
    {
        var at = ToWorld(state.GigaflareSpot(cycle, blast));
        if (blast < gigaflareCasters.Count) PlayEffect(gigaflareCasters[blast], GigaflareActions[blast], 1.1f, at: at);
        foreach (var hit in party.Find.InsideCircle(at, GigaflareLethalWithin).Where(m => m.IsAlive()).ToList())
            hit.Die(GigaflareActions[blast], "too close to Gigaflare's Edge (it falls off with distance; go to the far side)");
        if (blast < 2) return;
        var used = gigaflareCasters.ToList();
        gigaflareCasters.Clear();
        world.Events.Add(EffectCarrierLinger, () => { foreach (var helper in used) ReturnHelper(helper); });
    }

    // The top two in enmity take the tank hits; the tanks trade them every pair of Trinities.
    private void Trinity(PartyRole darkTank)
    {
        var lightTank = darkTank == PartyRole.MainTank ? PartyRole.OffTank : PartyRole.MainTank;
        var nearest = party.Find.Closest(ThordanHome);
        foreach (var (target, action) in new[]
                 {
                     (party.Get(darkTank), ActionId.TrinityFirstEnmity),
                     (party.Get(lightTank), ActionId.TrinitySecondEnmity),
                     (nearest, ActionId.TrinityNearest),
                 })
        {
            if (target is not { } t || !t.IsAlive()) continue;
            PlayFromHelper(action, t.Position, RotationTowards(ThordanHome, t.Position), t.GameObjectId);
            foreach (var hit in party.Find.InsideCircle(t.Position, TrinityRadius).Where(m => m.IsAlive()).ToList())
                TrinityHit(hit, action);
        }
    }

    private void TrinityHit(SimCharacter hit, uint action)
    {
        var dark = action != ActionId.TrinitySecondEnmity;
        var light = action != ActionId.TrinityFirstEnmity;
        if (action == ActionId.TrinityNearest && hit.HasStatus(StatusId.PhysicalVulnerabilityUp))
        {
            hit.Die(action, "nearest to the Dragon-king for Trinity twice in a row (let the next pair take it)");
            return;
        }
        if ((dark && Stacks(hit, StatusId.DarkResistanceDown) >= TrinityLethalStacks) || (light && Stacks(hit, StatusId.TrinityLightResistanceDown) >= TrinityLethalStacks))
        {
            hit.Die(action, "took a third Trinity of the same element (tanks swap each pair)");
            return;
        }
        if (dark) hit.AddStatus(StatusId.DarkResistanceDown, TrinityResistanceSeconds);
        if (light) hit.AddStatus(StatusId.TrinityLightResistanceDown, TrinityResistanceSeconds);
        if (action == ActionId.TrinityNearest) hit.AddStatus(StatusId.PhysicalVulnerabilityUp, TrinityVulnSeconds);
    }

    private static int Stacks(SimCharacter member, ushort statusId) => member.FindStatus(statusId)?.Stacks ?? 0;

    private void CastMornAfahsEdge()
    {
        CastEdge(ActionId.MornAfahsEdge, MornAfahsEdgeCastSeconds, End, TrinityHits.Length / 2);
        foreach (var (spot, action) in new[]
                 {
                     (EdgeSpot.Party, ActionId.MornAfahsEdgePartyFirst),
                     (EdgeSpot.SecondParty, ActionId.MornAfahsEdgeSecondPartyFirst),
                     (EdgeSpot.Tanks, ActionId.MornAfahsEdgeTanksFirst),
                 })
        {
            var at = ToWorld(state.TurnedSpot(spot));
            if (TakeHelper(new Placement(at, 0f)) is { } helper)
                helper.NativeCast(action, ActionType.Action, 0f, 10.4f, false, targetId: helper.GameObjectId, position: at);
        }
    }

    private void DespawnAll()
    {
        foreach (var helper in helperPool.Concat(akhMornCasters.Values).Concat(gigaflareCasters)) helper.Despawn();
        helperPool.Clear();
        akhMornCasters.Clear();
        gigaflareCasters.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
        thordan = null;
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

    private SimEnemy? SpawnHelper(Placement placement)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Dummy,
            NameId: BNpcNameId.DragonKingThordan,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Visibility: SimEnemy.LegacyVisibility(true, true),
            Placement: placement));
        if (helper != null) spawned.Add(helper);
        return helper;
    }

    // One effect per helper per frame: the client shows only one when an actor plays several.
    private void PlayFromHelper(uint actionId, Vector3 at, float rotation = 0f, GameObjectId? target = null)
    {
        if (TakeHelper(new Placement(at, rotation)) is not { } helper) return;
        PlayEffect(helper, actionId, 1.1f, rotation, target, at);
        world.Events.Add(EffectCarrierLinger, () => ReturnHelper(helper));
    }

    private bool IsTank(SimCharacter member) => member is ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank };

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    private static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    private static void CastSelf(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, targetId: caster.GameObjectId);

    // An effect without a position plays at the arena centre, so default it to the caster.
    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);

    private static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);
}
