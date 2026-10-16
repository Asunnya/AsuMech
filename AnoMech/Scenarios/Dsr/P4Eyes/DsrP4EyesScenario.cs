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

namespace AnoMech.Scenarios.Dsr.P4Eyes;

// The eyes of Nidhogg, from their arrival to Steep in Rage. The eyes' HP is not simulated: the blue
// (west) eye goes down on a fixed time, so Steep in Rage only ever comes from the red one.
public sealed class DsrP4EyesScenario : IScenario
{
    public string Name => "The Eyes";
    public IPhase Phase => DsrZone.Eyes;

    public IReadOnlyList<IScenarioAi> AiStrats => [new DsrP4EyesAi()];

    private readonly DsrP4EyesStateOverrides overrides = new();
    public object SettingsOverrides => overrides;

    private const uint LeftEyeMaxHp = 3979000;
    private const uint RightEyeMaxHp = 4860608;
    private const float RightEyeStartingHealth = 0.5f;
    private const uint AllyMaxHp = 69200;
    public static readonly Vector3 LeftEyeSpot = new(10f, 0f, 0f);
    public static readonly Vector3 RightEyeSpot = new(-10f, 0f, 0f);
    public static readonly Vector3 AlphinaudSpot = new(0f, 0f, 10f);
    private const float EyeDriftSpeed = 2.4f;
    private static readonly Vector3 DiverWaitingSpot = new(23f, 0f, 4f);

    private const float SoulRadius = 5f;
    // UNVERIFIED: how close a red and a blue holder must stand for the tethers to trade.
    public const float PassRange = 1.5f;
    private const float BoundAndDeterminedSeconds = 3f;
    public static readonly Vector3[] GildedSpots = [new(-17f, 0f, 0f), new(17f, 0f, 0f)];
    public static readonly Vector3[] AzureSpots = [new(-10f, 0f, -7f), new(10f, 0f, -7f), new(-10f, 0f, 7f), new(10f, 0f, 7f)];
    private const float OrbBlastRadius = 6f;
    // UNVERIFIED: an orb pops when a player comes within its hitbox plus this.
    public const float OrbTouchMargin = 1f;
    private const float FireResistanceDownSeconds = 11.96f;
    private const float MirageDiveRadius = 4f;
    private const float PiercingResistanceDownSeconds = 12.96f;
    private const float BleedingSeconds = 6f;
    // UNVERIFIED: hits as a share of max HP.
    private const float RaidwideDamage = 0.3f;
    private const float OrbDamage = 0.4f;
    private const float DiveDamage = 0.5f;
    private const float EffectCarrierLinger = 3f;

    public const float OrbSpawnTime = 33.70f;
    public static readonly float[] DiveTimes = [59.87f, 65.00f, 70.13f, 75.22f];

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private DsrP4EyesState state = null!;

    private SimEnemy? leftEye;
    private SimEnemy? rightEye;
    private SimEnemy? estinien;
    private SimEnemy? alphinaud;
    private SimEnemy? ysayle;
    private SimEnemy? haurchefant;
    private readonly List<SimEnemy> extras = [];
    private readonly Queue<SimEnemy> waitingDivers = new();
    private readonly List<Orb> orbs = [];
    private readonly Dictionary<PartyRole, SimTether> boundTethers = [];
    private bool tethersPass;

    private sealed class Orb(SimEnemy enemy, Vector3 at, bool gilded)
    {
        public SimEnemy Enemy { get; } = enemy;
        public Vector3 At { get; } = at;
        public bool Gilded { get; } = gilded;
        public bool Popped { get; set; }
        // UNVERIFIED: the model is scaled by the hitbox radius; how the game sizes it was never seen.
        public float Scale { get; set; }
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new DsrP4EyesState(world.Rng, overrides);
        extras.Clear();
        waitingDivers.Clear();
        orbs.Clear();
        boundTethers.Clear();
        tethersPass = false;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<DsrP4EyesState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.ThordanArenaRadius, "Touched the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(NaurWaymarks));
        world.Events.Add(0f, SpawnWaitingDivers);
        world.Events.Add(0.07f, () => world.Map.DirectorUpdate(ArenaDirector.Layout, 0U, ArenaDirector.EyesLayout));
        world.Events.Add(0.10f, () => world.Map.DirectorUpdate(ArenaDirector.MapChange, ArenaDirector.KnightsMap));
        world.Events.Add(4.85f, SpawnEyesAndEstinien);
        world.Events.Add(5.10f, TetherEyesToEstinien);
        world.Events.Add(5.17f, () => { leftEye?.PlayActionTimeline(TimelineId.EyeAppear); rightEye?.PlayActionTimeline(TimelineId.EyeAppear); estinien?.PlayActionTimeline(TimelineId.EyeAppear); });
        world.Events.Add(5.40f, () => rightEye?.SetHealth(RightEyeMaxHp, RightEyeStartingHealth));
        world.Events.Add(6.00f, SpawnAlphinaud);
        world.Events.Add(6.30f, () => alphinaud?.PlayActionTimeline(TimelineId.AllyAppear));
        world.Events.Add(11.90f, SpawnYsayleAndHaurchefant);
        world.Events.Add(12.36f, MarkSouls);
        world.Events.Add(12.45f, () => { ysayle?.PlayActionTimeline(TimelineId.AllyAppear); haurchefant?.PlayActionTimeline(TimelineId.AllyAppear); });
        world.Events.Add(14.46f, EyesDriftApart);
        world.Events.Add(17.30f, EyesEngage);
        world.Events.Add(17.49f, GrantSouls);
        world.Events.Add(19.30f, () => { ysayle?.FadeOut(); haurchefant?.FadeOut(); });
        world.Events.Add(24.70f, Resentment);

