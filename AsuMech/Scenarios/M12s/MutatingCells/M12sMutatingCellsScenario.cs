using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.EnemyActions;
using AsuMech.Core.Game;
using AsuMech.Core.Game.Ai;
using AsuMech.Core.Game.Geometry;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using static AsuMech.Scenarios.M12s.M12sConstants;
using Actions = AsuMech.Scenarios.M12s.M12sActions;

namespace AsuMech.Scenarios.M12s.MutatingCells;

// Mutating Cells to Double Sobat (t=0 = P2 2:56, 4.3s before Mutating Cells): α must take no damage
// and β must, twice with the letters swapped; Blood Mana's orbs drift into two black holes, β pops the
// far side's matching orbs to delay them, and Blood Wakening releases what each hole swallowed in two
// waves; Netherworld's stack for the second β; Arcadia Aflame; Double Sobat's buster, its turned
// repeat and the two tank Esoteric Finishers.
public sealed class M12sMutatingCellsScenario : IScenario
{
    public string Name => "Mutating Cells";
    public IPhase Phase => M12sZone.P2;

    public IReadOnlyList<IScenarioAi> AiStrats => [new M12sMutatingCellsAi()];

    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    private readonly M12sMutatingCellsSettingsWindow settingsWindow = new();

    public const float ArenaRadius = 20f;
    public const float MutatingCellsCastAt = 4.25f;
    public const float MutationsAt = 9f;
    public const float FirstMutationLasts = 17f;
    public const float MutatingLasts = 1.98f;
    public const float SecondMutationLasts = 28f;
    public static readonly float[] MutationEndsAt = [MutationsAt + FirstMutationLasts, MutationsAt + FirstMutationLasts + MutatingLasts + SecondMutationLasts];
    public const float BloodManaCastAt = 10.42f;
    public const float HolesShowAt = 14.3f;
    public const float OrbsShowAt = 17.06f;
    public const float OrbsFlyOutAt = 17.31f;
    public const float OrbsDriftAt = 24.33f;
    public const float LastOrbAbsorbedBy = 33.6f;
    public const float BloodWakeningCastAt = 37.63f;
    public static readonly float[] WaveAt = [42.41f, 47.49f];
    private const float HolePulseBeforeWave = 0.5f;
    public const float NetherworldCastAt = 46.82f;
    public const float WailingWaveAt = 53.11f;
    public const float ArcadiaCastAt = 54.98f;
    public const float SobatMarkAt = 64.12f;
    public const float SobatCastAt = 64.21f;
    public const float SobatBusterAt = 69.78f;
    public const float SobatRepeatResolves = 4.6f;
    public const float EsotericAt = 76.82f;

    private const float FlyOutSpeed = 13f;
    public const float DriftSpeed = 2.05f;
    public const float PopPause = 3f;
    // UNVERIFIED: how close an orb must come to a player to pop on them.
    public const float PopReach = 1.5f;
    public const float BurstRadius = 5f;
    private const float AbsorbedWithin = 0.3f;
    private const float SameWaveWithin = 0.5f;

    private SimWorld world = null!;
    private M12sMutatingCellsState state = null!;
    private SimEnemy? boss;
    private readonly List<SimEnemy> helpers = [];
    private int nextHelper;
    private readonly SimEnemy?[] holes = new SimEnemy?[2];
    private readonly List<OrbActor> orbs = [];
    // Who took damage during the current mutation; the letter decides whether that was right.
    private readonly HashSet<SimCharacter> damaged = [];

