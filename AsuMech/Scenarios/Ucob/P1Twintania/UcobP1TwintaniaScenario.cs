using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static AsuMech.Scenarios.Ucob.UcobConstants;
using State = AsuMech.Scenarios.Ucob.P1Twintania.UcobP1TwintaniaState;

namespace AsuMech.Scenarios.Ucob.P1Twintania;

// Twintania from the pull until she flies off. Her HP pushes run on a fixed clock at a group's
// real pace, so every mechanic lands at the same time each run; the enrage is never reached.
public sealed class UcobP1TwintaniaScenario : IScenario
{
    public string Name => "Twintania";
    public IPhase Phase => UcobZone.P1;

    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobP1TwintaniaAi()];

    private readonly UcobP1TwintaniaSettingsWindow settingsWindow = new();
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;

    public static readonly Vector2 Spawn = new(0f, -15f);
    public static readonly Vector2 NorthCamp = new(0.1f, -8.5f);
    public static readonly Vector2 FirstWestCamp = new(-6.7f, -3.5f);
    public static readonly Vector2 SecondWestCamp = new(-7.7f, 5.9f);
    public static readonly Vector2 EastCamp = new(8.8f, 5.7f);
    public const float ToFirstWestCamp = 30.6f;
    public const float ToSecondWestCamp = 54.03f;
    public const float ToEastCamp = 76.51f;

    public static readonly float[] PlummetHits = [6.47f, 112.05f, 127.46f];
    public static readonly float[] TwisterCasts = [10.63f, 68.02f, 120.31f];
    public static readonly float[] TwisterSnapshots = [12.64f, 69.98f, 122.27f];
    public const float TwisterArms = UcobTwisters.Arms;
    public const float TwisterFades = UcobTwisters.Fades;
    public static readonly float[] FireballMarkers = [10.55f, 92.54f];
    public static readonly float[] FireballHits = [15.85f, 99.77f];
    public static readonly float[] DeathSentenceCasts = [19.96f, 55.72f, 104.90f];
    private const float DeathSentenceCastSeconds = 3.7f;
    // The other tank provokes after each Death Sentence; the main tank holds her first.
    public static readonly float[] AggroSwaps = [24.93f, 61.54f, 111.87f];
    public static readonly float[] NeurolinkDrops = [27.04f, 72.18f, 142.48f];
    public static readonly float[][] LiquidHellDrops =
    [
        [35.38f, 36.54f, 37.71f, 38.87f, 40.03f],
        [47.59f, 48.75f, 49.96f, 51.21f, 52.51f],
        [80.56f, 81.72f, 82.88f, 84.04f, 85.21f],
        [92.76f, 93.92f, 95.09f, 96.25f, 97.41f],
        [129.70f, 130.86f, 132.02f, 133.19f, 134.35f],
    ];
    // The fourth set chases one rolled player; the others take whoever is furthest from her.
    public const int TargetedLiquidHellSet = 3;
    public const float PuddleAppears = 0.9f;
    private const float PuddleFades = 11.7f;
    private const float PuddleGone = 12.7f;
    private const float BurnsSeconds = 15f;
    private const float BurnsKill = 3f; // UNVERIFIED
    public static readonly float[] GenerateCasts = [41.24f, 63.86f, 86.42f, 116.16f, 135.56f];
    private const float HatchMarkerLead = 0.09f;
    private const float GenerateCastSeconds = 2.7f;
    public const float OviformChaseAfterCast = 1.25f;
    public const float OviformSpeed = 5f;
    // Follow stops at the hitbox edge.
    private const float OviformContactSlack = 0.3f;
    private const float ManaHypersensitivitySeconds = 15.96f;
    public const float FliesOff = 141.50f;
    public const float End = 144f;

    private static readonly (float At, float Fraction)[] Health =
    [
        (0f, 1f), (12.59f, 0.886f), (44.23f, 0.621f), (66.86f, 0.473f), (69.98f, 0.451f),
        (89.41f, 0.322f), (119.14f, 0.122f), (122.27f, 0.109f), (138.55f, 0.015f), (FliesOff, 0f),
    ];

    private const float EffectCarrierLinger = 3f;
    private const int HelperPoolSize = 8;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private State state = null!;
    private SimEnemy? twintania;
    private readonly List<SimEnemy> spawned = [];
    private readonly Queue<SimEnemy> helperPool = [];
    private readonly List<SimEventObject> eobjs = [];
    private readonly UcobNeurolinks neurolinks = new();
    private readonly UcobTwisters twisters = new();
    private readonly List<Puddle> puddles = [];
    private readonly List<Oviform> oviforms = [];

    private sealed record Puddle(Vector2 At, float FadesAt);

    private sealed class Oviform(SimEnemy enemy, SimCharacter target)
    {
        public SimEnemy Enemy { get; } = enemy;
        public SimCharacter Target { get; } = target;
        public bool Chasing { get; set; }
        public bool Hatched { get; set; }
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new State(world.Rng, settingsWindow.Overrides);
        spawned.Clear();
        helperPool.Clear();
        eobjs.Clear();
        neurolinks.Clear();
        twisters.Clear();
        puddles.Clear();
        oviforms.Clear();
        twintania = null;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnTwintania);
        world.Events.Add(0f, FillHelperPool);
        world.Events.Add(0.3f, () => twintania?.MoveTo(ToWorld(NorthCamp), 6f, MathF.PI));
        world.Events.Add(3.21f, () => twintania?.AddStatus(StatusId.TwintaniaNeurolink));

        foreach (var at in PlummetHits) world.Events.Add(at, Plummet);
        for (var i = 0; i < TwisterCasts.Length; i++)
        {
            var set = i;
            world.Events.Add(TwisterCasts[i], () => CastSelf(twintania, ActionId.Twister, 1.7f));
            world.Events.Add(TwisterSnapshots[i] - 0.05f, () => PlayEffect(twintania, ActionId.Twister, 1.1f));
            world.Events.Add(TwisterSnapshots[i], () => DropTwisters(set));
        }
        for (var i = 0; i < FireballHits.Length; i++)
        {
            var target = state.FireballTargets[i];
            world.Events.Add(FireballMarkers[i], () => party.Get(target)?.AttachLockonVfx(LockonId.Fireball));
            world.Events.Add(FireballHits[i], () => Fireball(target));
        }
        foreach (var at in DeathSentenceCasts)
        {
            world.Events.Add(at, CastDeathSentence);
            world.Events.Add(at + DeathSentenceCastSeconds - 0.02f, DeathSentence);
        }
        foreach (var at in NeurolinkDrops) world.Events.Add(at, DropNeurolink);
        world.Events.Add(ToFirstWestCamp, () => twintania?.MoveTo(ToWorld(FirstWestCamp), 4.3f));
        world.Events.Add(ToSecondWestCamp, () => twintania?.MoveTo(ToWorld(SecondWestCamp), 7f));
        world.Events.Add(ToEastCamp, () => twintania?.MoveTo(ToWorld(EastCamp), 6.6f));

        for (var set = 0; set < LiquidHellDrops.Length; set++)
        {
            var targeted = set == TargetedLiquidHellSet;
            foreach (var at in LiquidHellDrops[set]) world.Events.Add(at, () => LiquidHell(targeted));
        }

        for (var i = 0; i < GenerateCasts.Length; i++)
        {
            var set = i;
            world.Events.Add(GenerateCasts[i] - HatchMarkerLead, () => Generate(set));
            world.Events.Add(GenerateCasts[i] + GenerateCastSeconds + OviformChaseAfterCast, () => ChaseHatchTargets(set));
        }

        world.Events.Add(FliesOff, FlyOff);
        world.Events.Add(End, DespawnAll);
    }

    public static PartyRole AggroTank(float at) => AggroSwaps.Count(s => s <= at) % 2 == 0 ? PartyRole.MainTank : PartyRole.OffTank;

    public void Tick(float delta, float elapsed)
    {
        var now = world.Events.Elapsed;
        if (twintania is { } boss)
        {
            if (!boss.IsCasting && !boss.IsMoving && party.Get(AggroTank(now)) is { } tank && tank.IsAlive()) boss.Face(tank);
            boss.SetHealth(0, HealthAt(now));
        }
        neurolinks.Tick(party);
        twisters.Tick(world, BlastTwister);
        BurnInPuddles(now);
        MoveOviforms();
    }

    private static float HealthAt(float t)
    {
        for (var i = 1; i < Health.Length; i++)
        {
            if (t > Health[i].At) continue;
            var (a, b) = (Health[i - 1], Health[i]);
            return a.Fraction + (b.Fraction - a.Fraction) * (t - a.At) / (b.At - a.At);
        }
        return 0f;
    }

    private void SpawnTwintania()
    {
        twintania = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Twintania,
            NameId: BNpcNameId.Twintania,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(ToWorld(Spawn), 0f)));
        if (twintania != null) spawned.Add(twintania);
    }

    private void Plummet()
    {
        if (twintania is not { } boss || party.Get(AggroTank(world.Events.Elapsed)) is not { } tank || !tank.IsAlive()) return;
        var facing = RotationTowards(boss.Position, tank.Position);
        boss.SetRotation(facing);
        PlayEffect(boss, ActionId.Plummet, 1.1f, facing, tank.GameObjectId);
        var forward = new Vector2(MathF.Sin(facing), MathF.Cos(facing));
        var range = State.PlummetRange + boss.HitboxRadius;
        foreach (var hit in AliveMembers().ToList())
        {
            var to = Flat(hit.Position) - Flat(boss.Position);
            if (to.Length() > range || (to.Length() > 0.01f && Vector2.Dot(Vector2.Normalize(to), forward) < MathF.Cos(State.PlummetHalfAngle))) continue;
            if (IsTank(hit)) damage.ApplyDamage(hit, 0.25f, ActionId.Plummet, "Plummet", lethal: false);
            else hit.Die(ActionId.Plummet, "stood in front of Twintania for Plummet (stay behind her)");
        }
    }

    private void DropTwisters(int set)
    {
        foreach (var role in state.TwisterTargets[set])
            if (party.Get(role) is { } member && member.IsAlive())
                twisters.Drop(world, Flat(member.Position));
    }

    private void BlastTwister(Vector2 at, SimCharacter toucher)
    {
        PlayFromHelper(ActionId.TwisterBlast, ToWorld(at));
        foreach (var hit in AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), at) <= State.TwisterBlastRadius).ToList())
            hit.Die(ActionId.TwisterBlast, ReferenceEquals(hit, toucher)
                ? "touched a Twister (move off your spot as the cast ends)"
                : "launched into the death wall by a Twister someone touched");
    }

    private void Fireball(PartyRole role)
    {
        if (twintania is not { } boss || party.Get(role) is not { } target || !target.IsAlive()) return;
        PlayEffect(boss, ActionId.Fireball, 1.1f, target: target.GameObjectId, at: target.Position);
        var hits = AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), Flat(target.Position)) <= State.FireballRadius).ToList();
        foreach (var hit in hits)
        {
            if (hits.Count < State.FireballMinimumStack) hit.Die(ActionId.Fireball, "Fireball wasn't shared (stack on the red marker)");
            else damage.ApplyDamage(hit, 0.5f, ActionId.Fireball, "Fireball", lethal: false);
        }
    }

    private void CastDeathSentence()
    {
        if (party.Get(AggroTank(world.Events.Elapsed)) is not { } tank) return;
        twintania?.NativeCast(ActionId.DeathSentence, ActionType.Action, 0f, DeathSentenceCastSeconds, false, targetId: tank.GameObjectId);
    }

    private void DeathSentence()
    {
        if (party.Get(AggroTank(world.Events.Elapsed)) is not { } tank || !tank.IsAlive()) return;
        PlayEffect(twintania, ActionId.DeathSentence, 1.1f, target: tank.GameObjectId);
        if (tank.HasStatus(StatusId.SlashingResistanceDownII))
        {
            tank.Die(ActionId.DeathSentence, "took Death Sentence with Slashing Resistance Down II (tanks swap after each one)");
            return;
        }
        damage.ApplyDamage(tank, 0.6f, ActionId.DeathSentence, "Death Sentence", lethal: false);
        tank.AddStatus(StatusId.SlashingResistanceDownII, 34f);
    }

    private void DropNeurolink()
    {
        if (twintania is not { } boss) return;
        neurolinks.Drop(world, Flat(boss.Position));
    }

    private bool InNeurolink(SimCharacter member) => neurolinks.Contains(member);

    private void LiquidHell(bool targeted)
    {
        if (twintania is not { } boss) return;
        var target = targeted && party.Get(state.LiquidHellTarget) is { } chosen && chosen.IsAlive()
            ? chosen
            : AliveMembers().OrderByDescending(m => Vector3.DistanceSquared(m.Position, boss.Position)).FirstOrDefault();
        if (target == null) return;
        var at = Flat(target.Position);
        PlayEffect(boss, ActionId.LiquidHell, 1.1f, target: target.GameObjectId, at: target.Position);
        damage.ApplyDamage(target, 0.05f, ActionId.LiquidHell, "Liquid Hell", lethal: false);
        world.Events.Add(PuddleAppears, () =>
        {
            var puddle = new Puddle(at, world.Events.Elapsed + PuddleFades);
            puddles.Add(puddle);
            var eobj = SpawnEObj(EObjId.LiquidHell, at);
            world.Events.Add(PuddleFades, () =>
            {
                puddles.Remove(puddle);
                eobj?.FadeOut();
            });
            world.Events.Add(PuddleGone, () => eobj?.SetState(0));
        });
    }

    private void BurnInPuddles(float now)
    {
        foreach (var member in AliveMembers().Where(m => !m.HasStatus(StatusId.Burns)).ToList())
        {
            if (!puddles.Any(p => now < p.FadesAt && Vector2.Distance(Flat(member.Position), p.At) <= State.LiquidHellPuddleRadius)) continue;
            member.AddStatus(StatusId.Burns, BurnsSeconds);
            world.Events.Add(BurnsKill, () =>
            {
                if (member.IsAlive() && member.HasStatus(StatusId.Burns))
                    member.Die(ActionId.LiquidHell, "stood in a Liquid Hell puddle (its Burns kill; keep the puddles away from the party)");
            });
        }
    }

    private void Generate(int set)
    {
        if (twintania is not { } boss) return;
        CastSelf(boss, ActionId.Generate, GenerateCastSeconds);
        foreach (var role in state.HatchTargets[set])
        {
            if (party.Get(role) is not { } target || !target.IsAlive()) continue;
            target.AttachLockonVfx(LockonId.Hatch);
            var enemy = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Oviform,
                NameId: BNpcNameId.Oviform,
                Level: Level,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                Placement: new Placement(boss.Position, boss.Rotation)));
            if (enemy == null) continue;
            spawned.Add(enemy);
            oviforms.Add(new Oviform(enemy, target));
        }
    }

    private void ChaseHatchTargets(int set)
    {
        foreach (var oviform in oviforms.Where(o => !o.Chasing && !o.Hatched))
        {
            oviform.Chasing = true;
            oviform.Enemy.Follow(oviform.Target, OviformSpeed);
        }
    }

    private void MoveOviforms()
    {
        foreach (var oviform in oviforms.Where(o => o.Chasing && !o.Hatched).ToList())
        {
            if (!oviform.Target.IsAlive())
            {
                oviform.Hatched = true;
                oviform.Enemy.Despawn();
                continue;
            }
            var contact = oviform.Enemy.HitboxRadius + oviform.Target.HitboxRadius + OviformContactSlack;
            if (Vector2.Distance(Flat(oviform.Enemy.Position), Flat(oviform.Target.Position)) <= contact) Hatch(oviform);
        }
    }

    private void Hatch(Oviform oviform)
    {
        oviform.Hatched = true;
        var enemy = oviform.Enemy;
        var target = oviform.Target;
        enemy.Follow(null);
        if (!InNeurolink(target))
        {
            PlayEffect(enemy, ActionId.DeepHatch, 1.1f);
            foreach (var member in AliveMembers().ToList())
                member.Die(ActionId.DeepHatch, ReferenceEquals(member, target)
                    ? "the Hatch reached you outside a Neurolink (run into one before it arrives)"
                    : "Deep Hatch: the marked player wasn't in a Neurolink");
        }
        else
        {
            PlayEffect(enemy, ActionId.Hatch, 1.1f, target: target.GameObjectId, at: target.Position);
            foreach (var hit in AliveMembers().Where(m => Vector2.Distance(Flat(m.Position), Flat(target.Position)) <= State.HatchRadius).ToList())
            {
                if (hit.HasStatus(StatusId.ManaHypersensitivity))
                    hit.Die(ActionId.Hatch, "took a second Hatch with Mana Hypersensitivity (Hatches go to separate Neurolinks)");
                else if (!InNeurolink(hit))
                    hit.Die(ActionId.Hatch, "caught in a Hatch outside a Neurolink (keep 8y away from the marked player)");
                else
                {
                    damage.ApplyDamage(hit, 0.1f, ActionId.Hatch, "Hatch", lethal: false);
                    hit.AddStatus(StatusId.ManaHypersensitivity, ManaHypersensitivitySeconds);
                }
            }
        }
        world.Events.Add(0.65f, enemy.Despawn);
    }

    private void FlyOff()
    {
        if (twintania is not { } boss) return;
        boss.SetTargetable(false);
        boss.RemoveStatus(StatusId.TwintaniaNeurolink);
        foreach (var oviform in oviforms.Where(o => !o.Hatched))
        {
            oviform.Hatched = true;
            oviform.Enemy.Despawn();
        }
        world.Events.Add(1.5f, () => boss.SetVisible(false));
    }

    private void DespawnAll()
    {
        foreach (var eobj in eobjs) eobj.Despawn();
        eobjs.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
        helperPool.Clear();
        twisters.Clear();
        puddles.Clear();
        oviforms.Clear();
        neurolinks.Clear();
    }

    private SimEventObject? SpawnEObj(uint eobjId, Vector2 at)
    {
        var eobj = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = eobjId,
            Placement = new Placement(ToWorld(at), 0f),
            TimelineState = 1,
        });
        if (eobj != null) eobjs.Add(eobj);
        return eobj;
    }

    // A helper spawned in the frame it acts drops the effect, so they wait in a pool.
    private void FillHelperPool()
    {
        for (var i = 0; i < HelperPoolSize; i++)
            if (SpawnHelper(new Placement(Vector3.Zero, 0f)) is { } helper) helperPool.Enqueue(helper);
    }

    private SimEnemy? SpawnHelper(Placement placement)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Dummy,
            NameId: BNpcNameId.Twintania,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            Visibility: SimEnemy.LegacyVisibility(true, true),
            Placement: placement));
        if (helper != null) spawned.Add(helper);
        return helper;
    }

    private void PlayFromHelper(uint actionId, Vector3 at)
    {
        var helper = helperPool.TryDequeue(out var pooled) ? pooled : SpawnHelper(new Placement(at, 0f));
        if (helper == null) return;
        helper.SetPosition(new Placement(at, 0f));
        PlayEffect(helper, actionId, 1.1f, at: at);
        world.Events.Add(EffectCarrierLinger, () => helperPool.Enqueue(helper));
    }

    private IEnumerable<SimCharacter> AliveMembers()
    {
        for (var slot = 0; slot < 8; slot++)
            if (party.Get(slot) is { } member && member.IsAlive())
                yield return member;
    }

    private static bool IsTank(SimCharacter member) => member is ISimPartyMember { Role: PartyRole.MainTank or PartyRole.OffTank };

    private static Vector3 ToWorld(Vector2 at) => new(at.X, 0f, at.Y);

    private static Vector2 Flat(Vector3 at) => new(at.X, at.Z);

    private static void CastSelf(SimEnemy? caster, uint actionId, float castSeconds) =>
        caster?.NativeCast(actionId, ActionType.Action, 0f, castSeconds, false, targetId: caster.GameObjectId);

    // An effect without a position plays at the arena centre, so default it to the caster.
    private static void PlayEffect(SimEnemy? caster, uint actionId, float animationLock, float? rotation = null, GameObjectId? target = null, Vector3? at = null) =>
        caster?.NativeActionEffect(actionId, animationLock, (ushort)actionId, 0, ActionType.Action, 0,
            rotation: rotation, position: at ?? caster.Position, animationTargetId: target ?? caster.GameObjectId);

    private static float RotationTowards(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);
}