        world.Events.Add(30.02f, () => { CastSelf(leftEye, ActionId.Hatebound, 2.7f); CastSelf(rightEye, ActionId.Hatebound, 2.7f); });
        world.Events.Add(33.02f, () => { PlayEffect(leftEye, ActionId.Hatebound, 1.1f); PlayEffect(rightEye, ActionId.Hatebound, 1.1f); });
        world.Events.Add(OrbSpawnTime, SpawnOrbs);
        world.Events.Add(33.75f, BindEveryone);
        world.Events.Add(OrbSpawnTime + Orbs.GildedBursts, () => BurstUnpopped(gilded: true));
        world.Events.Add(OrbSpawnTime + Orbs.AzureBursts, () => BurstUnpopped(gilded: false));

        world.Events.Add(56.10f, () => { CastSelf(leftEye, ActionId.MirageDiveWindup, 2.7f); CastSelf(rightEye, ActionId.MirageDiveWindup, 2.7f); });
        world.Events.Add(59.10f, () => { PlayEffect(leftEye, ActionId.MirageDiveWindup, 1.1f); PlayEffect(rightEye, ActionId.MirageDiveWindup, 1.1f); });
        for (var i = 0; i < DiveTimes.Length; i++)
        {
            var dive = i;
            world.Events.Add(DiveTimes[i], () => MirageDive(dive));
        }
        world.Events.Add(76.00f, () => rightEye?.Defeat());
        world.Events.Add(79.10f, () => CastSelf(leftEye, ActionId.SteepInRage, 5.7f));
        world.Events.Add(85.05f, SteepInRage);
        world.Events.Add(86.30f, () => leftEye?.Defeat());
        world.Events.Add(89.20f, Unbind);
        world.Events.Add(92.00f, DespawnAll);
    }

    public void Tick(float delta, float elapsed)
    {
        if (tethersPass) PassTethers();
        TouchOrbs(elapsed);
    }

    private void SpawnEyesAndEstinien()
    {
        leftEye = SpawnNpc(BNpcBaseId.LeftEye, BNpcNameId.LeftEye, LeftEyeMaxHp, new Placement(new Vector3(2f, 0f, 0f), -MathF.PI / 4f), EnemyListMode.Always);
        rightEye = SpawnNpc(BNpcBaseId.RightEye, BNpcNameId.RightEye, RightEyeMaxHp, new Placement(new Vector3(-2f, 0f, 0f), MathF.PI / 4f), EnemyListMode.Always);
        estinien = SpawnNpc(BNpcBaseId.Estinien, BNpcNameId.Estinien, AllyMaxHp, new Placement(Vector3.Zero, 0f), EnemyListMode.Never);
    }

    private void TetherEyesToEstinien()
    {
        if (estinien == null) return;
        if (leftEye != null) world.Tether(leftEye, estinien, TetherId.LeftEyeToEstinien);
        if (rightEye != null) world.Tether(rightEye, estinien, TetherId.RightEyeToEstinien);
    }

    private void SpawnAlphinaud() =>
        alphinaud = SpawnNpc(BNpcBaseId.Alphinaud, BNpcNameId.Alphinaud, AllyMaxHp, new Placement(AlphinaudSpot, MathF.PI), EnemyListMode.Never);