    private sealed class OrbActor(Orb orb, SimEnemy actor)
    {
        public Orb Orb { get; } = orb;
        public SimEnemy Actor { get; } = actor;
        public bool Drifting { get; set; }
        public bool Popped { get; set; }
        public float PausedUntil { get; set; }
        public float? AbsorbedAt { get; set; }
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        state = new M12sMutatingCellsState(world.Rng, settingsWindow.Overrides, world.Party.PlayerRole);
        helpers.Clear();
        nextHelper = 0;
        Array.Clear(holes);
        orbs.Clear();
        damaged.Clear();
        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<M12sMutatingCellsState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0f, () => world.EnforceArenaBoundary(new CircleArena(ArenaRadius), "Walked off the arena"));
        world.Events.Add(0f, () => world.PlaceWaymarks(DnP2Waymarks));
        world.Events.Add(0f, SpawnActors);
        world.Events.Add(4f, () => TankWith(0));
        world.Events.Add(MutatingCellsCastAt, () => boss?.Cast(ActionId.MutatingCells, animationLock: 0.2f));
        world.Events.Add(8.51f, () =>
        {
            foreach (var member in world.Party.ActiveMembers().ToList())
                Helper(new Placement(Vector3.Zero, 0f)).Cast(ActionId.MutatingCellsVisual, member, animationLock: 0.2f);
        });
        world.Events.Add(MutationsAt, () => Mutate(0, FirstMutationLasts));
        world.Events.Add(10.29f, () => Talk(0x1C));
        world.Events.Add(BloodManaCastAt, () => boss?.Cast(ActionId.BloodMana, animationLock: 0.2f));
        world.Events.Add(HolesShowAt, SpawnHoles);
        world.Events.Add(OrbsShowAt, SpawnOrbs);
        world.Events.Add(OrbsFlyOutAt, FlyOut);
        world.Events.Add(19.48f, () => Talk(0x1D));
        world.Events.Add(OrbsDriftAt, Drift);
        world.Events.Add(MutationEndsAt[0], () => ResolveMutation(0));
        world.Events.Add(MutationEndsAt[0], () => ForEachMember(m => m.AddStatus(StatusId.MutatingCells, MutatingLasts)));
        world.Events.Add(MutationEndsAt[0] + MutatingLasts, () => Mutate(1, SecondMutationLasts));
        world.Events.Add(MutationEndsAt[0] + MutatingLasts, () => TankWith(1));
        world.Events.Add(BloodWakeningCastAt, () => boss?.Cast(ActionId.BloodWakening, animationLock: 0.2f));
        for (var w = 0; w < WaveAt.Length; w++)
        {
            var wave = w;
            world.Events.Add(WaveAt[wave] - HolePulseBeforeWave, () =>
            {
                foreach (var hole in holes) hole?.Cast(ActionId.BlackHoleActivate, animationLock: 0.2f);
            });
            world.Events.Add(WaveAt[wave], () => ReleaseWave(wave));
        }
        world.Events.Add(NetherworldCastAt, () => boss?.Cast(state.NetherworldFar ? ActionId.NetherworldFar : ActionId.NetherworldNear, animationLock: 0.2f));
        world.Events.Add(WailingWaveAt, WailingWave);
        world.Events.Add(ArcadiaCastAt, () => boss?.Cast(Actions.ArcadiaAflame));
        world.Events.Add(MutationEndsAt[1], () => ResolveMutation(1));
        world.Events.Add(SobatMarkAt, () => SobatTarget()?.AttachLockonVfx(LockonId.DoubleSobat));
        world.Events.Add(SobatCastAt, () =>
        {
            boss?.Face(SobatTarget());
            boss?.Cast(ActionId.DoubleSobat, animationLock: 0.2f);
        });
        world.Events.Add(SobatBusterAt, DoubleSobat);
        world.Events.Add(EsotericAt, () =>
        {
            foreach (var tank in new[] { PartyRole.MainTank, PartyRole.OffTank }.Select(Alive).OfType<SimCharacter>())
                Helper(tank.Placement()).Cast(Actions.EsotericFinisher, tank);
        });
    }

    public void Tick(float delta, float elapsed)
    {
        foreach (var orb in orbs)
        {
            if (!orb.Drifting || orb.AbsorbedAt != null) continue;
            if (orb.PausedUntil > 0f)
            {
                if (elapsed < orb.PausedUntil) continue;
                orb.PausedUntil = 0f;
                orb.Actor.MoveTo(M12sMutatingCellsState.Hole(orb.Orb.Side), DriftSpeed);
            }
            if (Vector3.Distance(orb.Actor.Position, M12sMutatingCellsState.Hole(orb.Orb.Side)) <= AbsorbedWithin)
            {
                Absorb(orb, elapsed);
                continue;
            }
            if (!orb.Popped && world.Party.Find.Closest(orb.Actor.Position) is { } touched
                && Vector3.Distance(touched.Position, orb.Actor.Position) <= PopReach)
                BloodyBurst(touched, elapsed);
        }
    }

    public void OnActionResolved(ActionResolved resolved)
    {
        if (!TakesDamage(resolved.Action)) return;
        foreach (var hit in resolved.Hits)
            if (!resolved.Killed.Contains(hit)) damaged.Add(hit);
    }

    private static bool TakesDamage(EnemyAction action)
        => action == Actions.BloodyBurst || action == Actions.WailingWave;

    private void SpawnActors()
    {
        boss = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.LindwurmP2,
            NameId: BNpcNameId.LindwurmP2,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
        for (var i = 0; i < 16; i++)
            if (world.SpawnEnemy(new EnemySpawnConfig(
                    BNpcBaseId: BNpcBaseId.Helper,
                    NameId: BNpcNameId.LindwurmP2,
                    Level: 1,
                    EnemyList: EnemyListMode.Never,
                    Visibility: SpawnVisibility.InvisibleHelper)) is { } helper)
                helpers.Add(helper);
    }

    private SimEnemy Helper(Placement at)
    {
        var helper = helpers[nextHelper++ % helpers.Count];
        helper.SetPosition(at);
        return helper;
    }

    private void Talk(uint line) => world.Map.DirectorUpdate(0x80000027U, line, 0x2U, BattleTalk.Speaker, BattleTalk.NoActor);

    private SimCharacter? Alive(PartyRole role) => world.Party.Get(role) is { } member && member.IsAlive() ? member : null;

    private void ForEachMember(Action<SimCharacter> act)
    {
        foreach (var member in world.Party.ActiveMembers().ToList()) act(member);
    }

    // The β tank of each mutation holds the boss: α must not even take its autos.
    private PartyRole TankOf(int round) => new[] { PartyRole.MainTank, PartyRole.OffTank }.First(t => !state.IsAlpha(t, round));

    private void TankWith(int round) => boss?.SetTarget(Alive(TankOf(round)), follow: false);

    private SimCharacter? SobatTarget() => Alive(TankOf(1)) ?? Alive(TankOf(0));

    private void Mutate(int round, float lasts)
    {
        damaged.Clear();
        foreach (var role in Enum.GetValues<PartyRole>())
            Alive(role)?.AddStatus(state.IsAlpha(role, round) ? StatusId.MutationAlpha : StatusId.MutationBeta, lasts);
    }

    private void ResolveMutation(int round)
    {
        foreach (var role in Enum.GetValues<PartyRole>())
        {
            if (Alive(role) is not { } member) continue;
            var alpha = state.IsAlpha(role, round);
            var tookDamage = damaged.Contains(member);
            var helper = Helper(new Placement(Vector3.Zero, 0f));
            if (alpha && tookDamage) helper.Cast(Actions.UnmitigatedExplosionAlpha, member);
            else if (!alpha && !tookDamage) helper.Cast(Actions.UnmitigatedExplosionBeta, member);
            else helper.Cast(ActionId.MutationDramaticLysis, member, animationLock: 0.2f);
        }
        damaged.Clear();
    }

    private void SpawnHoles()
    {
        for (var i = 0; i < holes.Length; i++)
            holes[i] = SpawnOrb(BNpcBaseId.BlackHole, M12sMutatingCellsState.Hole(i == 0 ? -1 : 1));
    }

    private SimEnemy? SpawnOrb(uint bnpcBaseId, Vector3 at) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: bnpcBaseId,
        NameId: BNpcNameId.ManaSphere,
        Level: Level,
        EnemyList: EnemyListMode.Never,
        Placement: new Placement(at, 0f)));

    private static uint BaseOf(OrbShape shape) => shape switch
    {
        OrbShape.Water => BNpcBaseId.WaterSphere,
        OrbShape.Aero => BNpcBaseId.AeroSphere,
        OrbShape.Thunder => BNpcBaseId.ThunderSphere,
        _ => BNpcBaseId.FireSphere,
    };

    private void SpawnOrbs()
    {
        foreach (var orb in state.Orbs)
            if (SpawnOrb(BaseOf(orb.Shape), M12sMutatingCellsState.Hole(orb.Side)) is { } actor)
                orbs.Add(new OrbActor(orb, actor));
    }

    private void FlyOut()
    {
        foreach (var orb in orbs)
        {
            orb.Actor.Cast(ActionId.ManaSphereSpawn, orb.Orb.Destination, animationLock: 0.2f);
            orb.Actor.MoveTo(orb.Orb.Destination, FlyOutSpeed);
        }
    }

    private void Drift()
    {
        foreach (var orb in orbs)
        {
            orb.Drifting = true;
            orb.Actor.MoveTo(M12sMutatingCellsState.Hole(orb.Orb.Side), DriftSpeed);
        }
    }

    // Bursts on whoever the orb touched; every drifting orb near them stalls, once only.
    private void BloodyBurst(SimCharacter touched, float elapsed)
    {
        Helper(touched.Placement()).Cast(Actions.BloodyBurst, touched);
        foreach (var orb in orbs.Where(o => o.Drifting && !o.Popped && o.AbsorbedAt == null
                     && Vector3.Distance(o.Actor.Position, touched.Position) <= BurstRadius))
        {
            orb.Popped = true;
            orb.PausedUntil = elapsed + PopPause;
            orb.Actor.StopMoving();
        }
    }

    private void Absorb(OrbActor orb, float elapsed)
    {
        orb.AbsorbedAt = elapsed;
        orb.Actor.StopMoving();
        orb.Actor.Cast(ActionId.BlackHoleAbsorb, animationLock: 0.2f);
        holes[orb.Orb.Side < 0 ? 0 : 1]?.Cast(ActionId.BlackHolePulse, animationLock: 0.2f);
        var actor = orb.Actor;
        world.Events.Add(1.5f, actor.Despawn);
    }

    // Each hole releases first what it swallowed first, then the rest.
    private IEnumerable<OrbActor> Wave(int side, int wave)
    {
        var mine = orbs.Where(o => o.Orb.Side == side).ToList();
        var first = mine.Select(o => o.AbsorbedAt ?? float.MaxValue).DefaultIfEmpty(float.MaxValue).Min();
        return mine.Where(o => (o.AbsorbedAt is { } at && at <= first + SameWaveWithin) == (wave == 0));
    }

    private void ReleaseWave(int wave)
    {
        foreach (var side in new[] { -1, 1 })
        {
            var hole = M12sMutatingCellsState.Hole(side);
            foreach (var orb in Wave(side, wave))
                switch (orb.Orb.Shape)
                {
                    case OrbShape.Water: Helper(new Placement(hole, 0f)).Cast(Actions.LindwurmsWater); break;
                    case OrbShape.Aero: Helper(new Placement(hole, 0f)).Cast(Actions.LindwurmsAero); break;
                    case OrbShape.Thunder:
                        Helper(new Placement(hole, 0f)).Cast(Actions.StraightforwardThunder);
                        Helper(new Placement(hole, MathF.PI)).Cast(Actions.StraightforwardThunder);
                        break;
                    default:
                        Helper(new Placement(hole, MathF.PI / 2f)).Cast(Actions.SidewaysFire);
                        Helper(new Placement(hole, -MathF.PI / 2f)).Cast(Actions.SidewaysFire);
                        break;
                }
        }
    }

    private void WailingWave()
    {
        if (boss is not { } lindwurm) return;
        var target = state.NetherworldFar ? world.Party.Find.Farest(lindwurm.Position) : world.Party.Find.Closest(lindwurm.Position);
        if (target != null) Helper(target.Placement()).Cast(Actions.WailingWave, target);
    }

    private void DoubleSobat()
    {
        if (boss is not { } lindwurm) return;
        if (SobatTarget() is { } tank)
        {
            lindwurm.Face(tank);
            lindwurm.Cast(Actions.DoubleSobatBuster(state.SobatTurn), tank);
        }
        var heading = lindwurm.Rotation;
        if (SobatTarget() is { } target)
        {
            var d = target.Position - lindwurm.Position;
            heading = MathF.Atan2(d.X, d.Z);
        }
        Helper(new Placement(lindwurm.Position, heading + state.SobatTurnRadians)).Cast(Actions.DoubleSobatRepeat);
    }
}
