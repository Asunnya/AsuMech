using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Scenarios.Legacy;
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
    public static readonly float[] FireballMarkers = [10.55f, 92.54f];
    public static readonly float[] FireballHits = [15.85f, 99.77f];
    public static readonly float[] DeathSentenceCasts = [19.96f, 55.72f, 104.90f];
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
    public static readonly float[] GenerateCasts = [41.24f, 63.86f, 86.42f, 116.16f, 135.56f];
    public const float FliesOff = 141.50f;
    public const float End = 144f;

    private static readonly (float At, float Fraction)[] Health =
    [
        (0f, 1f), (12.59f, 0.886f), (44.23f, 0.621f), (66.86f, 0.473f), (69.98f, 0.451f),
        (89.41f, 0.322f), (119.14f, 0.122f), (122.27f, 0.109f), (138.55f, 0.015f), (FliesOff, 0f),
    ];

    private const int HelperPoolSize = 8;

    private SimWorld world = null!;
    private SimParty party = null!;
    private DamageSolver damage = null!;
    private State state = null!;
    private SimEnemy? twintania;
    private readonly List<SimEnemy> spawned = [];
    private UcobHelperPool helpers = null!;
    private UcobNeurolinks neurolinks = null!;
    private UcobTwisters twisters = null!;
    private UcobLiquidHell liquidHell = null!;
    private UcobHatches hatches = null!;

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        damage = new DamageSolver(party);
        state = new State(world.Rng, settingsWindow.Overrides);
        spawned.Clear();
        helpers = new UcobHelperPool(world, BNpcNameId.Twintania, spawned);
        neurolinks = new UcobNeurolinks(world);
        twisters = new UcobTwisters(world, helpers);
        liquidHell = new UcobLiquidHell(world, damage);
        hatches = new UcobHatches(world, damage, neurolinks, spawned);
        twintania = null;

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<State>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(Geometry.P1ArenaRadius, "Fell into the death wall"));
        world.Events.Add(0f, () => world.PlaceWaymarks(AetherWaymarks));
        world.Events.Add(0f, SpawnTwintania);
        world.Events.Add(0f, () => helpers.Fill(HelperPoolSize));
        world.Events.Add(0.3f, () => twintania?.MoveTo(UcobSim.ToWorld(NorthCamp), 6f, MathF.PI));
        world.Events.Add(3.21f, () => twintania?.AddStatus(StatusId.TwintaniaNeurolink));

        foreach (var at in PlummetHits) world.Events.Add(at, Plummet);
        for (var i = 0; i < TwisterCasts.Length; i++)
        {
            var targets = state.TwisterTargets[i];
            world.Events.Add(TwisterCasts[i], () => twisters.Cast(twintania));
            world.Events.Add(TwisterSnapshots[i], () => twisters.Drop(twintania, targets));
        }
        for (var i = 0; i < FireballHits.Length; i++)
        {
            var target = state.FireballTargets[i];
            world.Events.Add(FireballMarkers[i], () => party.Get(target)?.AttachLockonVfx(LockonId.Fireball));
            world.Events.Add(FireballHits[i], () => Fireball(target));
        }
        foreach (var at in DeathSentenceCasts)
        {
            world.Events.Add(at, () => UcobTwintaniaAttacks.CastDeathSentence(twintania, party.Get(AggroTank(world.Events.Elapsed))));
            world.Events.Add(at + UcobTwintaniaAttacks.DeathSentenceCastSeconds - 0.02f,
                () => UcobTwintaniaAttacks.DeathSentence(damage, twintania, party.Get(AggroTank(world.Events.Elapsed))));
        }
        foreach (var at in NeurolinkDrops) world.Events.Add(at, () => { if (twintania != null) neurolinks.Drop(UcobSim.Flat(twintania.Position)); });
        world.Events.Add(ToFirstWestCamp, () => twintania?.MoveTo(UcobSim.ToWorld(FirstWestCamp), 4.3f));
        world.Events.Add(ToSecondWestCamp, () => twintania?.MoveTo(UcobSim.ToWorld(SecondWestCamp), 7f));
        world.Events.Add(ToEastCamp, () => twintania?.MoveTo(UcobSim.ToWorld(EastCamp), 6.6f));

        for (var set = 0; set < LiquidHellDrops.Length; set++)
        {
            var targeted = set == TargetedLiquidHellSet;
            foreach (var at in LiquidHellDrops[set])
                world.Events.Add(at, () => liquidHell.Drop(twintania, targeted ? party.Get(state.LiquidHellTarget) : null));
        }

        for (var i = 0; i < GenerateCasts.Length; i++)
        {
            var targets = state.HatchTargets[i];
            world.Events.Add(GenerateCasts[i] - UcobHatches.MarkerLead, () => hatches.Generate(twintania, targets));
            world.Events.Add(GenerateCasts[i] + UcobHatches.CastSeconds + UcobHatches.ChaseAfterCast, hatches.Chase);
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
            boss.SetHealth(0, UcobSim.Interpolate(Health, now));
        }
        neurolinks.Tick();
        twisters.Tick(now);
        liquidHell.Tick(now);
        hatches.Tick();
    }

    private void SpawnTwintania()
    {
        twintania = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Twintania,
            NameId: BNpcNameId.Twintania,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(UcobSim.ToWorld(Spawn), 0f)));
        if (twintania != null) spawned.Add(twintania);
    }

    private void Plummet() => UcobTwintaniaAttacks.Plummet(party, damage, twintania, party.Get(AggroTank(world.Events.Elapsed)));

    private void Fireball(PartyRole role)
    {
        if (twintania is not { } boss || party.Get(role) is not { } target || !target.IsAlive()) return;
        UcobSim.PlayEffect(boss, ActionId.Fireball, 1.1f, target: target.GameObjectId, at: target.Position);
        var hits = UcobSim.AliveMembers(party).Where(m => Vector2.Distance(UcobSim.Flat(m.Position), UcobSim.Flat(target.Position)) <= State.FireballRadius).ToList();
        foreach (var hit in hits)
        {
            if (hits.Count < State.FireballMinimumStack) hit.Die(ActionId.Fireball, "Fireball wasn't shared (stack on the red marker)");
            else damage.ApplyDamage(hit, 0.5f, ActionId.Fireball, "Fireball", lethal: false);
        }
    }

    private void FlyOff()
    {
        if (twintania is not { } boss) return;
        boss.SetTargetable(false);
        boss.RemoveStatus(StatusId.TwintaniaNeurolink);
        hatches.Clear();
        world.Events.Add(1.5f, () => boss.SetVisible(false));
    }

    private void DespawnAll()
    {
        neurolinks.Clear();
        twisters.Clear();
        liquidHell.Clear();
        hatches.Clear();
        foreach (var enemy in spawned) enemy.Despawn();
        spawned.Clear();
        helpers.Clear();
    }
}