    private void SpawnYsayleAndHaurchefant()
    {
        ysayle = SpawnNpc(BNpcBaseId.Ysayle, BNpcNameId.Ysayle, AllyMaxHp, new Placement(new Vector3(3f, 0f, 2f), 0f), EnemyListMode.Never);
        haurchefant = SpawnNpc(BNpcBaseId.Haurchefant, BNpcNameId.Haurchefant, AllyMaxHp, new Placement(new Vector3(-3f, 0f, 2f), 0f), EnemyListMode.Never);
    }

    private void MarkSouls()
    {
        if (party.Get(state.SoulMarked) is { } marked && haurchefant != null)
        {
            marked.AttachLockonVfx(LockonId.SoulOfFriendship);
            world.Tether(haurchefant, marked, TetherId.Soul, duration: 5.2f);
        }
        if (alphinaud != null && ysayle != null)
        {
            alphinaud.AttachLockonVfx(LockonId.SoulOfDevotion);
            world.Tether(ysayle, alphinaud, TetherId.Soul, duration: 5.2f);
        }
    }

    // Only those under both buffs can hurt the eyes, so missing one loses the phase.
    private void GrantSouls()
    {
        var friendshipAt = party.Get(state.SoulMarked) is { } marked && marked.IsAlive() ? marked.Position : AlphinaudSpot;
        PlayEffect(haurchefant, ActionId.SoulOfFriendship, 1.1f, at: friendshipAt);
        PlayEffect(ysayle, ActionId.SoulOfDevotion, 1.1f, at: AlphinaudSpot);
        foreach (var member in AliveMembers().ToList())
        {
            var friendship = FlatDistance(member.Position, friendshipAt) <= SoulRadius;
            var devotion = FlatDistance(member.Position, AlphinaudSpot) <= SoulRadius;
            if (friendship) member.AddStatus(StatusId.SoulOfFriendship, 89.96f);
            if (devotion) member.AddStatus(StatusId.SoulOfDevotion, 89.96f);
            if (!friendship || !devotion)
                member.Die(friendship ? ActionId.SoulOfDevotion : ActionId.SoulOfFriendship, "missed Alphinaud's buffs (stack on him and the marked player)");
        }
    }

    private void EyesDriftApart()
    {
        PlayEffect(leftEye, ActionId.EyesDrift, 0.6f, at: LeftEyeSpot);
        PlayEffect(rightEye, ActionId.EyesDrift, 0.6f, at: RightEyeSpot);
        leftEye?.MoveTo(LeftEyeSpot, EyeDriftSpeed, -MathF.PI / 2f);
        rightEye?.MoveTo(RightEyeSpot, EyeDriftSpeed, MathF.PI / 2f);
    }

    private void EyesEngage()
    {
        leftEye?.SetTargetable(true);
        rightEye?.SetTargetable(true);
    }

    private void Resentment()
    {
        PlayEffect(estinien, ActionId.Resentment, 1.1f);
        foreach (var member in AliveMembers().ToList())
        {
            damage.ApplyDamage(member, RaidwideDamage, ActionId.Resentment, "raidwide", lethal: false);
            member.AddStatus(StatusId.Bleeding, BleedingSeconds);
        }
    }

    private void BindEveryone()
    {
        foreach (var role in Enum.GetValues<PartyRole>())
            if (party.Get(role) is { } member && member.IsAlive())
                Bind(role, member, state.FirstRed.Contains(role));
        tethersPass = true;
    }

    private void Bind(PartyRole role, SimCharacter member, bool red)
    {
        if (boundTethers.Remove(role, out var old)) old.Despawn();
        member.RemoveStatus(red ? StatusId.Fangbound : StatusId.Clawbound);
        member.AddStatus(red ? StatusId.Clawbound : StatusId.Fangbound);
        if (red) state.Red.Add(role);
        else state.Red.Remove(role);
        if ((red ? leftEye : rightEye) is { } eye)
            boundTethers[role] = world.Tether(member, eye, red ? TetherId.Clawbound : TetherId.Fangbound);
    }

    // A red and a blue holder touching trade tethers; neither can trade again for 3s.
    private void PassTethers()
    {
        var members = Enum.GetValues<PartyRole>()
            .Select(r => (Role: r, Member: party.Get(r)))
            .Where(x => x.Member is { } m && m.IsAlive() && boundTethers.ContainsKey(x.Role) && !m.HasStatus(StatusId.BoundAndDetermined))
            .ToList();
        var traded = new HashSet<PartyRole>();
        foreach (var red in members.Where(x => state.HoldsRed(x.Role)))
        {
            var blue = members
                .Where(x => !state.HoldsRed(x.Role) && !traded.Contains(x.Role))
                .Where(x => FlatDistance(x.Member!.Position, red.Member!.Position) <= PassRange)
                .OrderBy(x => FlatDistance(x.Member!.Position, red.Member!.Position))
                .FirstOrDefault();
            if (blue.Member == null) continue;
            traded.Add(red.Role);
            traded.Add(blue.Role);
            Bind(red.Role, red.Member!, red: false);
            Bind(blue.Role, blue.Member, red: true);
            red.Member!.AddStatus(StatusId.BoundAndDetermined, BoundAndDeterminedSeconds);
            blue.Member.AddStatus(StatusId.BoundAndDetermined, BoundAndDeterminedSeconds);
        }
    }

    private void SpawnOrbs()
    {
        foreach (var at in GildedSpots)
            SpawnOrb(BNpcBaseId.GildedPrice, BNpcNameId.GildedPrice, at, gilded: true);
        foreach (var at in AzureSpots)
            SpawnOrb(BNpcBaseId.AzurePrice, BNpcNameId.AzurePrice, at, gilded: false);
    }

    private void SpawnOrb(uint baseId, uint nameId, Vector3 at, bool gilded)
    {
        var enemy = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Placement: new Placement(at, RotationTowards(at, Vector3.Zero))));
        if (enemy == null) return;
        extras.Add(enemy);
        orbs.Add(new Orb(enemy, at, gilded));
    }

    // Anyone touching an orb pops it, red tether or not. The model grows with each hitbox step.
    private void TouchOrbs(float elapsed)
    {
        var age = elapsed - OrbSpawnTime;
        foreach (var orb in orbs.Where(o => !o.Popped).ToList())
        {
            var radius = Orbs.Radius(orb.Gilded, age);
            if (radius != orb.Scale)
            {
                orb.Scale = radius;
                orb.Enemy.SetModelScale(radius);
            }
            var touchers = AliveMembers().Where(m => FlatDistance(m.Position, orb.At) <= radius + OrbTouchMargin).ToList();
            if (touchers.Count > 0) Pop(orb, touchers, Orbs.Ripe(orb.Gilded, age));
        }
    }

    // An orb popped before it has grown blasts whoever touched it for far more than they can take.
    private void Pop(Orb orb, List<SimCharacter> touchers, bool ripe)
    {
        orb.Popped = true;
        var actionId = orb.Gilded ? ActionId.FlareNova : ActionId.FlareStar;
        PlayEffect(orb.Enemy, actionId, 1.1f);
        foreach (var hit in AliveMembers().Where(m => FlatDistance(m.Position, orb.At) <= OrbBlastRadius).ToList())
        {
            if (!ripe && touchers.Contains(hit)) hit.Die(actionId, "popped an orb before it finished growing");
            else Scorch(hit, actionId, "popped an orb");
        }
        world.Events.Add(EffectCarrierLinger, orb.Enemy.FadeOut);
    }

    // An orb left alone bursts over the whole arena.
    private void BurstUnpopped(bool gilded)
    {
        foreach (var orb in orbs.Where(o => o.Gilded == gilded && !o.Popped).ToList())
        {
            orb.Popped = true;
            var actionId = gilded ? ActionId.FlareNovaUnpopped : ActionId.FlareStarUnpopped;
            PlayEffect(orb.Enemy, actionId, 1.1f);
            foreach (var hit in AliveMembers().ToList())
                hit.Die(actionId, "an orb was left to burst (pop it once it has grown)");
            world.Events.Add(EffectCarrierLinger, orb.Enemy.FadeOut);
        }
    }

    private void Scorch(SimCharacter hit, uint actionId, string context)
    {
        if (hit is ISimPartyMember { Role: var role } && !state.HoldsRed(role))
        {
            hit.Die(actionId, "hit by an orb without the red tether");
            return;
        }
        if (hit.HasStatus(StatusId.FireResistanceDownII))
        {
            hit.Die(actionId, "hit by a second orb (Fire Resistance Down)");
            return;
        }
        damage.ApplyDamage(hit, OrbDamage, actionId, context, lethal: false);
        hit.AddStatus(StatusId.FireResistanceDownII, FireResistanceDownSeconds);
    }

    private void SpawnWaitingDivers()
    {
        for (var i = 0; i < DsrP4EyesState.Dives * 2; i++)
        {
            var diver = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.NidhoggDiver,
                NameId: BNpcNameId.Nidhogg,
                Level: Level,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(DiverWaitingSpot, 0f),
                NpcSpawnTemplate: DsrNpcSpawn.Build(BNpcBaseId.NidhoggDiver, BNpcNameId.Nidhogg, 1)));
            if (diver == null) continue;
            diver.SetVisible(false);
            extras.Add(diver);
            waitingDivers.Enqueue(diver);
        }
    }

    // Two of the red holders are dived; a second dive before Piercing Resistance Down wears off kills.
    private void MirageDive(int dive)
    {
        var red = Enum.GetValues<PartyRole>().Where(r => state.HoldsRed(r) && party.Get(r) is { } m && m.IsAlive()).ToList();
        if (red.Count == 0) return;
        var (first, second) = state.DivePicks[dive];
        var picks = new[] { red[first % red.Count], red[second % red.Count] }.Distinct().ToList();
        state.DiveTargets[dive].Clear();
        state.DiveTargets[dive].AddRange(picks);
        var hits = new HashSet<SimCharacter>();
        foreach (var role in picks)
        {
            if (party.Get(role) is not { } target) continue;
            var at = target.Position;
            if (waitingDivers.TryDequeue(out var diver))
            {
                diver.SetPosition(new Placement(at, target.Rotation));
                diver.SetVisible(true);
                PlayEffect(diver, ActionId.MirageDive, 1.1f, target: target.GameObjectId, at: at);
                world.Events.Add(EffectCarrierLinger, () => diver.SetVisible(false));
            }
            foreach (var hit in party.Find.InsideCircle(at, MirageDiveRadius))
                hits.Add(hit);
        }
        foreach (var hit in hits.Where(h => h.IsAlive()).ToList())
        {
            if (hit.HasStatus(StatusId.PiercingResistanceDown))
            {
                hit.Die(ActionId.MirageDive, "dived again with Piercing Resistance Down (pass the red tether after a dive)");
                continue;
            }
            damage.ApplyDamage(hit, DiveDamage, ActionId.MirageDive, "dive", lethal: false);
            hit.AddStatus(StatusId.PiercingResistanceDown, PiercingResistanceDownSeconds);
        }
    }

    // Lethal only while both eyes live; the blue one is always down by now.
    private void SteepInRage()
    {
        PlayEffect(leftEye, ActionId.SteepInRage, 1.1f);
        var bothEyes = rightEye is { Dead: false };
        foreach (var member in AliveMembers().ToList())
        {
            if (bothEyes) member.Die(ActionId.SteepInRage, "both eyes were alive for Steep in Rage");
            else damage.ApplyDamage(member, RaidwideDamage, ActionId.SteepInRage, "raidwide", lethal: false);
        }
    }

    private void Unbind()
    {
        tethersPass = false;
        foreach (var tether in boundTethers.Values) tether.Despawn();
        boundTethers.Clear();
        state.Red.Clear();
        foreach (var member in AliveMembers())
            foreach (var status in new[] { StatusId.Clawbound, StatusId.Fangbound, StatusId.SoulOfFriendship, StatusId.SoulOfDevotion })
                member.RemoveStatus(status);
    }

    private void DespawnAll()
    {
        Unbind();
        foreach (var enemy in extras) enemy.Despawn();
        extras.Clear();
        foreach (var npc in new[] { leftEye, rightEye, estinien, alphinaud, ysayle, haurchefant })
            npc?.Despawn();
    }

    private SimEnemy? SpawnNpc(uint baseId, uint nameId, uint maxHp, Placement placement, EnemyListMode enemyList) =>
        world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            NameId: nameId,
            Level: Level,
            Targetable: false,
            EnemyList: enemyList,
            Placement: placement,
            NpcSpawnTemplate: DsrNpcSpawn.Build(baseId, nameId, maxHp)));

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

// The orbs swell in steps; each step's hitbox radius by seconds since they appeared.
public static class Orbs
{
    public const float GildedBursts = 9.39f;
    public const float AzureBursts = 15.36f;
    // UNVERIFIED: size before the first step.
    private const float Seed = 0.5f;
    private static readonly (float At, float Radius)[] Gilded = [(2.7f, 1.025f), (4.7f, 1.5f), (5.8f, 1.525f), (7.2f, 2f)];
    private static readonly (float At, float Radius)[] Azure = [(5.8f, 1.025f), (7.9f, 1.5f), (11.8f, 1.525f), (13.2f, 2f)];

    // Popping is safe from the third step on; earlier the blast kills whoever touched the orb.
    public static bool Ripe(bool gilded, float age) => age >= (gilded ? Gilded : Azure)[2].At;

    public static float Radius(bool gilded, float age)
    {
        var radius = Seed;
        foreach (var (at, r) in gilded ? Gilded : Azure)
            if (age >= at) radius = r;
        return radius;
    }
}
